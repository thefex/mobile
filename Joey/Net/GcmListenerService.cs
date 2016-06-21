using Android.App;
using Android.Content;
using Android.OS;
using Android.Gms.Gcm;
using System;
using Android.Util;

namespace Toggl.Joey.Net
{
    [Service(Exported = false), IntentFilter(new [] { "com.google.android.c2dm.intent.RECEIVE" })]
    public class TogglGcmListenerService : GcmListenerService
    {
        public override void OnMessageReceived(string from, Bundle data)
        {
            try
            {
                Phoebe.Reactive.RxChain.Send(new Phoebe.Data.ServerRequest.GetChanges());
            }
            catch (Exception ex)
            {
                Log.Warn("TogglGcmListenerService", "Failed to trigger sync: " + ex.Message);
            }
        }
    }
}