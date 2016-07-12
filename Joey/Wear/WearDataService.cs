using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.Util;
using Java.Interop;
using Java.Util.Concurrent;
using Toggl.Joey.UI.Activities;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;

namespace Toggl.Joey.Wear
{
    /// <summary>
    /// Listens to DataItems and Messages from the local node
    /// </summary>
    [Service, IntentFilter(new [] { "com.google.android.gms.wearable.BIND_LISTENER" }) ]
    public class WearDataService : WearableListenerService,  GoogleApiClient.IConnectionCallbacks
    {
        public const string Tag = "WearableTag";
        public const string DataStorePath = "/TimeEntryDataStore";
        private GoogleApiClient googleApiClient;
        private List<SimpleTimeEntryData> entryData;
        private PutDataMapRequest mapReq;
        private List<DataMap> currentDataMap;
        private SynchronizationContext uiContext;
        private IDisposable subscriptionTimeEntries;

        public WearDataService()
        {
        }

        public WearDataService(Context ctx)
        {
            Init(ctx);
        }

        private void Init(Context ctx)
        {
            googleApiClient = new GoogleApiClient.Builder(ctx)
            .AddApi(WearableClass.API)
            .Build();
            googleApiClient.Connect();
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Init(this);
            // Detect running time entries in a reactive way.
            subscriptionTimeEntries = StoreManager
                                      .Singleton
                                      .Observe(x => x.State.TimeEntries)
                                      .ObserveOn(SynchronizationContext.Current)
                                      .StartWith(StoreManager.Singleton.AppState.TimeEntries)
                                      .Select(timeEntries => timeEntries.Values.FirstOrDefault(e => e.Data.State == TimeEntryState.Running))
                                      .DistinctUntilChanged()
                                      .Subscribe(SyncState);
        }

        private void SyncState(RichTimeEntry runningEntry)
        {
            Task.Run(() => UpdateSharedTimeEntryList());
        }

        public override void OnDataChanged(DataEventBuffer dataEvents)
        {
            if (!googleApiClient.IsConnected)
            {
                ConnectionResult connectionResult = googleApiClient.BlockingConnect(30, TimeUnit.Seconds);
                if (!connectionResult.IsSuccess)
                {
                    Log.Error(Tag, "DataLayerListenerService failed to connect to GoogleApiClient");
                    return;
                }
            }
        }

        public async void OnConnected(Android.OS.Bundle connectionHint)
        {
            Log.Info("WearIntegration", "Connected");
            await UpdateSharedTimeEntryList();
        }

        public void OnConnectionSuspended(int cause)
        {
        }

        public async override void OnMessageReceived(IMessageEvent messageEvent)
        {
            LOGD(Tag, "OnMessageReceived: " + messageEvent);
            await HandleMessage(messageEvent);
            base.OnMessageReceived(messageEvent);
        }

        public override void OnPeerConnected(INode peer)
        {
            LOGD(Tag, "OnPeerConnected: " + peer);
        }

        public override void OnPeerDisconnected(INode peer)
        {
            LOGD(Tag, "OnPeerDisconnected: " + peer);
        }

        private async Task HandleMessage(IMessageEvent message)
        {
            try
            {
                Log.Info("WearIntegration", "Received Message");

                googleApiClient.Connect();
                if (!googleApiClient.IsConnected)
                {
                    Log.Info("WearIntegration", "Connecting");
                }

                var path = message.Path;

                try
                {
                    if (path == Common.StartStopTimeEntryPath)
                    {

                        WearDataProvider.StartStopTimeEntry(BaseContext);
                        await UpdateSharedTimeEntryList();
                    }
                    else if (path == Common.ContinueTimeEntryPath)
                    {

                        var guid = Guid.Parse(Common.GetString(message.GetData()));
                        StartEntry(guid);
                    }
                    else if (path == Common.RequestSyncPath)
                    {
                        Log.Info("WearIntegration", "Sending sync data!");

                        await UpdateSharedTimeEntryList();
                    }
                    else if (path == Common.OpenHandheldPath)
                    {

                        StartMainActivity();
                    }
                }
                catch (Exception e)
                {
                    Log.Error("WearIntegration", e.ToString());
                }
            }
            catch (Exception e)
            {
                Log.Error("WearIntegration", e.ToString());
            }
        }

        private void NotifyNotLoggedIn()
        {
            Task.Run(() =>
            {
                var apiResult = WearableClass.NodeApi.GetConnectedNodes(googleApiClient) .Await().JavaCast<INodeApiGetConnectedNodesResult> ();
                var nodes = apiResult.Nodes;
                foreach (var node in nodes)
                {
                    WearableClass.MessageApi.SendMessage(googleApiClient, node.Id,
                                                         Common.UserNotLoggedInPath,
                                                         new byte[0]);
                }
            });
        }

        private void StartMainActivity()
        {
            var intent = new Intent(this, typeof(MainDrawerActivity));
            intent.AddFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private async Task StartEntry(Guid id)
        {
            WearDataProvider.ContinueTimeEntry(id);
            await Task.Delay(1000);
            await UpdateSharedTimeEntryList();
        }

        public async Task UpdateSharedTimeEntryList()
        {
            entryData = WearDataProvider.GetTimeEntryData();

            mapReq = PutDataMapRequest.Create(Common.TimeEntryListPath).SetUrgent();

            currentDataMap = new List<DataMap> ();

            foreach (var entry in entryData)
            {
                currentDataMap.Add(entry.DataMap);
            }
            mapReq.DataMap.PutDataMapArrayList(Common.TimeEntryListKey, currentDataMap);
            await SendData(mapReq);
        }

        private Task SendData(PutDataMapRequest mapReq)
        {
            return Task.Run(() =>
            {
                foreach (var node in clientNodes)
                {
                    WearableClass.DataApi.PutDataItem(googleApiClient, mapReq.AsPutDataRequest().SetUrgent());
                }
            });
        }

        private IList<INode> clientNodes
        {
            get
            {
                return WearableClass
                       .NodeApi
                       .GetConnectedNodes(googleApiClient)
                       .Await()
                       .JavaCast<INodeApiGetConnectedNodesResult> ()
                       .Nodes;
            }
        }

        public static void LOGD(string tag, string message)
        {
            if (Log.IsLoggable(tag, LogPriority.Debug))
            {
                Log.Debug(tag, message);
            }
        }
    }
}