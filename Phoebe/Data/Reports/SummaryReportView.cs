﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Reports
{
    public class SummaryReportView
    {
        private static readonly string Tag = "SummaryReportsView";
        private DateTime startDate;
        private DateTime endDate;
        private ReportData dataObject;
        private DayOfWeek startOfWeek;
        private long? workspaceId;
        public ZoomLevel Period;

        public async Task Load (int backDate)
        {
            if (IsLoading) {
                return;
            }

            IsLoading = true;
            if (workspaceId == null) {
                await Initialize ();
            }
            startDate = ResolveStartDate (backDate);
            endDate = ResolveEndDate (startDate);

            await FetchData ();
            IsLoading = false;
        }

        private async Task Initialize ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            workspaceId = await store.ExecuteInTransactionAsync (ctx => ctx.GetRemoteId<WorkspaceData> (user.DefaultWorkspaceId));
            startOfWeek = user.StartOfWeek;
        }

        private async Task FetchData ()
        {
            try {
                var client = ServiceContainer.Resolve<IReportsClient> ();
                var json = await client.GetReports (startDate, endDate, (long)workspaceId);
                dataObject = json.Import ();
                await AddProjectColors ();
                AddFormattedTime();
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to fetch reports.");
            }
        }

        public bool IsLoading { get; private set; }

        public int ActivityCount
        {
            get {
                return dataObject.Activity.Count;
            }
        }

        public List<ReportActivity> Activity
        {
            get {
                return dataObject.Activity;
            }
        }

        public List<ReportProject> Projects
        {
            get {
                return dataObject.Projects;
            }
        }

        public string TotalBillale
        {
            get {
                return FormatMilliseconds (dataObject.TotalBillable);
            }
        }

        public string TotalGrand
        {
            get {
                return FormatMilliseconds (dataObject.TotalGrand);
            }
        }

        public List<string> ChartRowLabels ()
        {
            List<string> labels = new List<string> ();
            foreach (var row in dataObject.Activity) {
                labels.Add (LabelForDate (row.StartTime));
            }
            return labels;
        }

        public string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            decimal totalHours = Math.Floor ((decimal)timeSpan.TotalHours);
            return String.Format ("{0} h {1} min", (int)totalHours, timeSpan.Minutes);

        }

        public string FormatByUserSettings (long ms)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds (ms);
            string formattedString = duration.ToString (@"h\:mm\:ss");
            var user = ServiceContainer.Resolve<AuthManager> ().User;

            if (user == null) {
                return formattedString;
            }

            if (user.DurationFormat == DurationFormat.Classic) {
                if (duration.TotalMinutes < 1) {
                    formattedString = duration.ToString (@"s\ \s\e\c");
                } else if (duration.TotalMinutes > 1 && duration.TotalMinutes < 60) {
                    formattedString = duration.ToString (@"mm\:ss\ \m\i\n");
                } else {
                    formattedString = duration.ToString (@"hh\:mm\:ss");
                }
            } else if (user.DurationFormat == DurationFormat.Decimal) {
                formattedString = String.Format ("{0:0.00} h", duration.TotalHours);
            }
            return formattedString;
        }

        public List<string> ChartTimeLabels ()
        {
            var max = GetMaxTotal ();
            List<string> labels = new List<string> ();
            for (int i = 1; i <= 4; i++) {
                labels.Add (String.Format ("{0} h", max / 4 * i));
            }
            return labels;
        }

        private int GetMaxTotal ()
        {
            long max = 0;
            foreach (var s in dataObject.Activity) {
                max = max < s.TotalTime ? s.TotalTime : max;
            }
            return (int)Math.Ceiling ((double)TimeSpan.FromSeconds (max).Hours / 4D) * 4;
        }

        private string LabelForDate (DateTime date)
        {
            if (Period == ZoomLevel.Week) {
                return String.Format ("{0:ddd}", date);
            } else if (Period == ZoomLevel.Month) {
                return String.Format ("{0:ddd dd}", date);
            } else {
                return String.Format ("{0:MMM}", date);
            }
        }

        public DateTime ResolveStartDate (int backDate)
        {
            var current = DateTime.Today;
            if (Period == ZoomLevel.Week) {
                var date = DateTime.Today.AddDays (-backDate * 7);
                var diff = (int)date.DayOfWeek - (int)startOfWeek;
                return date.AddDays (diff);
            } else if (Period == ZoomLevel.Month) {
                current = current.AddMonths (-backDate);
                return new DateTime (current.Year, current.Month, 1);

            } else {
                return new DateTime (current.Year - backDate, 1, 1);
            }
        }

        public DateTime ResolveEndDate (DateTime start)
        {
            if (Period == ZoomLevel.Week) {
                return start.AddDays (6);
            } else if (Period == ZoomLevel.Month) {
                return start.AddMonths (1).AddDays (-1);
            } else {
                return start.AddYears (1).AddDays (-1);
            }
        }

        private async Task AddProjectColors ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var user = ServiceContainer.Resolve<AuthManager> ().User;

            var withColors = new List<ReportProject> ();
            foreach (var item in dataObject.Projects) {
                var d = new ReportProject ();
                d.Project = item.Project;
                d.TotalTime = item.TotalTime;
                d.Color = await store.ExecuteInTransactionAsync (ctx => ctx.GetProjectColorFromName (user.DefaultWorkspaceId, item.Project));
                withColors.Add (d);
            }
            dataObject.Projects = withColors;
        }

        private void AddFormattedTime ()
        {
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            var withFormattedTime = new List<ReportProject> ();

            foreach (var item in dataObject.Projects) {
                var d = new ReportProject ();
                d.Project = item.Project;
                d.TotalTime = item.TotalTime;
                d.Color = item.Color;

                TimeSpan duration = TimeSpan.FromMilliseconds ( item.TotalTime);
                string formattedString = duration.ToString (@"h\:mm\:ss");

                if ( user!= null) {
                    if ( user.DurationFormat == DurationFormat.Classic) {
                        if (duration.TotalMinutes < 1) {
                            formattedString = duration.ToString (@"s\ \s\e\c");
                        } else if (duration.TotalMinutes > 1 && duration.TotalMinutes < 60) {
                            formattedString = duration.ToString (@"mm\:ss\ \m\i\n");
                        } else {
                            formattedString = duration.ToString (@"hh\:mm\:ss");
                        }
                    } else if (user.DurationFormat == DurationFormat.Decimal) {
                        formattedString = String.Format ("{0:0.00} h", duration.TotalHours);
                    }
                }
                d.FormattedTime = formattedString;
                withFormattedTime.Add (d);
            }
            dataObject.Projects = withFormattedTime;
        }
    }
}