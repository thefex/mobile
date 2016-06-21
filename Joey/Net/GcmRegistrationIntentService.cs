using System;
using Android.App;
using Android.Content;
using Android.Gms.Gcm;
using Android.Gms.Gcm.Iid;
using Toggl.Phoebe;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Data;
using Android.Util;

namespace Toggl.Joey.Net
{
    // This intent service receives the registration token from GCM:
    [Service(Exported = false)]
    class GcmRegistrationIntentService : IntentService
    {
        // Notification topics that I subscribe to:
        static readonly string[] Topics = { "global" };

        // Create the IntentService, name the worker thread for debugging purposes:
        public GcmRegistrationIntentService() : base("RegistrationIntentService")
        { }

        // OnHandleIntent is invoked on a worker thread:
        protected override void OnHandleIntent(Intent intent)
        {
            try
            {
                Log.Info("RegistrationIntentService", "Calling InstanceID.GetToken");

                // Ensure that the request is atomic:
                lock (this)
                {
                    // Request a registration token:
                    var instanceID = InstanceID.GetInstance(this);
                    var token = instanceID.GetToken(Build.GcmSenderId,
                                                    GoogleCloudMessaging.InstanceIdScope, null);

                    // Log the registration token that was returned from GCM:
                    Log.Info("RegistrationIntentService", "GCM Registration Token: " + token);

                    // Send to the app server (if it requires it):
                    SendRegistrationToAppServer(token);

                    // Subscribe to receive notifications:
                    SubscribeToTopics(token, Topics);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("RegistrationIntentService", "Failed to get a registration token: " + ex.Message);
            }
        }

        void SendRegistrationToAppServer(string token)
        {
            try
            {
                RxChain.Send(new DataMsg.RegisterPush(token));
            }
            catch (Exception ex)
            {
                Log.Warn("RegistrationIntentService", "Failed to send registration token: " + ex.Message);
            }
        }

        void SubscribeToTopics(string token, string[] topics)
        {
            foreach (var topic in topics)
            {
                var pubSub = GcmPubSub.GetInstance(this);
                pubSub.Subscribe(token, "/topics/" + topic, null);
            }
        }
    }
}
