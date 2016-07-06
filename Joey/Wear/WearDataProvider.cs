using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using XPlatUtils;

namespace Toggl.Joey.Wear
{
    public static class WearDataProvider
    {
        private const int itemCount = 10;

        public static void StartStopTimeEntry(Context ctx)
        {
            //await Task.Delay(5);//??
            var active = StoreManager.Singleton.AppState.ActiveEntry;
            if (active.Data.State == TimeEntryState.Running)
            {
                RxChain.Send(new DataMsg.TimeEntryStop(active.Data));
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent(TimerStopSource.Watch);
            }
            else
            {
                active.Data.With(t =>
                {
                    t.Description = ctx.Resources.GetString(Resource.String.WearEntryDefaultDescription);
                });

                var tcs = new TaskCompletionSource<ITimeEntryData>();

                RxChain.Send(new DataMsg.TimeEntryStart(), new RxChain.Continuation((state) =>
                {
                    ServiceContainer.Resolve<ITracker>().SendTimerStartEvent(TimerStartSource.AppNew);
                    tcs.SetResult(StoreManager.Singleton.AppState.ActiveEntry.Data);
                }));
            }
        }

        public static void ContinueTimeEntry(Guid timeEntryId)
        {
            RichTimeEntry richEntry;
            StoreManager.Singleton.AppState.TimeEntries.TryGetValue(timeEntryId, out richEntry);
            RxChain.Send(new DataMsg.TimeEntryContinue(richEntry.Data));
            ServiceContainer.Resolve<ITracker>().SendTimerStartEvent(TimerStartSource.AppContinue);
        }

        public static List<SimpleTimeEntryData> GetTimeEntryData()
        {
            var entries = StoreManager.Singleton.AppState.TimeEntries.Values.Where(x => x.Data.State == TimeEntryState.Finished).OrderByDescending(x => x.Data.StartTime);


            var uniqueEntries = entries.GroupBy(x  => new {x.Data.ProjectId, x.Data.Description })
            .Select(grp => grp.First())
            .Take(itemCount)
            .ToList();

            var simpleEntries = new List<SimpleTimeEntryData> ();
            foreach (var entry in uniqueEntries)
            {

                int color = 0;
                String projectName = "";
                if (entry.Info.ProjectData != null)
                {
                    color = entry.Info.ProjectData.Color;
                    projectName = entry.Info.ProjectData.Name;
                }
                var colorString = ProjectData.HexColors [color % ProjectData.HexColors.Length];
                simpleEntries.Add(
                    new SimpleTimeEntryData
                {
                    Id = entry.Data.Id,
                    IsRunning = entry.Data.State == TimeEntryState.Running,
                    Description = entry.Data.Description,
                    Project = entry.Info.ProjectData.Name,
                    ProjectColor = colorString,
                    StartTime = entry.Data.StartTime,
                    StopTime = entry.Data.StopTime ?? DateTime.MinValue
                });
            }
            return simpleEntries;
        }
    }
}