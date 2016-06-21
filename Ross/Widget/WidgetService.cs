using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Threading;
using Foundation;
using Newtonsoft.Json;
using NotificationCenter;
using Toggl.Phoebe;
using Toggl.Phoebe.Reactive;
using XPlatUtils;
using Toggl.Phoebe.Data.Models;
using UIKit;
using Toggl.Ross.ViewControllers;

namespace Toggl.Ross.Widget
{
    public class WidgetService : IDisposable
    {
        public static string StartedEntryKey = "started_entry_key";
        public static string TimeEntriesKey = "time_entries_key";
        public static string AppActiveEntryKey = "app_active_entry_key";
        public static string AppBackgroundEntryKey = "app_bg_entry_key";
        public static string IsUserLoggedKey = "is_logged_key";
        public static string TodayUrlPrefix = "today";
        public static string StartEntryUrlPrefix = "start";
        public static string ContinueEntryUrlPrefix = "continue";

        private NSUserDefaults nsUserDefaults;
        private IDisposable entriesSubscriber, userStateSubscriber;

        public NSUserDefaults UserDefaults
        {
            get
            {
                if (nsUserDefaults == null)
                {
                    nsUserDefaults = new NSUserDefaults("group." + NSBundle.MainBundle.BundleIdentifier, NSUserDefaultsType.SuiteName);
                }
                return nsUserDefaults;
            }
        }

        public WidgetService()
        {
            SetAppActivated(true);
            SetAppOnBackground(false);
            //;
            entriesSubscriber = StoreManager
                                .Singleton
                                .Observe(x => x.State.TimeEntries.Values)
                                .DistinctUntilChanged()
                                .ObserveOn(SynchronizationContext.Current)
                                .Subscribe(x => OnEntriesChanged(x));

            userStateSubscriber = StoreManager
                                  .Singleton
                                  .Observe(x => x.State.Settings.UserId)
                                  .DistinctUntilChanged()
                                  .Subscribe(x =>
            {
                UserDefaults.SetBool(x != Guid.Empty, IsUserLoggedKey);
                UpdateWidgetContent();
            });
        }

        private void OnEntriesChanged(IEnumerable<RichTimeEntry> entries)
        {
            var data = entries.Take(4);
            var lastEntries = new List<WidgetEntryData> ();

            foreach (var entry in data)
            {
                lastEntries.Add(new WidgetEntryData
                {
                    Id = entry.Data.Id.ToString(),
                    ProjectName = entry.Info.ProjectData.Name,
                    Description = entry.Data.Description,
                    Color = ProjectData.HexColors [ entry.Info.ProjectData.Color % ProjectData.HexColors.Length],
                    IsRunning = entry.Data.State == TimeEntryState.Running,
                    ClientName = entry.Info.ClientData.Name,
                    StartTime = entry.Data.StartTime,
                    StopTime = entry.Data.StopTime.HasValue ? entry.Data.StopTime.Value : Time.Now
                });
            }

            var json = JsonConvert.SerializeObject(lastEntries);
            UserDefaults.SetString(json, TimeEntriesKey);
            UpdateWidgetContent();
        }

        public void SetAppActivated(bool isActivated)
        {
            UserDefaults.SetBool(isActivated, AppActiveEntryKey);
            UpdateWidgetContent();
        }

        public void SetAppOnBackground(bool isBackground)
        {
            UserDefaults.SetBool(isBackground, AppBackgroundEntryKey);
            UpdateWidgetContent();
        }

        public void ShowNewTimeEntryScreen(AppState state, Guid currentTimeEntry)
        {
            if (state.TimeEntries.ContainsKey(currentTimeEntry))
            {
                var wId = state.TimeEntries [currentTimeEntry].Data.WorkspaceId;
                var topVCList = new List<UIViewController> (UIApplication.SharedApplication.KeyWindow.RootViewController.ChildViewControllers);
                if (topVCList.Count > 0)
                {
                    // Get current VC's navigation
                    var controllers = new List<UIViewController> (topVCList[0].NavigationController.ViewControllers);
                    var editController = EditTimeEntryViewController.ForExistingEntry(currentTimeEntry);
                    controllers.Add(editController);
                    if (state.Settings.ChooseProjectForNew)
                    {
                        controllers.Add(new ProjectSelectionViewController(editController));
                    }
                    topVCList[0].NavigationController.SetViewControllers(controllers.ToArray(), true);
                }
            }
        }

        public void Dispose()
        {
            userStateSubscriber.Dispose();
            entriesSubscriber.Dispose();
        }

        private void UpdateWidgetContent()
        {
            if (ServiceContainer.Resolve<IPlatformUtils> ().IsWidgetAvailable)
            {
                var controller = NCWidgetController.GetWidgetController();
                controller.SetHasContent(true, NSBundle.MainBundle.BundleIdentifier + ".today");
            }
        }

        private class WidgetEntryData
        {
            public string Id { get; set; }
            public string ProjectName { get; set; }
            public string Description { get; set; }
            public string ClientName { get; set; }
            public string Color { get; set; }
            public bool IsRunning { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime StopTime { get; set; }
        }
    }
}