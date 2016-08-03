using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Reports
{
    public class SummaryReportView
    {
        private static readonly string Tag = "SummaryReportsView";

        public static long Workspace
        {
            get
            {
                if (workspaceId == 0)
                {
                    return StoreManager.Singleton.AppState.User.DefaultWorkspaceRemoteId;
                }
                return workspaceId;
            }
            set
            {
                if (workspaceId == value) return;
                workspaceId = value;
            }
        }

        private static long workspaceId;

        private ReportData dataObject;
        private DayOfWeek startOfWeek;
        private IReportsClient reportClient;
        private List<ReportProject> collapsedProjects;
        private List<ReportProject> projects;

        public ZoomLevel Period { get; set; }
        public long WorkspaceId { get; set; }

        public async Task Load(IUserData userData, int backDate)
        {
            if (IsLoading)
            {
                return;
            }
            IsLoading = true;

            reportClient = ServiceContainer.Resolve<IReportsClient> ();
            startOfWeek = userData.StartOfWeek;

            var startDate = ResolveStartDate(backDate);
            var endDate = ResolveEndDate(startDate);
            await FetchData(userData, startDate, endDate);
            IsLoading = false;
        }

        public void CancelLoad()
        {
            if (IsLoading)
            {
                reportClient.CancelRequest();
            }
        }

        private async Task FetchData(IUserData userData, DateTime startDate, DateTime endDate)
        {
            dataObject = CreateEmptyReport(startDate);

            try
            {
                _isError = false;
                var json = await reportClient.GetReports(userData.ApiToken, userData.RemoteId.GetValueOrDefault(), startDate, endDate, Workspace);
                var mapper = new JsonMapper();
                dataObject = mapper.Map<ReportData> (json);
            }
            catch (Exception exc)
            {
                var log = ServiceContainer.Resolve<ILogger> ();
                if (exc.IsNetworkFailure() || exc is TaskCanceledException)
                {
                    var msg = (reportClient.IsCancellationRequested) ? "Fetch reports cancelation requested by user" : "Failed to fetch reports. Network failure.";
                    log.Info(Tag, exc, msg);
                }
                else
                {
                    log.Warning(Tag, exc, "Failed to fetch reports.");
                }
                _isError = !(exc is TaskCanceledException);
            }
            finally
            {
                CalculateReportData();
            }
        }

        private void CalculateReportData()
        {
            //var user = ServiceContainer.Resolve<AuthManager> ().User;
            var user = new UserData();

            long max = 0;
            foreach (var s in dataObject.Activity)
            {
                max = max < s.TotalTime ? s.TotalTime : max;
            }

            _maxTotal = (int)Math.Ceiling(max / 3600f / 5f) * 5;

            _chartRowLabels = new List<string> ();
            foreach (var row in dataObject.Activity)
            {
                _chartRowLabels.Add(LabelForDate(row.StartTime));
            }

            _chartTimeLabels = new List<string> ();
            for (int i = 1; i <= 5; i++)
            {
                _chartTimeLabels.Add(String.Format("{0} h", _maxTotal / 5 * i));
            }

            dataObject.Projects.Sort((x, y) => y.TotalTime.CompareTo(x.TotalTime));
            collapsedProjects = new List<ReportProject> ();
            projects = new List<ReportProject> ();

            var containerProject = new ReportProject
            {
                Currencies = new List<ReportCurrency>(),
                Color = ProjectData.GroupedProjectColorIndex
            };

            const float minimunWeight = 0.01f; // minimum weight of project respect to total time
            var totalValue = Convert.ToSingle(dataObject.Projects.Sum(p => p.TotalTime));
            int count = ProjectData.GroupedProjectColorIndex;

            // group projects on one single project
            foreach (var item in dataObject.Projects)
            {
                if (Convert.ToSingle(item.TotalTime) / totalValue > minimunWeight)
                {
                    collapsedProjects.Add(item);
                }
                else
                {
                    containerProject.BillableTime += item.BillableTime;
                    containerProject.TotalTime += item.TotalTime;

                    // group currencies
                    foreach (var currencyItem in item.Currencies)
                    {
                        var index = containerProject.Currencies.FindIndex(c => c.Currency == currencyItem.Currency);
                        if (index != -1)
                            containerProject.Currencies [index] = new ReportCurrency
                        {
                            Amount = containerProject.Currencies [index].Amount + currencyItem.Amount,
                            Currency = currencyItem.Currency
                        };
                        else
                        {
                            containerProject.Currencies.Add(currencyItem);
                        }
                    }
                    count++;
                }
            }

            // check if small projects exists and are enough to be a separeted slice
            if (containerProject.TotalTime > 0 && Convert.ToSingle(containerProject.TotalTime) / totalValue > minimunWeight)
            {
                containerProject.Project = count.ToString();
                collapsedProjects.Add(containerProject);
                projects = new List<ReportProject> (collapsedProjects);
            }
            else
            {
                projects = new List<ReportProject> (dataObject.Projects);
            }

            // format total and billable time
            FormatTimeData(collapsedProjects, user);
            FormatTimeData(projects, user);
            FormatTimeData(dataObject.Projects, user);
            FormatActivityTimeData(dataObject.Activity, user);
        }

        public bool IsLoading { get; private set; }


        public int ActivityCount
        {
            get
            {
                return dataObject.Activity.Count;
            }
        }

        public List<ReportActivity> Activity
        {
            get
            {
                return dataObject.Activity;
            }
        }

        public List<ReportProject> CollapsedProjects
        {
            get
            {
                return collapsedProjects;
            }
        }

        public List<ReportProject> Projects
        {
            get
            {
                return projects;
            }
        }

        public List<string> TotalCost
        {
            get
            {
                return dataObject.TotalCost;
            }
        }

        public string TotalBillale
        {
            get
            {
                return FormatMilliseconds(dataObject.TotalBillable);
            }
        }

        public string TotalGrand
        {
            get
            {
                return FormatMilliseconds(dataObject.TotalGrand);
            }
        }

        private List<string> _chartRowLabels;

        public List<string> ChartRowLabels
        {
            get
            {
                return _chartRowLabels;
            }
        }

        private List<string> _chartTimeLabels;

        public List<string> ChartTimeLabels
        {
            get
            {
                return _chartTimeLabels;
            }
        }

        private int _maxTotal;

        public int MaxTotal
        {
            get
            {
                return _maxTotal;
            }
        }

        private bool _isError;

        public bool IsError
        {
            get
            {
                return _isError;
            }
        }

        public string FormatMilliseconds(long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds(ms);
            decimal totalHours = Math.Floor((decimal)timeSpan.TotalHours);
            return string.Format("{0} h {1} min", (int)totalHours, timeSpan.Minutes);
        }

        public DateTime ResolveStartDate(int backDate)
        {
            var current = DateTime.Today;
            if (Period == ZoomLevel.Week)
            {
                var date = current.StartOfWeek(startOfWeek).AddDays(backDate * 7);
                return date;
            }

            if (Period == ZoomLevel.Month)
            {
                current = current.AddMonths(backDate);
                return new DateTime(current.Year, current.Month, 1);
            }

            return new DateTime(current.Year + backDate, 1, 1);
        }

        public DateTime ResolveEndDate(DateTime start)
        {
            if (Period == ZoomLevel.Week)
            {
                return start.AddDays(6);
            }

            if (Period == ZoomLevel.Month)
            {
                return start.AddMonths(1).AddDays(-1);
            }

            return start.AddYears(1).AddDays(-1);
        }

        public static ZoomLevel GetLastZoomViewed()
        {
            return (ZoomLevel)StoreManager.Singleton.AppState.Settings.LastReportZoom;
        }

        public static void SaveReportsState(ZoomLevel zoomLevel)
        {
            RxChain.Send(new DataMsg.UpdateSetting(nameof(SettingsState.LastReportZoom), (int)zoomLevel));
        }

        private string LabelForDate(DateTime date)
        {
            if (Period == ZoomLevel.Week)
            {
                return string.Format("{0:ddd}", date);
            }

            if (Period == ZoomLevel.Month)
            {
                return string.Format("{0:ddd dd}", date);
            }

            return string.Format("{0:MMM}", date);
        }

        private string GetFormattedTime(UserData user, long milliseconds)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);
            decimal totalHours = Math.Floor((decimal)duration.TotalHours);
            string formattedString = string.Format("{0}:{1}:{2}", (int)totalHours, duration.ToString(@"mm"), duration.ToString(@"ss"));

            if (user != null)
            {
                if (user.DurationFormat == DurationFormat.Classic)
                {
                    if (duration.TotalMinutes < 1)
                    {
                        formattedString = duration.ToString(@"s\ \s\e\c");
                    }
                    else if (duration.TotalMinutes > 1 && duration.TotalMinutes < 60)
                    {
                        formattedString = duration.ToString(@"mm\:ss\ \m\i\n");
                    }
                }
                else if (user.DurationFormat == DurationFormat.Decimal)
                {
                    formattedString = string.Format("{0:0.00} h", duration.TotalHours);
                }
            }
            return formattedString;
        }

        private void FormatTimeData(IList<ReportProject> items, UserData user)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var project = items[i];
                project.FormattedTotalTime = GetFormattedTime(user, project.TotalTime);
                project.FormattedBillableTime = GetFormattedTime(user, project.BillableTime);
                items[i] = project;
            }
        }

        private void FormatActivityTimeData(IList<ReportActivity> activities, UserData user)
        {
            for (int i = 0; i < activities.Count; i++)
            {
                var activity = activities [i];

                string formattedString = string.Empty;
                if (activity.TotalTime > 0)
                {
                    TimeSpan duration = TimeSpan.FromSeconds(activity.TotalTime);
                    decimal totalHours = Math.Floor((decimal)duration.TotalHours);

                    formattedString = string.Format("{0}:{1}", (int)totalHours, duration.ToString(@"mm"));
                    if (user.DurationFormat == DurationFormat.Decimal)
                    {
                        formattedString = string.Format("{0:0.00} h", duration.TotalHours);
                    }
                }

                activity.FormattedTotalTime = formattedString;
                activities [i] = activity;
            }
        }

        private ReportData CreateEmptyReport(DateTime startDate)
        {
            var activityList = new List<ReportActivity> ();

            int total;
            if (Period == ZoomLevel.Week)
            {
                total = 7;
            }
            else if (Period == ZoomLevel.Month)
            {
                total = 30;
            }
            else
            {
                total = 12;
            }

            for (int i = 0; i < total; i++)
            {
                var activiy = new ReportActivity();
                activiy.BillableTime = 0;
                activiy.TotalTime = 0;
                if (Period == ZoomLevel.Week)
                {
                    activiy.StartTime = startDate.AddDays(Convert.ToDouble(i));
                }
                else if (Period == ZoomLevel.Month)
                {
                    activiy.StartTime = startDate.AddDays(Convert.ToDouble(i));
                }
                else
                {
                    activiy.StartTime = startDate.AddMonths(i);
                }
                activityList.Add(activiy);
            }

            return new ReportData()
            {
                Projects = new List<ReportProject>(),
                Activity = activityList,
                TotalBillable = 0,
                TotalGrand = 0
            };
        }
    }
}
