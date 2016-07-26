using System;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;


namespace Toggl.Joey.Widget
{
    [BroadcastReceiver(Label = "@string/WidgetName")]
    [IntentFilter(new  [] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/widget_info")]

    public class WidgetProvider : AppWidgetProvider
    {
        public const string TimeEntryIdParameter = "entryId";
        public const string StartAction = "com.toggl.timer.widget.START_ENTRY";
        public const string StopAction = "com.toggl.timer.widget.STOP_ENTRY";
        public const string ContinueAction = "com.toggl.timer.widget.CONTINUE_ENTRY";
        public const string RefreshEntryListAction = "com.toggl.timer.widget.REFRESH_CONTENT";
        public const string RefreshStartStopAction = "com.toggl.timer.widget.REFRESH_COMPLETE";
        private const string ThreadWorkerName = "com.toggl.timer.widgetprovider";

        private static HandlerThread workerThread;
        private static Handler workerQueue;

        public WidgetProvider()
        {
            // Start the worker thread
            workerThread = new HandlerThread(ThreadWorkerName);
            workerThread.Start();
            workerQueue = new Handler(workerThread.Looper);
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            // Run update service
            var serviceIntent = new Intent(context, typeof(WidgetUpdateService));
            context.StartService(serviceIntent);

            var wm = AppWidgetManager.GetInstance(context);
            var cn = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetProvider)));
            var ids = wm.GetAppWidgetIds(cn);
            var views = new RemoteViews(context.PackageName, Resource.Layout.keyguard_widget);

            // Set correct adapter.
            var adapterServiceIntent = new Intent(context, typeof(RemotesViewsFactoryService));
            adapterServiceIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            adapterServiceIntent.SetData(Android.Net.Uri.Parse(adapterServiceIntent.ToUri(IntentUriType.Scheme)));
            views.SetRemoteAdapter(Resource.Id.WidgetRecentEntriesListView, adapterServiceIntent);

            var listItemIntent = new Intent(context, typeof(WidgetStartStopService.Receiver));
            listItemIntent.SetData(Android.Net.Uri.Parse(listItemIntent.ToUri(IntentUriType.Scheme)));

            var pendingIntent = PendingIntent.GetBroadcast(context, 0, listItemIntent, PendingIntentFlags.UpdateCurrent);
            views.SetPendingIntentTemplate(Resource.Id.WidgetRecentEntriesListView, pendingIntent);

            // Update widget view.
            wm.UpdateAppWidget(ids, views);

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;

            if (action == RefreshEntryListAction)
            {
                ScheduleUpdate(context, action);
            }

            if (action == RefreshStartStopAction)
            {
                ScheduleUpdate(context, action);
            }

            base.OnReceive(context, intent);
        }

        private void ScheduleUpdate(Context ctx, string action)
        {
            // Adds a runnable to update the widgets in the worker queue.
            workerQueue.RemoveMessages(0);
            workerQueue.Post(() =>
            {

                var wm = AppWidgetManager.GetInstance(ctx);
                var cn = new ComponentName(ctx, Java.Lang.Class.FromType(typeof(WidgetProvider)));
                var ids = wm.GetAppWidgetIds(cn);

                if (action == RefreshStartStopAction)
                {
                    var serviceIntent = new Intent(ctx, typeof(WidgetUpdateService));
                    ctx.StartService(serviceIntent);
                }
                else
                {
                    var views = new RemoteViews(ctx.PackageName, Resource.Layout.keyguard_widget);
                    wm.PartiallyUpdateAppWidget(ids, views);
                    wm.NotifyAppWidgetViewDataChanged(ids, Resource.Id.WidgetRecentEntriesListView);
                }
            });
        }

        // Static methods to refresh widget from outside!
        public static void RefreshWidget(Context ctx, string action)
        {
            // Sends a request to the rich push message to refresh.
            RefreshWidget(ctx, action, 0);
        }

        public static void RefreshWidget(Context ctx, string action, long delayInMs)
        {
            //Sends a request to the rich push message to refresh with a delay.
            var refreshIntent = new Intent(ctx, typeof(WidgetProvider));
            refreshIntent.SetAction(action);

            if (delayInMs > 0)
            {
                PendingIntent pendingIntent = PendingIntent.GetBroadcast(ctx, 0, refreshIntent, 0);
                var am = (AlarmManager) ctx.GetSystemService(Context.AlarmService);
                am.Set(AlarmType.RtcWakeup, (long) new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds + delayInMs, pendingIntent);
            }
            else
            {
                ctx.SendBroadcast(refreshIntent);
            }
        }
    }
}
