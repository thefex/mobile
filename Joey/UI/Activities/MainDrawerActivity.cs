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
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Fragments;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
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
        private readonly TimerComponent barTimer = new TimerComponent();
        private readonly Lazy<LogTimeEntriesListFragment> trackingFragment = new Lazy<LogTimeEntriesListFragment> ();
        private readonly Lazy<SettingsListFragment> settingsFragment = new Lazy<SettingsListFragment> ();
        private readonly Lazy<LoginFragment> loginFragment = new Lazy<LoginFragment> ();
        private readonly Lazy<ReportsPagerFragment> reportFragment = new Lazy<ReportsPagerFragment>();
        private readonly Lazy<ReportsNoApiFragment> reportNoApiFragment = new Lazy<ReportsNoApiFragment> ();
        private readonly Lazy<FeedbackFragment> feedbackFragment = new Lazy<FeedbackFragment>();
        private readonly Lazy<FeedbackNoApiFragment> feedbackNoApiFragment = new Lazy<FeedbackNoApiFragment> ();

        private DrawerListAdapter drawerAdapter;
        private IDisposable stateObserver;

        private ListView DrawerListView { get; set; }
        private TextView DrawerUserName { get; set; }
        private TextView DrawerEmail { get; set; }
        private ProfileImageView DrawerImage { get; set; }
        private DrawerLayout DrawerLayout { get; set; }
        protected ActionBarDrawerToggle DrawerToggle { get; private set; }
        private FrameLayout DrawerSyncView { get; set; }
        public Toolbar MainToolbar { get; set; }

        bool userWithoutApiToken
        {
            get
            {
                return string.IsNullOrEmpty(StoreManager.Singleton.AppState.User.ApiToken);
            }
        }

        protected override void OnCreateActivity(Bundle state)
        {
            base.OnCreateActivity(state);

            SetContentView(Resource.Layout.MainDrawerActivity);

            MainToolbar = FindViewById<Toolbar> (Resource.Id.MainToolbar);
            DrawerListView = FindViewById<ListView> (Resource.Id.DrawerListView);
            DrawerUserName = FindViewById<TextView> (Resource.Id.TitleTextView);
            DrawerEmail = FindViewById<TextView> (Resource.Id.EmailTextView);
            DrawerImage = FindViewById<ProfileImageView> (Resource.Id.IconProfileImageView);
            DrawerListView.ItemClick += OnDrawerListViewItemClick;

            DrawerLayout = FindViewById<DrawerLayout> (Resource.Id.DrawerLayout);
            DrawerToggle = new ActionBarDrawerToggle(this, DrawerLayout, MainToolbar, Resource.String.EntryName, Resource.String.EntryName);
            DrawerLayout.SetDrawerShadow(Resource.Drawable.drawershadow, (int)GravityFlags.Start);
            DrawerLayout.AddDrawerListener(DrawerToggle);

            var drawerFrameLayout = FindViewById<FrameLayout> (Resource.Id.DrawerFrameLayout);
            drawerFrameLayout.Touch += (sender, e) =>
            {
                // Do nothing, just absorb the event
                // TODO: Improve this dirty solution?
            };

            Timer.OnCreate(this);

            var lp = new Android.Support.V7.App.ActionBar.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent, (int)GravityFlags.Right);

            MainToolbar.SetNavigationIcon(Resource.Drawable.ic_menu_black_24dp);
            SetSupportActionBar(MainToolbar);
            SupportActionBar.SetTitle(Resource.String.MainDrawerTimer);
            SupportActionBar.SetCustomView(Timer.Root, lp);
            SupportActionBar.SetDisplayShowCustomEnabled(true);

            // ATTENTION Suscription to state (settings) changes inside
            // the view. This will be replaced for "router"
            // modified in the reducers.
            stateObserver = StoreManager.Singleton
                            .Observe(x => x.State.User)
                            .StartWith(StoreManager.Singleton.AppState.User)
                            .ObserveOn(SynchronizationContext.Current)
            .DistinctUntilChanged(x => new {x.ApiToken, x.Id})
            .Subscribe(userData => ResetFragmentNavigation(userData));
        }

        protected override void OnDestroy()
        {
            stateObserver.Dispose();
            base.OnDestroy();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            DrawerToggle.OnConfigurationChanged(newConfig);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return DrawerToggle.OnOptionsItemSelected(item) || base.OnOptionsItemSelected(item);
        }

        public override void Finish()
        {
            base.Finish();
        }

        private void ResetFragmentNavigation(IUserData userData)
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


#if DEBUG // TODO: DELETE TEST CODE --------
        bool firstTime = true;
        private bool tryMigrateDatabase(IUserData userData)
        {
            if (firstTime)
            {
                firstTime = false;
                MigrationFragment.CreateOldDbForTesting();
            }
#else
        private bool tryMigrateDatabase(IUserData userData)
        {
#endif
            var oldVersion = DatabaseHelper.CheckOldDb(DatabaseHelper.GetDatabaseDirectory());
            if (oldVersion == -1)
                return false;

            SetToolbarTimerVisible(false);
            SupportActionBar.SetTitle(Resource.String.MigratingScreenTitle);
            var migrationFragment = MigrationFragment.NewInstance(oldVersion);
            OpenFragment(migrationFragment);

            return true;
        }

        public void OpenPage(int id)
        {
            // Configure timer component for selected page:
            SetToolbarTimerVisible(id == DrawerListAdapter.TimerPageId);

            if (id == DrawerListAdapter.SettingsPageId)
            {
                OpenFragment(settingsFragment.Value);
                SupportActionBar.SetTitle(Resource.String.MainDrawerSettings);
            }
            else if (id == DrawerListAdapter.ReportsPageId)
            {
                if (userWithoutApiToken)
                {
                    SupportActionBar.SetTitle(Resource.String.MainDrawerReports);
                    OpenFragment(reportNoApiFragment.Value);
                }
                else
                {
                    if (reportFragment.Value.ZoomLevel == ZoomLevel.Week)
                        SupportActionBar.SetTitle(Resource.String.MainDrawerReportsWeek);
                    else if (reportFragment.Value.ZoomLevel == ZoomLevel.Month)
                        SupportActionBar.SetTitle(Resource.String.MainDrawerReportsMonth);
                    else
                        SupportActionBar.SetTitle(Resource.String.MainDrawerReportsYear);
                    OpenFragment(reportFragment.Value);
                }
            }
            else if (id == DrawerListAdapter.FeedbackPageId)
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerFeedback);
                if (userWithoutApiToken)
                    OpenFragment(feedbackNoApiFragment.Value);
                else
                    OpenFragment(feedbackFragment.Value);
            }
            else if (id == DrawerListAdapter.LoginPageId)
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerLogin);
                loginFragment.Value.ChangeToLogin();
                OpenFragment(loginFragment.Value);
            }
            else if (id == DrawerListAdapter.SignupPageId)
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerSignup);
                loginFragment.Value.ChangeToRegister();
                OpenFragment(loginFragment.Value);
            }
            else
            {
                SupportActionBar.SetTitle(Resource.String.MainDrawerTimer);
                OpenFragment(trackingFragment.Value);
            }

            DrawerListView.ClearChoices();
            DrawerListView.ChoiceMode = (ChoiceMode)ListView.ChoiceModeSingle;
            DrawerListView.SetItemChecked(drawerAdapter.GetItemPosition(id), true);
        }

        private void OpenFragment(Fragment fragment)
        {
            var old = FragmentManager.FindFragmentById(Resource.Id.ContentFrameLayout);
            if (old == null)
            {
                FragmentManager.BeginTransaction()
                .Add(Resource.Id.ContentFrameLayout, fragment)
                .Commit();
            }
            else if (old != fragment)
            {
                // The detach/attach is a workaround for https://code.google.com/p/android/issues/detail?id=42601
                FragmentManager.BeginTransaction()
                .Detach(old)
                .Replace(Resource.Id.ContentFrameLayout, fragment)
                .Attach(fragment)
                .Commit();
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

        private void SetToolbarTimerVisible(bool isVisible)
        {
            SupportActionBar.SetDisplayShowTitleEnabled(!isVisible);
            Timer.Hide = !isVisible;
        }

        public TimerComponent Timer
        {
            get { return barTimer; }
        }
    }
}
