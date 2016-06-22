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
            dataStore.Table<UserData> ().Delete(e => true);
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
    }
}

