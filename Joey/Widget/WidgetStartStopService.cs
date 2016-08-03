using System;
using Android.App;
using Android.Content;
using Android.Support.V4.Content;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Reactive;
using XPlatUtils;

namespace Toggl.Joey.Widget
{
    [Service(Exported = false)]
    public sealed class WidgetStartStopService : Service
    {
        public WidgetStartStopService()
        {
        }

        public WidgetStartStopService(IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base(javaRef, transfer)
        {
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            try
            {
                var action = intent.Action;
                var state = StoreManager.Singleton.AppState;

                // Get entry Id string.
                var entryId = intent.GetStringExtra(WidgetProvider.TimeEntryIdParameter);

                RichTimeEntry entry;
                Guid entryGuid;
                Guid.TryParse(entryId, out entryGuid);

                switch (action)
                {
                    case WidgetProvider.StartAction:
                        RxChain.Send(new DataMsg.TimeEntryStart());
                        ServiceContainer.Resolve<ITracker>().SendTimerStartEvent(TimerStartSource.WidgetStart);
                        break;
                    case WidgetProvider.StopAction:
                        entry = state.TimeEntries[entryGuid];
                        RxChain.Send(new DataMsg.TimeEntryStop(entry.Data));
                        ServiceContainer.Resolve<ITracker>().SendTimerStopEvent(TimerStopSource.Widget);
                        break;
                    case WidgetProvider.ContinueAction:
                        entry = state.TimeEntries[entryGuid];
                        RxChain.Send(new DataMsg.TimeEntryContinue(entry.Data));
                        break;
                }
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.Resolve<ILogger>();
                logger.Error(nameof(WidgetStartStopService), ex, "Error login stopping a TimeEntry from the widget.");
            }
            finally
            {
                WakefulBroadcastReceiver.CompleteWakefulIntent(intent);
                StopSelf(startId);
            }

            return StartCommandResult.Sticky;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            ((AndroidApp)Application).InitializeComponents();
        }

        public override Android.OS.IBinder OnBind(Intent intent)
        {
            return null;
        }

        [BroadcastReceiver(Exported = true)]
        public sealed class Receiver : WakefulBroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                var serviceIntent = new Intent(context, typeof(WidgetStartStopService));
                serviceIntent.SetAction(intent.Action);
                serviceIntent.PutExtra(WidgetProvider.TimeEntryIdParameter, intent.GetStringExtra(WidgetProvider.TimeEntryIdParameter));
                StartWakefulService(context, serviceIntent);
            }
        }
    }
}