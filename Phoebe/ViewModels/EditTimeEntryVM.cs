using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using GalaSoft.MvvmLight;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using System.Reactive.Concurrency;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    public class EditTimeEntryVM : ViewModelBase, IDisposable
    {
        public const string ErrorTitle = "Error";
        public const string StartTimeError = "Start time should be earlier than stop time!";
        public const string StopTimeError = "Stop time should be after start time!";
        public const int LoadSuggestionsCharLimit = 2;
        public const int LoadSuggestionsResultsLimit = 10;
        public const int LoadSuggestionsThrottleMilliseconds = 500;

        private IDisposable subscriptionTimer, subscriptionState;
        private RichTimeEntry richData;
        private RichTimeEntry previousData;
        private event EventHandler<string> DescriptionChanged;

        public static EditTimeEntryVM ForManualAddition(AppState appState)
        {
            return new EditTimeEntryVM(appState);
        }

        public static EditTimeEntryVM ForExistingTimeEntry(AppState appState, Guid timeEntryId)
        {
            if (timeEntryId == Guid.Empty)
            {
                throw new Exception($"Do not create {nameof(EditTimeEntryVM)} with Guid.Empty. Use {nameof(ForManualAddition)} instead.");
            }

            return new EditTimeEntryVM(appState, timeEntryId);
        }

        private EditTimeEntryVM(AppState appState)
        {
            IsManual = true;

            richData = new RichTimeEntry(appState.GetTimeEntryDraft(), appState);
            UpdateView(x =>
            {
                var now = Time.UtcNow;
                x.Tags = new List<string>(richData.Data.Tags);
                x.StartTime = now;
                x.StopTime = now;
                x.State = TimeEntryState.Finished;
            });
            previousData = new RichTimeEntry(richData.Data.With(x => x.Tags = new List<string>()), appState);

            finishInit();
        }

        private EditTimeEntryVM(AppState appState, Guid timeEntryId)
        {
            richData = appState.TimeEntries[timeEntryId];
            previousData = new RichTimeEntry(richData.Data, richData.Info);
            // TODO Rx First ugly code or patch from
            // Unidirectional era. Why the DistinctUntilChanged doesn't
            // work correctly? We should manage RemoteId-LocalId in
            // a better way.
            subscriptionState = StoreManager
                                .Singleton
                                .Observe()
                                .Where(x => x.State.TimeEntries.ContainsKey(timeEntryId))
                                .Select(x => x.State.TimeEntries[timeEntryId])
                                .DistinctUntilChanged(x => x.Data.RemoteId)
                                .ObserveOn(SynchronizationContext.Current)
                                .Subscribe(syncedRichData =>
            {
                if (!richData.Data.RemoteId.HasValue && syncedRichData.Data.RemoteId.HasValue)
                {
                    UpdateView(x => x.RemoteId = syncedRichData.Data.RemoteId);
                }
            });

            finishInit();
        }

        private void finishInit()
        {
            subscriptionTimer = Observable.Timer(TimeSpan.FromMilliseconds(1000 - Time.Now.Millisecond),
                                                 TimeSpan.FromSeconds(1))
                                .ObserveOn(SynchronizationContext.Current)
                                .Subscribe(x => UpdateDuration());

            Observable.FromEventPattern<string>(h => DescriptionChanged += h, h => DescriptionChanged -= h)
            // Observe on the task pool to prevent locking UI
            .SubscribeOn(TaskPoolScheduler.Default)
            .Select(ev => ev.EventArgs)
            .Where(desc => desc != null && desc.Length >= LoadSuggestionsCharLimit)
            .Throttle(TimeSpan.FromMilliseconds(LoadSuggestionsThrottleMilliseconds))
            .Select(desc => LoadSuggestions(desc))
            // Go back to current context (UI thread)
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(result => SuggestionsCollection.Reset(result));

            ServiceContainer.Resolve<ITracker>().CurrentScreen = "Edit Time Entry";
        }

        public void Dispose()
        {
            if (subscriptionState != null)
                subscriptionState.Dispose();
            subscriptionTimer.Dispose();
        }

        #region viewModel State properties
        public bool IsManual { get; }

        public string Duration
        {
            get
            {
                return TimeEntryData.GetFormattedDuration(richData.Data.GetDuration());
            }
        }

        public DateTime StartDate
        {
            get
            {
                return richData.Data.StartTime == DateTime.MinValue
                       ? DateTime.UtcNow.AddMinutes(-1).ToLocalTime()
                       : richData.Data.StartTime.ToLocalTime();
            }
        }

        public DateTime StopDate
        {
            get
            {
                return richData.Data.StopTime.HasValue
                       ? richData.Data.StopTime.Value.ToLocalTime()
                       : DateTime.UtcNow.ToLocalTime();
            }
        }

        public string ProjectColorHex
        {
            get
            {
                return richData.Info.ProjectData.Id != Guid.Empty
                       ? ProjectData.HexColors[richData.Info.ProjectData.Color % ProjectData.HexColors.Length]
                       : ProjectData.HexColors[ProjectData.DefaultColor];
            }
        }

        public bool IsRunning { get { return richData.Data.State == TimeEntryState.Running; } }
        public string Description { get { return richData.Data.Description ?? string.Empty; } }
        public bool IsBillable { get { return richData.Data.IsBillable; } }
        public bool IsPremium { get { return richData.Info.WorkspaceData.IsPremium; } }
        public Guid WorkspaceId { get { return richData.Data.WorkspaceId; } }
        public string ProjectName { get { return richData.Info.ProjectData.Name ?? string.Empty; } }
        public string TaskName { get { return richData.Info.TaskData.Name ?? string.Empty; } }
        public string ClientName { get { return richData.Info.ClientData.Name ?? string.Empty; } }
        public List<string> Tags { get { return richData.Data.Tags.ToList(); } }
        public ObservableRangeCollection<ITimeEntryData> SuggestionsCollection { get; private set; } = new ObservableRangeCollection<ITimeEntryData>();
        #endregion

        public void ChangeTimeEntry(ITimeEntryData data)
        {
            // Clean suggestion list.
            SuggestionsCollection.Clear();

            UpdateView(x =>
            {
                x.Description = data.Description;
                x.ProjectRemoteId = data.ProjectRemoteId;
                x.ProjectId = data.ProjectId;
                x.WorkspaceId = data.WorkspaceId;
                x.WorkspaceRemoteId = data.WorkspaceRemoteId;
                x.IsBillable = data.IsBillable;
            }, nameof(Description) , nameof(ProjectName), nameof(ClientName), nameof(ProjectColorHex), nameof(IsPremium), nameof(IsBillable));
        }

        public void ChangeProjectAndTask(Guid projectId, Guid taskId)
        {
            if (projectId == richData.Data.ProjectId &&
                    taskId == richData.Data.TaskId)
            {
                return;
            }

            if (projectId != Guid.Empty)
            {
                SetNewProject(projectId);
                SetNewTask(taskId);
            }
            else
            {
                SetEmptyProject();
            }
        }

        public void ChangeTimeEntryDuration(TimeSpan newDuration)
        {
            UpdateView(x => x.SetDuration(newDuration, IsManual), nameof(Duration), nameof(StartDate), nameof(StopDate));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart(TimeSpan diffTime)
        {
            ChangeTimeEntryStart(richData.Data.StartTime + diffTime);
        }

        public void ChangeTimeEntryStart(DateTime newStartTime)
        {
            // Disable automatic update of StopTime as it confuses users (see #1593)
            //if (richData.Data.State != TimeEntryState.Running)
            //    UpdateView(x => x.ChangeStartTime(newStartTime), nameof(Duration), nameof(StartDate), nameof(StopDate));
            //else
            UpdateView(x => x.ChangeStartTime(newStartTime), nameof(Duration), nameof(StartDate));

            ServiceContainer.Resolve<ITracker>().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop(TimeSpan diffTime)
        {
            ChangeTimeEntryStop(richData.Data.StopTime.Value + diffTime);
        }

        public void ChangeTimeEntryStop(DateTime newStopTime)
        {
            UpdateView(x => x.ChangeStoptime(newStopTime), nameof(Duration), nameof(StopDate));

            ServiceContainer.Resolve<ITracker>().CurrentScreen = "Change Stop Time";
        }

        public void ChangeTagList(IEnumerable<string> newTags)
        {
            UpdateView(x => x.Tags = newTags.ToList(), nameof(Tags));
        }

        public void ChangeDescription(string description)
        {
            UpdateView(x => x.Description = description, nameof(Description));

            DescriptionChanged(this, description);
        }

        public void ChangeBillable(bool billable)
        {
            UpdateView(x => x.IsBillable = billable, nameof(IsBillable));
        }

        public void Delete()
        {
            RxChain.Send(new DataMsg.TimeEntriesRemove(richData.Data));
        }

        public bool Save()
        {
            if (HasTimeEntryChanged(previousData.Data, richData.Data))
            {
                // Check start and stop times
                var isStartStopTimeCorrect =
                    !richData.Data.StopTime.HasValue || richData.Data.StartTime <= richData.Data.StopTime.Value;

                if (isStartStopTimeCorrect)
                {
                    RxChain.Send(new DataMsg.TimeEntryPut(richData.Data, Tags));
                    previousData = richData;
                }

                return isStartStopTimeCorrect;
            }

            return true;
        }

        private void UpdateView(Action<TimeEntryData> updater, params string[] changedProperties)
        {
            richData = new RichTimeEntry(richData.Data.With(updater), StoreManager.Singleton.AppState);
            foreach (var prop in changedProperties)
            {
                RaisePropertyChanged(prop);
            }
        }

        private void UpdateDuration()
        {
            if (richData.Data.State == TimeEntryState.Running)
            {
                RaisePropertyChanged(nameof(Duration));
            }
        }

        private void SetNewProject(Guid projectId)
        {
            if (richData.Data.ProjectId == projectId)
            {
                return;
            }

            var projectData = StoreManager.Singleton.AppState.Projects [projectId];
            UpdateView(x =>
            {
                x.ProjectRemoteId = projectData.RemoteId;
                x.ProjectId = projectData.Id;
                x.WorkspaceId = projectData.WorkspaceId;
                x.WorkspaceRemoteId = projectData.WorkspaceRemoteId;
                x.IsBillable = projectData.IsBillable;
            }, nameof(ProjectName), nameof(ClientName), nameof(ProjectColorHex), nameof(IsPremium), nameof(IsBillable));
        }

        private void SetEmptyProject()
        {
            if (richData.Data.ProjectId == Guid.Empty)
            {
                return;
            }

            UpdateView(x =>
            {
                x.ProjectRemoteId = null;
                x.ProjectId = Guid.Empty;
                x.TaskId = Guid.Empty;
                x.TaskRemoteId = null;
            }, nameof(ProjectName), nameof(ClientName), nameof(ProjectColorHex));
        }

        private void SetNewTask(Guid taskId)
        {
            if (richData.Data.TaskId == taskId)
            {
                return;
            }

            var taskRemoteId = taskId != Guid.Empty ? StoreManager.Singleton.AppState.Tasks [taskId].RemoteId : null;
            UpdateView(x =>
            {
                x.TaskId = taskId;
                x.TaskRemoteId = taskRemoteId;
            }, nameof(TaskName));
        }

        private bool HasTimeEntryChanged(ITimeEntryData previous, ITimeEntryData current)
        {
            if (previous.StartTime != current.StartTime)
                return true;
            if (previous.StopTime != current.StopTime)
                return true;
            if (previous.IsBillable != current.IsBillable)
                return true;
            if (previous.ProjectId != current.ProjectId)
                return true;
            if (previous.Description != current.Description)
            {
                if (string.IsNullOrEmpty(previous.Description) &&
                        string.IsNullOrEmpty(current.Description))
                    return false;
                return true;
            }
            if (!previous.Tags.SequenceEqual(current.Tags))
                return true;
            if (previous.TaskId != current.TaskId)
                return true;
            return false;
        }

        private List<ITimeEntryData> LoadSuggestions(string description)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            return dataStore.Table<TimeEntryData>()
                   .OrderBy(r => r.StartTime)
                   .Where(r => r.State != TimeEntryState.New
                          && r.DeletedAt == null
                          && r.Description != null
                          && r.Description.Contains(description))
                   .Take(LoadSuggestionsResultsLimit)
                   .Cast<ITimeEntryData> ()
                   .ToList();
        }
    }
}
