using System;

namespace Toggl.Phoebe.Analytics
{
    public abstract class BaseTracker : ITracker, IDisposable
    {
        private Plan userPlan;

        public BaseTracker()
        {
            //var experiment = ServiceContainer.Resolve<ExperimentManager> ().CurrentExperiment;
            //CurrentExperiment = experiment != null ? experiment.Id : null;
            CurrentExperiment = null;
        }

        ~BaseTracker()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }

        public void SendAppInitTime(TimeSpan duration)
        {
            SendTiming((long)duration.TotalMilliseconds, "App", "Init", null);
        }

        public void SendSettingsChangeEvent(SettingName settingName)
        {
            string label;

            switch (settingName)
            {
                case SettingName.AskForProject:
                    label = "AskForProject";
                    break;
                case SettingName.IdleNotification:
                    label = "IdleNotification";
                    break;
                case SettingName.DefaultMobileTag:
                    label = "DefaultMobileTag";
                    break;
                case SettingName.GroupedTimeEntries:
                    label = "GroupedTimeEntries";
                    break;
                case SettingName.ShowNotification:
                    label = "ShowNotification";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "settingName");
#else
                    return;
#endif
            }

            SendEvent("Settings", "Change", label);
        }

        public void SendAccountLoginEvent(AccountCredentials credentialsType)
        {
            string label;

            switch (credentialsType)
            {
                case AccountCredentials.Password:
                    label = "Password";
                    break;
                case AccountCredentials.Google:
                    label = "Google";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "credentialsType");
#else
                    return;
#endif
            }

            SendEvent("Account", "Login", label);
        }

        public void SendAccountCreateEvent(AccountCredentials credentialsType)
        {
            string label;

            switch (credentialsType)
            {
                case AccountCredentials.Password:
                    label = "Password";
                    break;
                case AccountCredentials.Google:
                    label = "Google";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "credentialsType");
#else
                    return;
#endif
            }

            SendEvent("Account", "Signup", label);
        }

        public void SendAccountLogoutEvent()
        {
            SendEvent("Account", "Logout");
        }

        public void SendTimerStartEvent(TimerStartSource startSource)
        {
            string label;

            switch (startSource)
            {
                case TimerStartSource.AppNew:
                    label = "In-app (new)";
                    break;
                case TimerStartSource.AppContinue:
                    label = "In-app (continue)";
                    break;
                case TimerStartSource.AppManual:
                    label = "In-app (manual)";
                    break;
                case TimerStartSource.WidgetStart:
                    label = "Widget (new)";
                    break;
                case TimerStartSource.WidgetNew:
                    label = "Widget (continue)";
                    break;
                case TimerStartSource.WatchStart:
                    label = "Watch (new)";
                    break;
                case TimerStartSource.WatchContinue:
                    label = "Watch (continue)";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "startSource");
#else
                    return;
#endif
            }

            SendEvent("Timer", "Start", label);
        }

        public void SendTimerStopEvent(TimerStopSource stopSource)
        {
            string label;

            switch (stopSource)
            {
                case TimerStopSource.App:
                    label = "In-app";
                    break;
                case TimerStopSource.Notification:
                    label = "Notification";
                    break;
                case TimerStopSource.Widget:
                    label = "Widget";
                    break;
                case TimerStopSource.Watch:
                    label = "Watch";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "stopSource");
#else
                    return;
#endif
            }

            SendEvent("Timer", "Stop", label);
        }

        private string CurrentExperiment
        {
            set { SetCustomDimension(Build.GoogleAnalyticsExperimentIndex, value); }
        }

        private Plan UserPlan
        {
            set
            {
                // Don't set the custom dimensions when it hasn't changed (except for null value)
                if (value != Plan.None && value == userPlan)
                {
                    return;
                }

                string planName;
                switch (value)
                {
                    case Plan.None:
                        planName = null;
                        break;
                    case Plan.Free:
                        planName = "Free";
                        break;
                    case Plan.Pro:
                        planName = "Pro";
                        break;
                    default:
#if DEBUG
                        throw new ArgumentException("Invalid value", "value");
#else
                        return;
#endif
                }

                userPlan = value;
                SetCustomDimension(Build.GoogleAnalyticsPlanIndex, planName);
            }
        }

        #region no-user-mode events

        public void SendIntroModeEvent(UserMode mode)
        {
            string label;
            switch (mode)
            {
                case UserMode.NormalMode:
                    label = "Normal";
                    break;
                case UserMode.NoUserMode:
                    label = "NoUser";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "userMode");
#else
                    return;
#endif
            }

            SendEvent("NoUser", "IntroRegister", label);
        }

        public void SendToRegisterScreenEvent(RegisterSource source)
        {
            string label;
            switch (source)
            {
                case RegisterSource.Feedback:
                    label = "Feedback";
                    break;
                case RegisterSource.Reports:
                    label = "Reports";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "registerSource");
#else
                    return;
#endif
            }

            SendEvent("NoUser", "ToRegisterScreen", label);
        }

        public void SendRegisterEvent(AccountCredentials credentialsType)
        {
            string label;

            switch (credentialsType)
            {
                case AccountCredentials.Password:
                    label = "Password";
                    break;
                case AccountCredentials.Google:
                    label = "Google";
                    break;
                default:
#if DEBUG
                    throw new ArgumentException("Invalid value", "credentialsType");
#else
                    return;
#endif
            }

            SendEvent("NoUser", "RegisterNoUser", label);
        }

        #endregion

        public abstract string CurrentScreen { set; }

        protected abstract void StartNewSession();
        protected abstract void SendTiming(long elapsedMilliseconds, string category, string variable, string label = null);
        protected abstract void SendEvent(string category, string action, string label = null, long value = 0);
        protected abstract void SetCustomDimension(int idx, string value);

        // TODO RX restore services correctly.
        /*
        private void OnAuthChanged (AuthChangedMessage msg)
        {
            // Start a new session whenever the user changes, exception being signup where the user just created an account
            if (msg.Reason != AuthChangeReason.Signup) {
                UserPlan = Plan.None;
                StartNewSession ();
            }
        }


        private async void OnSyncFinished (SyncFinishedMessage msg)
        {
            // Check if the user has access to any premium workspaces
            var store = ServiceContainer.Resolve<IDataStore> ();
            var numPremium = await store.Table<WorkspaceData> ()
                             .Where (r => r.IsPremium).CountAsync();

            UserPlan = numPremium > 0 ? Plan.Pro : Plan.Free;
        }
        */

        private void OnExperimentChanged(ExperimentChangedMessage msg)
        {
            var experiment = msg.CurrentExperiment;
            CurrentExperiment = experiment != null ? experiment.Id : null;
        }

        private enum Plan
        {
            None,
            Free,
            Pro
        }
    }
}
