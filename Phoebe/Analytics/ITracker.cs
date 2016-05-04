using System;

namespace Toggl.Phoebe.Analytics
{
    public interface ITracker
    {
        string CurrentScreen { set; }

        // Timing events
        void SendAppInitTime(TimeSpan duration);

        // Action events
        void SendSettingsChangeEvent(SettingName settingName);
        void SendAccountLoginEvent(AccountCredentials credentialsType);
        void SendAccountCreateEvent(AccountCredentials credentialsType);
        void SendAccountLogoutEvent();
        void SendTimerStartEvent(TimerStartSource startSource);
        void SendTimerStopEvent(TimerStopSource stopSource);

        // No user mode events
        void SendIntroModeEvent(UserMode mode);
        void SendToRegisterScreenEvent(RegisterSource source);
        void SendRegisterEvent(AccountCredentials credentialsType);
    }
}
