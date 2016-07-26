using System;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;

namespace Toggl.Joey.Widget
{
    [Service(Exported = false)]
    public class WidgetUpdateService : Service
    {
        public WidgetUpdateService()
        {
        }

        public WidgetUpdateService(IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base(javaRef, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            SetupWidget(this, StoreManager.Singleton.AppState);
            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        private void SetupWidget(Context ctx, AppState state)
        {
            var wm = AppWidgetManager.GetInstance(ctx);
            var cn = new ComponentName(ctx, Java.Lang.Class.FromType(typeof(WidgetProvider)));
            var ids = wm.GetAppWidgetIds(cn);
            var views = new RemoteViews(ctx.PackageName, Resource.Layout.keyguard_widget);

            // Set top button
            var runningEntry = state.TimeEntries.FindActiveEntry();
            SetupRunningBtn(ctx, views, runningEntry);

            // Update widget view.
            wm.UpdateAppWidget(ids, views);
        }

        private void SetupRunningBtn(Context ctx, RemoteViews views, RichTimeEntry runningEntry)
        {
            var baseTime = SystemClock.ElapsedRealtime();

            if (runningEntry != null)
            {
                var color = ProjectData.HexColors[runningEntry.Info.ProjectData.Color];
                views.SetInt(Resource.Id.WidgetActionButton, "setBackgroundColor", ctx.Resources.GetColor(Resource.Color.bright_red));
                views.SetInt(Resource.Id.WidgetActionButton, "setText", Resource.String.TimerStopButtonText);
                views.SetInt(Resource.Id.WidgetColorView, "setColorFilter", Color.ParseColor(color));
                views.SetViewVisibility(Resource.Id.WidgetRunningEntry, ViewStates.Visible);
                var title = string.IsNullOrWhiteSpace(runningEntry.Data.Description) ?
                            ctx.Resources.GetString(Resource.String.RunningWidgetNoDescription) :
                            runningEntry.Data.Description;
                views.SetTextViewText(Resource.Id.WidgetRunningDescriptionTextView, title);

                var duration = runningEntry.Data.GetDuration();
                var time = (long)duration.TotalMilliseconds;

                // Format chronometer correctly.
                string format = "00:%s";
                if (time >= 3600000 && time < 36000000)
                    format = "0%s";
                else if (time >= 36000000)
                    format = "%s";

                views.SetChronometer(Resource.Id.Chronometer, baseTime - time, format, true);
                // Set button event.
                views.SetOnClickPendingIntent(Resource.Id.WidgetActionButton, StopEntryIntent(ctx, runningEntry.Data.Id));
            }
            else
            {
                views.SetInt(Resource.Id.WidgetActionButton, "setBackgroundColor", ctx.Resources.GetColor(Resource.Color.bright_green));
                views.SetInt(Resource.Id.WidgetActionButton, "setText", Resource.String.TimerStartButtonText);
                views.SetViewVisibility(Resource.Id.WidgetRunningEntry, ViewStates.Invisible);
                views.SetChronometer(Resource.Id.Chronometer, baseTime, "00:%s", false);
                views.SetTextViewText(Resource.Id.Chronometer, "00:00:00");
                views.SetOnClickPendingIntent(Resource.Id.WidgetActionButton, StartEntryIntent(ctx));

            }
        }

        private PendingIntent StopEntryIntent(Context ctx, Guid entryId)
        {
            var intent = new Intent(ctx, typeof(WidgetStartStopService.Receiver));
            intent.SetAction(WidgetProvider.StopAction);
            intent.PutExtra(WidgetProvider.TimeEntryIdParameter, entryId.ToString());
            return PendingIntent.GetBroadcast(ctx, 0, intent, PendingIntentFlags.UpdateCurrent);
        }

        private PendingIntent StartEntryIntent(Context ctx)
        {
            var intent = new Intent(ctx, typeof(WidgetStartStopService.Receiver));
            intent.SetAction(WidgetProvider.StartAction);
            return PendingIntent.GetBroadcast(ctx, 0, intent, PendingIntentFlags.UpdateCurrent);
        }
    }
}