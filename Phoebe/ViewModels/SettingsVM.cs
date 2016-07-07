using System;
using System.Reactive.Linq;
using System.Threading;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    [ImplementPropertyChanged]
    public class SettingsVM : ViewModelBase, IDisposable
    {
        private readonly IDisposable subscription;

        public SettingsVM(AppState state)
        {
            subscription = StoreManager.Singleton
                           .Observe(x => x.State.Settings)
                           .StartWith(state.Settings)
                           .DistinctUntilChanged()
                           .ObserveOn(SynchronizationContext.Current)
                           .Subscribe(stateUpdated);

            ServiceContainer.Resolve<ITracker>().CurrentScreen = "Settings";
        }

        #region exposed UI properties

        public bool RunningNotification { get; private set; }
        public bool IdleNotification { get; private set; }
        public bool ChooseProjectForNew { get; private set; }
        public bool UseDefaultTag { get; private set; }
        public bool GroupedTimeEntries { get; private set; }

        #endregion

        #region public methods

        public void SetRunningNotification(bool value)
        {
            setSetting(nameof(SettingsState.RunningNotification), value);
        }
        public void SetIdleNotification(bool value)
        {
            setSetting(nameof(SettingsState.IdleNotification), value);
        }
        public void SetChooseProjectForNew(bool value)
        {
            setSetting(nameof(SettingsState.ChooseProjectForNew), value);
        }
        public void SetUseDefaultTag(bool value)
        {
            setSetting(nameof(SettingsState.UseDefaultTag), value);
        }
        public void SetGroupedTimeEntries(bool value)
        {
            setSetting(nameof(SettingsState.GroupedEntries), value);
        }

        #endregion

        private void stateUpdated(SettingsState settings)
        {
            RunningNotification = settings.RunningNotification;
            IdleNotification = settings.IdleNotification;
            ChooseProjectForNew = settings.ChooseProjectForNew;
            UseDefaultTag = settings.UseDefaultTag;
            GroupedTimeEntries = settings.GroupedEntries;
        }

        private static void setSetting(string propertyName, object value)
        {
            RxChain.Send(new DataMsg.UpdateSetting(propertyName, value));
        }

        public void Dispose()
        {
            subscription.Dispose();
        }
    }
}

