using System;
using System.Reactive.Linq;
using System.Threading;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Fragments;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Reactive;
using XPlatUtils;
using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Activities
{
    [Activity(
         ScreenOrientation = ScreenOrientation.Portrait,
         Name = "toggl.joey.ui.activities.MainDrawerActivity",
         Label = "@string/EntryName",
         Exported = true,
#if DEBUG
         // The actual entry-point is defined in manifest via activity-alias, this here is just to
         // make adb launch the activity automatically when developing.
#endif
         Theme = "@style/Theme.Toggl.Main")]
    public class MainDrawerActivity : BaseActivity
    {
        private const string MainFragmentTag = "MainPageFragmentTag";

        private DrawerListAdapter drawerAdapter;
        private IDisposable stateObserver;

        private ListView DrawerListView { get; set; }
        private TextView DrawerUserName { get; set; }
        private TextView DrawerEmail { get; set; }
        private ProfileImageView DrawerImage { get; set; }
        private DrawerLayout DrawerLayout { get; set; }
        protected ActionBarDrawerToggle DrawerToggle { get; private set; }
        private FrameLayout DrawerSyncView { get; set; }
        private Toolbar MainToolbar { get; set; }

        bool userWithoutApiToken
        {
            get
            {
                return string.IsNullOrEmpty(StoreManager.Singleton.AppState.User.ApiToken);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.MainDrawerActivity);

            MainToolbar = FindViewById<Toolbar>(Resource.Id.MainToolbar);
            DrawerListView = FindViewById<ListView>(Resource.Id.DrawerListView);
            DrawerUserName = FindViewById<TextView>(Resource.Id.TitleTextView);
            DrawerEmail = FindViewById<TextView>(Resource.Id.EmailTextView);
            DrawerImage = FindViewById<ProfileImageView>(Resource.Id.IconProfileImageView);
            DrawerListView.ItemClick += OnDrawerListViewItemClick;

            DrawerLayout = FindViewById<DrawerLayout>(Resource.Id.DrawerLayout);
            DrawerToggle = new ActionBarDrawerToggle(this, DrawerLayout, MainToolbar, Resource.String.EntryName, Resource.String.EntryName);
            DrawerLayout.SetDrawerShadow(Resource.Drawable.drawershadow, (int)GravityFlags.Start);
            DrawerLayout.AddDrawerListener(DrawerToggle);

            var drawerFrameLayout = FindViewById<FrameLayout>(Resource.Id.DrawerFrameLayout);
            drawerFrameLayout.Touch += (sender, e) =>
            {
                // Do nothing, just absorb the event
                // TODO: Improve this dirty solution?
            };

            MainToolbar.SetNavigationIcon(Resource.Drawable.ic_menu_black_24dp);
            SetSupportActionBar(MainToolbar);
            SupportActionBar.SetDisplayShowCustomEnabled(true);

            // ATTENTION Suscription to state (settings) changes inside
            // the view. This will be replaced for "router"
            // modified in the reducers.
            stateObserver = StoreManager.Singleton
                            .Observe(x => x.State.User)
                            .ObserveOn(SynchronizationContext.Current)
                            .StartWith(StoreManager.Singleton.AppState.User)
                            .DistinctUntilChanged(x => x.ApiToken)
                            .Subscribe(userData => ResetFragmentNavigation(userData));
        }

        protected override void OnDestroy()
        {
            stateObserver.Dispose();
            base.OnDestroy();
        }

        // `onPostCreate` called when activity start-up is complete after `onStart()`
        // NOTE! Make sure to override the method with only a single `Bundle` argument
        public override void OnPostCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnPostCreate(savedInstanceState, persistentState);
            // Sync the toggle state after onRestoreInstanceState has occurred.
            DrawerToggle.SyncState();
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            // Pass any configuration change to the drawer toggles
            DrawerToggle.OnConfigurationChanged(newConfig);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return DrawerToggle.OnOptionsItemSelected(item) || base.OnOptionsItemSelected(item);
        }

        public void ResetFragmentNavigation(IUserData userData)
        {
            // TODO : Don't let both name/email empty.
            // maybe an elegant solution is possible.
            DrawerUserName.Text = string.IsNullOrEmpty(userData.Name) ? "John Doe" : userData.Name;
            DrawerEmail.Text = string.IsNullOrEmpty(userData.Email) ? "support@toggl.com" : userData.Email;
            DrawerImage.ImageUrl = userData.ImageUrl;

            if (tryMigrateDatabase(userData))
                return;

            if (userData.Id == Guid.Empty)
                // If user doesn't exists, create a dummy one
                // and use it until the user connects to
                // Toggl servers.
                RxChain.Send(new DataMsg.NoUserDataPut());
            else
                // Make sure that the user will see newest data when they start the activity
                RxChain.Send(new ServerRequest.GetChanges());

            // Configure left menu.
            DrawerListView.Adapter = drawerAdapter = new DrawerListAdapter(withApiToken: string.IsNullOrEmpty(userData.ApiToken));

            // Open Timer fragment.
            OpenPage(DrawerListAdapter.TimerPageId);
        }

        private bool tryMigrateDatabase(IUserData userData)
        {
            var oldVersion = DatabaseHelper.CheckOldDb(DatabaseHelper.GetDatabaseDirectory());
            if (oldVersion == -1)
                return false;

            SupportActionBar.SetTitle(Resource.String.MigratingScreenTitle);
            var migrationFragment = MigrationFragment.NewInstance(oldVersion);
            OpenFragment(migrationFragment);

            return true;
        }

        public void OpenPage(int id)
        {
            if (id == DrawerListAdapter.SettingsPageId)
            {
                OpenFragment(typeof(SettingsListFragment));
                SupportActionBar.SetTitle(Resource.String.MainDrawerSettings);
            }
            else if (id == DrawerListAdapter.ReportsPageId)
            {
                if (userWithoutApiToken)
                {
                    SupportActionBar.SetTitle(Resource.String.MainDrawerReports);
                    OpenFragment(typeof(ReportsNoApiFragment));
                }
                else
                {
                    var zoomLevel = (ZoomLevel)StoreManager.Singleton.AppState.Settings.LastReportZoom;
                    if (zoomLevel == ZoomLevel.Week)
                        SupportActionBar.SetTitle(Resource.String.MainDrawerReportsWeek);
                    else if (zoomLevel == ZoomLevel.Month)
                        SupportActionBar.SetTitle(Resource.String.MainDrawerReportsMonth);
                    else
                        SupportActionBar.SetTitle(Resource.String.MainDrawerReportsYear);
                    OpenFragment(typeof(ReportsPagerFragment));
                }
            }
            else if (id == DrawerListAdapter.FeedbackPageId)
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerFeedback);
                if (userWithoutApiToken)
                    OpenFragment(typeof(FeedbackNoApiFragment));
                else
                    OpenFragment(typeof(FeedbackFragment));
            }
            else if (id == DrawerListAdapter.LoginPageId)
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerLogin);
                OpenFragment(LoginFragment.NewInstance());
            }
            else if (id == DrawerListAdapter.SignupPageId)
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerSignup);
                OpenFragment(LoginFragment.NewInstance(isSignupMode: true));
            }
            else
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerTimer);
                OpenFragment(typeof(LogTimeEntriesListFragment));
            }

            DrawerListView.ClearChoices();
            DrawerListView.ChoiceMode = (ChoiceMode)ListView.ChoiceModeSingle;
            DrawerListView.SetItemChecked(drawerAdapter.GetItemPosition(id), true);
        }

        private void OpenFragment(Type fragmentType)
        {
            try
            {
                var fragmentClass = Java.Lang.Class.FromType(fragmentType);
                var fragment = (Fragment)fragmentClass.NewInstance();
                FragmentManager.BeginTransaction()
                .Replace(Resource.Id.ContentFrameLayout, fragment, MainFragmentTag)
                .Commit();
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.Resolve<ILogger>();
                logger.Error(nameof(MainDrawerActivity), ex, "Error opening Drawer fragment.");
            }
        }

        private void OpenFragment(Fragment newFragment)
        {
            try
            {
                FragmentManager.BeginTransaction()
                .Replace(Resource.Id.ContentFrameLayout, newFragment, MainFragmentTag)
                .Commit();
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.Resolve<ILogger>();
                logger.Error(nameof(MainDrawerActivity), ex, "Error opening Drawer fragment.");
            }
        }

        private void OnDrawerListViewItemClick(object sender, ListView.ItemClickEventArgs e)
        {

            // If tap outside options just close drawer
            if (e.Id == -1)
            {
                DrawerLayout.CloseDrawers();
                return;
            }

            if (e.Id == DrawerListAdapter.TimerPageId)
            {
                OpenPage(DrawerListAdapter.TimerPageId);
            }
            else if (e.Id == DrawerListAdapter.LogoutPageId)
            {
                OpenPage(DrawerListAdapter.TimerPageId);
                // Attention. At this remote point
                // send a Reset message and unregister from
                // GCM system.
                RxChain.Send(new DataMsg.UnregisterPush());
                RxChain.Send(new DataMsg.ResetState());
            }
            else if (e.Id == DrawerListAdapter.ReportsPageId)
            {
                OpenPage(DrawerListAdapter.ReportsPageId);
            }
            else if (e.Id == DrawerListAdapter.SettingsPageId)
            {
                OpenPage(DrawerListAdapter.SettingsPageId);
            }
            else if (e.Id == DrawerListAdapter.FeedbackPageId)
            {
                OpenPage(DrawerListAdapter.FeedbackPageId);
            }
            else if (e.Id == DrawerListAdapter.LoginPageId)
            {
                OpenPage(DrawerListAdapter.LoginPageId);
            }
            else if (e.Id == DrawerListAdapter.SignupPageId)
            {
                OpenPage(DrawerListAdapter.SignupPageId);
            }

            DrawerLayout.CloseDrawers();
        }

        public override void OnBackPressed()
        {
            if (!IsTimerFragmentCurrentlyPresent())
                OpenPage(DrawerListAdapter.TimerPageId);
            else
                base.OnBackPressed();
        }

        private bool IsTimerFragmentCurrentlyPresent()
        {
            var currentlyPresentedFragment = FragmentManager.FindFragmentByTag(MainFragmentTag);

            return currentlyPresentedFragment != null &&
                   currentlyPresentedFragment.GetType() == typeof (LogTimeEntriesListFragment);
        }
    }
}
