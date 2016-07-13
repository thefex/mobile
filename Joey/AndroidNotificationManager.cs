using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Toggl.Joey.UI.Activities;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using XPlatUtils;
using NotificationCompat = Android.Support.V4.App.NotificationCompat;

namespace Toggl.Joey
{
    public class AndroidNotificationManager : IDisposable
    {
        private const int IdleNotifId = 40;
        private const int RunningNotifId = 42;
        private readonly Context ctx;
        private readonly NotificationManager notificationManager;
        private readonly string emptyProjectName;
        private readonly string emptyDescription;
        private readonly NotificationCompat.Builder runningBuilder;
        private readonly NotificationCompat.Builder idleBuilder;
        private IDisposable subscriptionTimeEntries, subscriptionSettings;
        private readonly SynchronizationContext uiContext;

        public AndroidNotificationManager()
        {
            uiContext = SynchronizationContext.Current;
            ctx = ServiceContainer.Resolve<Context>();
            notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
            runningBuilder = CreateRunningNotificationBuilder(ctx);
            idleBuilder = CreateIdleNotificationBuilder(ctx);
            emptyProjectName = ctx.Resources.GetString(Resource.String.RunningNotificationNoProject);
            emptyDescription = ctx.Resources.GetString(Resource.String.RunningNotificationNoDescription);

            // Detect running time entries in a reactive way.
            subscriptionTimeEntries = StoreManager
                                      .Singleton
                                      .Observe(x => x.State.TimeEntries)
                                      .ObserveOn(uiContext)
                                      .StartWith(StoreManager.Singleton.AppState.TimeEntries)
                                      .Select(timeEntries => timeEntries.Values.FirstOrDefault(e => e.Data.State == TimeEntryState.Running))
                                      .DistinctUntilChanged()
                                      .Subscribe(SyncNotifications);

            // Detect changes in settings in a super reactive way :)
            subscriptionSettings = StoreManager
                                   .Singleton
                                   .Observe(x => x.State.Settings)
            .DistinctUntilChanged(x => new { x.IdleNotification, x.RunningNotification })
            .SubscribeOn(TaskPoolScheduler.Default)
            .Subscribe(SyncNotifications);
        }

        public void Dispose()
        {
            subscriptionTimeEntries.Dispose();
            subscriptionSettings.Dispose();
        }

        private void SyncNotifications(SettingsState settings)
        {
            // Change notification state when setting changes.
            var runningEntry = StoreManager.Singleton.AppState.TimeEntries.FindActiveEntry();
            SetIdleNotification(runningEntry, settings.IdleNotification);
            SetRunningNotification(runningEntry, settings.RunningNotification);
        }

        private void SyncNotifications(RichTimeEntry runningEntry)
        {
            // Change notification state when running time entry changes.
            SetIdleNotification(runningEntry, StoreManager.Singleton.AppState.Settings.IdleNotification);
            SetRunningNotification(runningEntry, StoreManager.Singleton.AppState.Settings.RunningNotification);
        }

        private void SetRunningNotification(RichTimeEntry runningEntry, bool showNotification)
        {
            if (runningEntry != null && showNotification)
            {

                var proj = string.IsNullOrEmpty(runningEntry.Info.ProjectData.Name) ? emptyProjectName : runningEntry.Info.ProjectData.Name;
                var desc = string.IsNullOrEmpty(runningEntry.Data.Description) ? emptyDescription : runningEntry.Data.Description;
                runningBuilder
                .SetContentTitle(proj)
                .SetContentText(desc)
                .SetWhen((long)runningEntry.Data.StartTime.ToUnix().TotalMilliseconds);
                notificationManager.Notify(RunningNotifId, runningBuilder.Build());
            }
            else
            {
                notificationManager.Cancel(RunningNotifId);
            }
        }

        private void SetIdleNotification(RichTimeEntry runningEntry, bool showNotification)
        {
            if (runningEntry != null)
                notificationManager.Cancel(IdleNotifId);
            else if (showNotification)
                notificationManager.Notify(IdleNotifId, idleBuilder.Build());
            else
                notificationManager.Cancel(IdleNotifId);
        }

        private static NotificationCompat.Builder CreateRunningNotificationBuilder(Context ctx)
        {
            var res = ctx.Resources;

            var openIntent = new Intent(ctx, typeof(SplashActivity));
            openIntent.SetAction(Intent.ActionMain);
            openIntent.AddCategory(Intent.CategoryLauncher);
            var pendingOpenIntent = PendingIntent.GetActivity(ctx, 0, openIntent, 0);

            var stopIntent = new Intent(ctx, typeof(StopRunningTimeEntryService.Receiver));
            var pendingStopIntent = PendingIntent.GetBroadcast(ctx, 0, stopIntent, PendingIntentFlags.UpdateCurrent);

            return new NotificationCompat.Builder(ctx)
                   .SetAutoCancel(false)
                   .SetUsesChronometer(true)
                   .SetOngoing(true)
                   .SetSmallIcon(Resource.Drawable.IcNotificationIcon)
                   // TODO: Removed Stop button from notification until
                   // find a fiable solution
                   // .AddAction (Resource.Drawable.IcActionStop, res.GetString (Resource.String.RunningNotificationStopButton), pendingStopIntent)
                   // .AddAction (Resource.Drawable.IcActionEdit, res.GetString (Resource.String.RunningNotificationEditButton), editIntent)
                   .SetContentIntent(pendingOpenIntent);
        }

        private static NotificationCompat.Builder CreateIdleNotificationBuilder(Context ctx)
        {
            var res = ctx.Resources;

            var openIntent = new Intent(ctx, typeof(SplashActivity));
            openIntent.SetAction(Intent.ActionMain);
            openIntent.AddCategory(Intent.CategoryLauncher);
            var pendingOpenIntent = PendingIntent.GetActivity(ctx, 0, openIntent, 0);

            return new NotificationCompat.Builder(ctx)
                   .SetAutoCancel(false)
                   .SetOngoing(true)
                   .SetSmallIcon(Resource.Drawable.IcNotificationIconIdle)
                   .SetContentIntent(pendingOpenIntent)
                   .SetContentTitle(res.GetString(Resource.String.IdleNotificationTitle))
                   .SetContentText(res.GetString(Resource.String.IdleNotificationText));
        }
    }
}