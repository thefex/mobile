using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.OS;
using Android.Support.Wearable.Views;
using Android.Util;
using Java.Interop;
using Java.Util.Concurrent;
using Toggl.Chandler.UI.Adapters;

namespace Toggl.Chandler.UI.Activities
{
    [Activity(Label = "Toggl", MainLauncher = true, Icon = "@drawable/Icon")]
    public class MainActivity : Activity, IDataApiDataListener, GoogleApiClient.IConnectionCallbacks, IMessageApiMessageListener
    {
        private const string Tag = "MainActivity";
        private GridViewPager ViewPager;
        private GoogleApiClient googleApiClient;
        private PagesAdapter adapter;
        private List<SimpleTimeEntryData> timeEntries = new List<SimpleTimeEntryData> ();
        private const int RebindTime = 4000;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            ViewPager = FindViewById<GridViewPager> (Resource.Id.pager);
            adapter = new PagesAdapter(this, FragmentManager);

            ViewPager.Adapter = adapter;

            googleApiClient = new GoogleApiClient.Builder(this)
            .AddApi(WearableClass.API)
            .AddConnectionCallbacks(this)
            .Build();
            googleApiClient.Connect();

            if (CollectionChanged != null)
            {
                CollectionChanged(this, EventArgs.Empty);
            }
        }

        public List<SimpleTimeEntryData> Data
        {
            get
            {
                return timeEntries;
            }
        }

        public event EventHandler CollectionChanged;

        protected override void OnResume()
        {
            base.OnResume();
            googleApiClient.Connect();
        }

        protected override void OnPause()
        {
            base.OnPause();
            WearableClass.DataApi.RemoveListener(googleApiClient, this);
            WearableClass.MessageApi.RemoveListener(googleApiClient, this);
            googleApiClient.Disconnect();
        }

        public void OnDataChanged(DataEventBuffer dataEvents)
        {
            if (!googleApiClient.IsConnected)
            {
                var connectionResult = googleApiClient.BlockingConnect(30, TimeUnit.Seconds);

                if (!connectionResult.IsSuccess)
                {
                    Log.Error(Tag, "DataLayerListenerService failed to connect to GoogleApiClient");
                    return;
                }
            }

            adapter.Timer.UserLoggedIn = true;
            foreach (var data in dataEvents)
            {
                if (data != null && data.Type == DataEvent.TypeChanged && data.DataItem.Uri.Path == Common.TimeEntryListPath)
                {
                    OnDataChanged(data.DataItem);
                }
            }

            if (CollectionChanged != null)
            {
                CollectionChanged(this, EventArgs.Empty);
            }
        }

        private void OnDataChanged(IDataItem dataItem)
        {
            var map = DataMapItem.FromDataItem(dataItem).DataMap;
            if (map.ContainsKey(Common.TimeEntryListKey))
            {
                var list = map.GetDataMapArrayList(Common.TimeEntryListKey);

                if (list.Count == 0)
                {
                    return;
                }

                timeEntries.Clear();
                foreach (var mapItem in list)
                {
                    var en = new SimpleTimeEntryData(mapItem);
                    timeEntries.Add(en);
                }
                adapter.Timer.TimerEnabled = true;
            }
        }

        public async Task RequestSync()
        {
            Exception error = null;
            do
            {
                try
                {
                    await SendMessage(Common.RequestSyncPath, new byte[0]);
                }
                catch (Exception tempEx)
                {
                    error = tempEx;
                    Log.Error(Tag, error.Message);
                }
                await Task.Delay(RebindTime);
            }
            while (error != null || Data.Count == 0);
        }

        public void RequestStartStop()
        {
            adapter.Timer.TimerEnabled = false;
            SendMessage(Common.StartStopTimeEntryPath, new byte[0]);
        }

        public void RequestStartEntry(string guid)
        {
            adapter.Timer.TimerEnabled = false;
            SendMessage(Common.ContinueTimeEntryPath, Common.GetBytes(guid));
            ViewPager.SetCurrentItem(0, 0, true);
        }

        public void RequestHandheldOpen()
        {
            SendMessage(Common.OpenHandheldPath, new byte[0]);
            ViewPager.SetCurrentItem(0, 0, true);
        }

        // Send messages to all client nodes in parallel
        private Task SendMessage(string message, byte[] data)
        {
            return Task.Run(() =>
            {
                foreach (var node in clientNodes)
                {
                    WearableClass.MessageApi.SendMessage(googleApiClient, node.Id, message, data);
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

        public void OnMessageReceived(IMessageEvent messageEvent)
        {
            if (messageEvent.Path == Common.UserNotLoggedInPath)
            {
                adapter.Timer.UserLoggedIn = false;
                Task.Run(() =>
                {
                    Task.Delay(RebindTime);
                    RequestSync();
                });
            }
        }

        public void OnConnected(Bundle bundle)
        {
            WearableClass.DataApi.AddListener(googleApiClient, this);
            WearableClass.MessageApi.AddListener(googleApiClient, this);
            Task.Run(() => { RequestSync(); });
        }

        public void OnConnectionSuspended(int cause)
        {
        }
    }
}
