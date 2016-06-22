using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using XPlatUtils;
using Toggl.Phoebe.Analytics;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class NewTagVMTest : Test
    {
        NewTagVM viewModel;
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
            viewModel = new NewTagVM(initState, Util.WorkspaceId);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            RxChain.Cleanup();
        }

        [Test]
        public async Task TestSaveTag()
        {
            var name = "MyTag";
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            networkSwitcher.SetNetworkConnection(false);

            ITagData tag =  await viewModel.SaveTagAsync(name);
            Assert.That(tag = StoreManager.Singleton.AppState.Tags.Values.SingleOrDefault(
                                  x => x.WorkspaceId == Util.WorkspaceId && x.Name == name), Is.Not.Null);

            // Check item has been correctly saved in database
            Assert.That(dataStore.Table<TagData> ().SingleOrDefault(
                            x => x.WorkspaceId == Util.WorkspaceId && x.Name == name && x.Id == tag.Id), Is.Not.Null);
        }
    }
}

