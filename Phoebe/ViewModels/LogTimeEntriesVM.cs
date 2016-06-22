using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels.Timer;
using XPlatUtils;
using System.Collections.Generic;
using Toggl.Phoebe.Logging;
using System.Reactive.Concurrency;

namespace Toggl.Phoebe.ViewModels
{
    [ImplementPropertyChanged]
    public class LogTimeEntriesVM : ViewModelBase, IDisposable
    {
        public class LoadInfoType
        {
            public bool IsSyncing { get; private set; }
            public bool HasMore { get; private set; }
            public bool HadErrors { get; private set; }

            public LoadInfoType(bool isSyncing, bool hasMore, bool hadErrors)
            {
                IsSyncing = isSyncing;
                HasMore = hasMore;
                HadErrors = hadErrors;
            }
        }

        private TimeEntryCollection timeEntryCollection;
        private readonly IDisposable subscriptionState;
        private readonly SynchronizationContext uiContext;
        private IDisposable durationSubscriber;
        private IDisposable subscriptionTimeEntries;
        private ILogger logger;

        public bool IsFullSyncing { get; private set; }
        public bool HasSyncErrors { get; private set; }
        public bool HasCRUDError { get; private set; }
        public bool IsGroupedMode { get; private set; }
        public string Duration { get; private set; }
        public bool IsEntryRunning { get; private set; }
        public LoadInfoType LoadInfo { get; private set; }
        public RichTimeEntry ActiveEntry { get; private set; }
        public ObservableCollection<IHolder> Collection => timeEntryCollection;
        public IObservable<long> TimerObservable { get; private set; }

        public LogTimeEntriesVM(AppState appState)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "TimeEntryList Screen";

            uiContext = SynchronizationContext.Current;
            ResetCollection(appState.Settings.GroupedEntries);

            subscriptionState = StoreManager
                                .Singleton
                                .Observe(x => x.State)
                                .ObserveOn(uiContext)
                                .StartWith(appState)
                .DistinctUntilChanged(state => new { state.RequestInfo, state.Settings})
                .SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(x => UpdateSettingsAndRequestInfo(x.Settings, x.RequestInfo));

            subscriptionTimeEntries = StoreManager
                .Singleton
                .Observe(x => x.State)
                .ObserveOn(uiContext)
                .StartWith(appState)
                .Select(state => state.TimeEntries.Values.Where(e => e.Data.State == TimeEntryState.Running))
                .DistinctUntilChanged(x => x.Count())
                .SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(CheckActiveEntries);

            // TODO: Rx find a better solution to force
            // an inmediate update using Rx code.
            UpdateSettingsAndRequestInfo(appState.Settings, appState.RequestInfo);

            TimerObservable = Observable
                                .Timer(TimeSpan.FromMilliseconds(1000 - Time.Now.Millisecond), TimeSpan.FromSeconds(1))
                                .ObserveOn(uiContext);
            
            durationSubscriber = TimerObservable.Subscribe(x => UpdateDuration());

            // The ViewModel is created and start to load
            // content. This line was in the View before because
            // it was an async method.
            LoadMore();
        }

        private void ResetCollection(bool isGroupedMode)
        {
            timeEntryCollection?.Dispose();
            IsGroupedMode = isGroupedMode;
            timeEntryCollection = new TimeEntryCollection(
                isGroupedMode ? TimeEntryGroupMethod.ByDateAndTask : TimeEntryGroupMethod.Single, uiContext);
        }

        public void Dispose()
        {
            durationSubscriber?.Dispose();
            subscriptionState?.Dispose();
            timeEntryCollection?.Dispose();
            subscriptionTimeEntries?.Dispose();
        }

        public void TriggerFullSync() => 
            RxChain.Send(new ServerRequest.GetChanges());

        public void LoadMore()
        {
            LoadInfo = new LoadInfoType(true, true, false);
            RxChain.Send(new DataMsg.TimeEntriesLoad());
        }

        public void ContinueTimeEntry(int index)
        {
            var timeEntryHolder = timeEntryCollection.ElementAt(index) as ITimeEntryHolder;
            if (timeEntryHolder == null)
            {
                return;
            }

            if (timeEntryHolder.Entry.Data.State == TimeEntryState.Running)
            {
                RxChain.Send(new DataMsg.TimeEntryStop(timeEntryHolder.Entry.Data));
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent(TimerStopSource.App);
            }
            else
            {
                RxChain.Send(new DataMsg.TimeEntryContinue(timeEntryHolder.Entry.Data));
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent(TimerStartSource.AppContinue);
            }
        }

        public Task<ITimeEntryData> StartNewTimeEntryAsync()
        {
            var tcs = new TaskCompletionSource<ITimeEntryData> ();

            RxChain.Send(new DataMsg.TimeEntryStart(), new RxChain.Continuation((state) =>
            {
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent(TimerStartSource.AppNew);
                var activeEntry = state.TimeEntries.FindActiveEntry();
                tcs.SetResult(activeEntry.Data);
            }));

            return tcs.Task;
        }

        public void StopTimeEntry()
        {
            // TODO RX: Protect from requests in short time (double click...)?
            var activeEntry = StoreManager.Singleton.AppState.TimeEntries.FindActiveEntry();
            RxChain.Send(new DataMsg.TimeEntryStop(activeEntry.Data));
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent(TimerStopSource.App);
        }

        public void RemoveTimeEntry(int index)
        {
            // TODO: Add analytic event
            var te = Collection.ElementAt(index) as ITimeEntryHolder;
            RxChain.Send(new DataMsg.TimeEntriesRemove(te.Entry.Data));
        }

        #region Extra

        public void ReportExperiment(string actionKey, string actionValue)
        {
            if (Collection.Count == 0 && WelcomeScreenShouldBeShown)
            {
                OBMExperimentManager.Send(actionKey, actionValue, StoreManager.Singleton.AppState.User);
            }
        }

        public bool IsInExperiment() => 
            OBMExperimentManager.IncludedInExperiment(StoreManager.Singleton.AppState.User);

        public bool WelcomeScreenShouldBeShown => 
            StoreManager.Singleton.AppState.Settings.ShowWelcome;

        #endregion

        private void UpdateSettingsAndRequestInfo(SettingsState settings, RequestInfo reqInfo)
        {
            if (settings.GroupedEntries != IsGroupedMode)
            {
                ResetCollection(settings.GroupedEntries);
            }

            // Check full Sync info
            IsFullSyncing = reqInfo.Running.Any(x => (x is ServerRequest.GetChanges || x is ServerRequest.GetCurrentState));
            // Sync error
            HasSyncErrors = reqInfo.HadErrors && reqInfo.ErrorInfo.Item2 == Guid.Empty;
            // Crud error
            HasCRUDError = reqInfo.HadErrors && reqInfo.ErrorInfo.Item2 != Guid.Empty;

            var newLoadInfo = new LoadInfoType(
                reqInfo.Running.Any(x => x is ServerRequest.DownloadEntries),
                reqInfo.HasMoreEntries,
                reqInfo.HadErrors
            );

            // Check if LoadInfo has changed
            if (LoadInfo == null ||
                    (LoadInfo.HadErrors != newLoadInfo.HadErrors ||
                     LoadInfo.HasMore != newLoadInfo.HasMore ||
                     LoadInfo.IsSyncing != newLoadInfo.IsSyncing))
            {
                LoadInfo = newLoadInfo;
            }
        }

        private void CheckActiveEntries(IEnumerable<RichTimeEntry> activeEntries)
        {
            // this should never happen, however a user reported the case  
            // TODO delete once the bug gets fixed
            var ordered = activeEntries.OrderByDescending(x => x.Data.StartTime);
            if (ordered.Count() > 1)
            {
                var wrongEntries = ordered.Skip(1).ToList();
                foreach (var entry in wrongEntries)
                {
                    RxChain.Send(new DataMsg.TimeEntryStop(entry.Data));
                }

                logger = logger ?? ServiceContainer.Resolve<ILogger>();
                logger.Error(nameof(LogTimeEntriesVM), "More than one active entry detected");
            }

            ActiveEntry = ordered.FirstOrDefault();

            IsEntryRunning = ActiveEntry != null;

            ActiveEntry = ActiveEntry ?? new RichTimeEntry(new TimeEntryData(), StoreManager.Singleton.AppState);

            UpdateDuration();
        }

        private void UpdateDuration()
        {
            if (IsEntryRunning)
            {
                Duration = string.Format("{0:D2}:{1:mm}:{1:ss}", 
                                         (int)ActiveEntry.Data.GetDuration().TotalHours, ActiveEntry.Data.GetDuration());
            }
            else
            {
                Duration = TimeSpan.FromSeconds(0).ToString().Substring(0, 8);
            }
        }

        public bool IsNoUserMode
        {
            get
            {
                return String.IsNullOrEmpty(StoreManager.Singleton.AppState.User.ApiToken);
            }
        }
    }
}
