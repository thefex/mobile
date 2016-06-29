using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.Analytics;
using XPlatUtils;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class LoginVMTest : Test
    {
        NetworkSwitcher networkSwitcher;
        LoginVM viewModel;
        readonly ToggleClientMock togglClient = new ToggleClientMock();
        readonly PlatformUtils platformUtils = new PlatformUtils();

        public override void Init()
        {
            base.Init();

            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            ServiceContainer.RegisterScoped<ITracker> (new TrackerMock());
            networkSwitcher = new NetworkSwitcher();
            ServiceContainer.RegisterScoped<INetworkPresence> (networkSwitcher);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var initState = Util.GetInitAppState();
            RxChain.Init(initState);
            viewModel = new LoginVM();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            viewModel.Dispose();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            dataStore.WipeTables();
            RxChain.Cleanup();
        }

        [Test]
        public void TestLoginEmailPassword()
        {
            // Set state as connected.
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            networkSwitcher.SetNetworkConnection(true);

            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.AuthResult))
                {
                    // Correct login.
                    Assert.That(viewModel.AuthResult, Is.EqualTo(AuthResult.Success));
                    Assert.That(StoreManager.Singleton.AppState.User.Email, Is.EqualTo(ToggleClientMock.fakeUserEmail));

                    // Check item has been correctly saved in database
                    Assert.That(dataStore.Table<UserData> ().SingleOrDefault(
                                    x => x.Email == ToggleClientMock.fakeUserEmail), Is.Not.Null);
                }
            };

            // None state.
            Assert.That(viewModel.AuthResult, Is.EqualTo(AuthResult.None));
            viewModel.TryLogin(ToggleClientMock.fakeUserEmail, ToggleClientMock.fakeUserPassword);
        }

        [Test]
        public void TestLoginGoogleToken()
        {
            // Set state as connected.
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            networkSwitcher.SetNetworkConnection(true);

            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.AuthResult))
                {
                    // Correct login.
                    Assert.That(viewModel.AuthResult, Is.EqualTo(AuthResult.Success));
                    Assert.That(StoreManager.Singleton.AppState.User.Email, Is.EqualTo(ToggleClientMock.fakeUserEmail));

                    // Check item has been correctly saved in database
                    Assert.That(dataStore.Table<UserData> ().SingleOrDefault(
                                    x => x.Email == ToggleClientMock.fakeUserEmail), Is.Not.Null);
                }
            };

            // None state.
            Assert.That(viewModel.AuthResult, Is.EqualTo(AuthResult.None));
            viewModel.TryLoginWithGoogle(ToggleClientMock.fakeGoogleId);
        }

        [Test]
        public void TestLoginWrongEmailPassword()
        {
            // Set state as connected.
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            networkSwitcher.SetNetworkConnection(true);
            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.AuthResult))
                {
                    // Correct login.
                    Assert.That(viewModel.AuthResult, Is.EqualTo(AuthResult.SystemError));
                    Assert.That(StoreManager.Singleton.AppState.User.Email, Is.EqualTo(string.Empty));

                    // Check item has been correctly saved in database
                    Assert.That(dataStore.Table<UserData> ().SingleOrDefault(
                                    x => x.Email == ToggleClientMock.fakeUserEmail), Is.Null);
                }
            };
            viewModel.TryLogin(ToggleClientMock.fakeUserEmail, ToggleClientMock.fakeUserPassword + "_");
        }

        [Test]
        public void TestLoginWrongGoogleToken()
        {
            // Set state as connected.
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            networkSwitcher.SetNetworkConnection(true);
            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.AuthResult))
                {
                    // Not Google account.
                    Assert.That(viewModel.AuthResult, Is.EqualTo(AuthResult.NoGoogleAccount));
                    Assert.That(StoreManager.Singleton.AppState.User, Is.Null);

                    // Nothing in DB.
                    Assert.That(dataStore.Table<UserData> ().SingleOrDefault(
                                    x => x.Email == ToggleClientMock.fakeUserEmail), Is.Null);
                }
            };

            // None state.
            viewModel.TryLoginWithGoogle(ToggleClientMock.fakeGoogleId + "__");
        }

        [Test]
        public async Task TestRegisterAfterActivity()
        {
            var registerTcs = Util.CreateTask<bool> ();
            var getStateTcs = Util.CreateTask<bool> ();

            var email = ToggleClientMock.fakeUserEmail;
            var pass = ToggleClientMock.fakeUserPassword;
            networkSwitcher.SetNetworkConnection(true); // Set state as connected.

            // Create some previous data before login
            var client = Util.CreateClientData().With(x => x.WorkspaceId = Util.WorkspaceId);
            var tag = Util.CreateTagData().With(x => x.WorkspaceId = Util.WorkspaceId);
            var project = Util.CreateProjectData(client.Id).With(x => x.WorkspaceId = Util.WorkspaceId);
            var te = Util.CreateTimeEntryData(DateTime.Now, 0, 0);

            te = te.With(t =>
            {
                t.WorkspaceId = Util.WorkspaceId;
                t.ProjectId = project.Id;
                t.Tags = new List<string> {tag.Name};
            });

            RxChain.Send(new DataMsg.ClientDataPut(client));
            RxChain.Send(new DataMsg.ProjectDataPut(project));
            RxChain.Send(new DataMsg.TagsPut(new List<ITagData> {tag}));
            RxChain.Send(new DataMsg.TimeEntryPut(te));

            // Register user
            RxChain.Send(ServerRequest.Authenticate.Signup(email, pass), new RxChain.Continuation((_, sent, queued) =>
            {
                try
                {
                    // User should be registered correctly.
                    Assert.That(_.User.RemoteId, Is.Not.Zero);
                    Assert.That(_.User.ApiToken, Is.Not.Empty);
                    registerTcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    registerTcs.SetException(ex);
                }
            }));

            await registerTcs.Task;

            StoreManager.Singleton
            .Observe(msg => msg.State)
            .Subscribe(state =>
            {
                if (state.Projects.First().Value.RemoteId != null)
                {
                    var mProject = state.Projects.First().Value;
                    var mClient = state.Clients.First().Value;
                    var mTag = state.Tags.First().Value;
                    var mTimeEntry = state.TimeEntries.First().Value.Data;

                    // Check that everything is synced!
                    Assert.That(mProject.SyncState, Is.EqualTo(SyncState.Synced));
                    Assert.That(mClient.SyncState, Is.EqualTo(SyncState.Synced));
                    Assert.That(mTag.SyncState, Is.EqualTo(SyncState.Synced));
                    Assert.That(mTimeEntry.SyncState, Is.EqualTo(SyncState.Synced));

                    // Check that remoteIds are > 0
                    Assert.That(mProject.RemoteId, Is.GreaterThan(0));
                    Assert.That(mClient.RemoteId, Is.GreaterThan(0));
                    Assert.That(mTag.RemoteId, Is.GreaterThan(0));
                    Assert.That(mTimeEntry.RemoteId, Is.GreaterThan(0));

                    // Check remote relationships
                    Assert.That(mProject.ClientRemoteId, Is.EqualTo(mClient.RemoteId));
                    Assert.That(mTimeEntry.ProjectRemoteId, Is.EqualTo(mProject.RemoteId));

                    getStateTcs.SetResult(true);
                }
            });

            await getStateTcs.Task;
        }
    }
}

