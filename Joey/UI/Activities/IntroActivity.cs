using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    [Activity(
         Exported = false,
         ScreenOrientation = ScreenOrientation.Portrait,
         WindowSoftInputMode = SoftInput.StateHidden,
         Theme = "@style/Theme.Toggl.Intro")]
    public class IntroActivity : FragmentActivity
    {
        private const string LogTag = "IntroActivity";
        private Button LoginButton;
        private Button StartNowButton;

        private Binding<AuthResult, AuthResult> resultBinding;

        public IntroVM ViewModel { get; private set; }

        protected override void OnCreate(Bundle state)
        {
            base.OnCreate(state);
            SetContentView(Resource.Layout.IntroActivity);
            FindViewById<TextView> (Resource.Id.IntroTitle).SetFont(Font.DINMedium);
            FindViewById<TextView> (Resource.Id.IntroLoginText).SetFont(Font.DINMedium);
            LoginButton = FindViewById<Button> (Resource.Id.LoginButton).SetFont(Font.DINMedium);
            StartNowButton = FindViewById<Button> (Resource.Id.StartNowButton).SetFont(Font.DINMedium);
            LoginButton.Click += LoginButtonClick;
            StartNowButton.Click += StartNowButtonClick;

            ViewModel = new IntroVM();

            resultBinding = this.SetBinding(() => ViewModel.AuthResult).WhenSourceChanges(() =>
            {
                switch (ViewModel.AuthResult)
                {
                    case AuthResult.None:
                        break;

                    case AuthResult.Success:
                        // TODO RX: Start the initial sync for the user
                        //ServiceContainer.Resolve<ISyncManager> ().Run ();
                        var intent = new Intent(this, typeof(MainDrawerActivity));
                        intent.AddFlags(ActivityFlags.ClearTop);
                        StartActivity(intent);
                        Finish();
                        break;

                }
            });
        }

        private void LoginButtonClick(object sender, EventArgs e)
        {
            ServiceContainer.Resolve<ITracker>().SendIntroModeEvent(UserMode.NormalMode);
            StartActivity(new Intent(this, typeof(LoginActivity)));
        }

        private void StartNowButtonClick(object sender, EventArgs e)
        {
            ServiceContainer.Resolve<ITracker>().SendIntroModeEvent(UserMode.NoUserMode);
            ViewModel.SetUpNoUser();
        }
    }
}
