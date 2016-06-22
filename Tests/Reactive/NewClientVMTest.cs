using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using XPlatUtils;
using Toggl.Phoebe.Analytics;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class NewClientVMTest : Test
    {
        NewClientVM viewModel;
        readonly ToggleClientMock togglClient = new ToggleClientMock();
        readonly NetworkSwitcher networkSwitcher = new NetworkSwitcher();

        public override void Init()
        {
            base.Init();

            var initState = Util.GetInitAppState();
            var platformUtils = new PlatformUtils();
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            ServiceContainer.RegisterScoped<ITracker> (new TrackerMock());
            ServiceContainer.RegisterScoped<INetworkPresence>(networkSwitcher);


            RxChain.Init(initState);
            viewModel = new NewClientVM(initState, Util.WorkspaceId);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            RxChain.Cleanup();
        }

        [Test]
        public async Task TestSaveClient()
        {
            var name = "MyClient";
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            networkSwitcher.SetNetworkConnection(false);

            IClientData client = await viewModel.SaveClientAsync(name);

            Assert.That(client = StoreManager.Singleton.AppState.Clients.Values.SingleOrDefault(
                                     x => x.WorkspaceId == Util.WorkspaceId && x.Name == name), Is.Not.Null);

            // Check item has been correctly saved in database
            Assert.That(dataStore.Table<ClientData> ().SingleOrDefault(
                            x => x.WorkspaceId == Util.WorkspaceId && x.Name == name && x.Id == client.Id), Is.Not.Null);
        }
    }
}

