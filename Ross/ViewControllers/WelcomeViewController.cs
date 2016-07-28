using System;
using Cirrious.FluentLayouts.Touch;
using GalaSoft.MvvmLight.Helpers;
using Google.SignIn;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.ViewModels;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class WelcomeViewController : UIViewController, ISignInDelegate, ISignInUIDelegate
    {
        private const string Tag = "WelcomeViewController";

        private UINavigationController navController;
        private UIImageView logoImageView;
        private UILabel sloganLabel;
        private UIButton createButton;
        private UIButton passwordButton;
        //private UIButton googleButton;

        private Binding<bool, bool> isAuthenticatingBinding;
        private Binding<AuthResult, AuthResult> resultBinding;
        private LoginVM viewModel {get; set;}
        private string googleEmail;

        public override void LoadView()
        {
            View = new UIImageView
            {
                UserInteractionEnabled = true,
            } .Apply(Style.Welcome.Background);
            View.Add(logoImageView = new UIImageView().Apply(Style.Welcome.Logo));
            View.Add(sloganLabel = new UILabel
            {
                Text = "WelcomeSlogan".Tr(),
            } .Apply(Style.Welcome.Slogan));
            View.Add(createButton = new UIButton().Apply(Style.Welcome.CreateAccount));
            View.Add(passwordButton = new UIButton().Apply(Style.Welcome.PasswordLogin));
            //View.Add(googleButton = new UIButton().Apply(Style.Welcome.GoogleLogin));

            createButton.SetTitle("WelcomeCreate".Tr(), UIControlState.Normal);
            passwordButton.SetTitle("WelcomePassword".Tr(), UIControlState.Normal);
            //googleButton.SetTitle("WelcomeGoogle".Tr(), UIControlState.Normal);

            createButton.TouchUpInside += OnCreateButtonTouchUpInside;
            passwordButton.TouchUpInside += OnPasswordButtonTouchUpInside;
            //googleButton.TouchUpInside += OnGoogleButtonTouchUpInside;

            View.AddConstraints(
                logoImageView.AtTopOf(View, 70f),
                logoImageView.WithSameCenterX(View),

                sloganLabel.Below(logoImageView, 18f),
                sloganLabel.AtLeftOf(View, 25f),
                sloganLabel.AtRightOf(View, 25f),

                //googleButton.AtBottomOf(View, 20f),
                //googleButton.AtLeftOf(View),
                //googleButton.AtRightOf(View),
                //googleButton.Height().EqualTo(60f),

                passwordButton.AtBottomOf(View, 25f + 20 + 60),
                passwordButton.AtLeftOf(View),
                passwordButton.AtRightOf(View),
                passwordButton.Height().EqualTo(60f),

                createButton.Above(passwordButton, 5f),
                createButton.AtLeftOf(View),
                createButton.AtRightOf(View),
                createButton.Height().EqualTo(60f)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
        }

        private void OnCreateButtonTouchUpInside(object sender, EventArgs e)
        {
            NavigationController.PushViewController(new SignupViewController(), true);
        }

        private void OnPasswordButtonTouchUpInside(object sender, EventArgs e)
        {
            NavigationController.PushViewController(new LoginViewController(), true);
        }

        private void OnGoogleButtonTouchUpInside(object sender, EventArgs e)
        {
            // Automatically sign in the user.
            SignIn.SharedInstance.SignInUser();
        }

        public void DidSignIn(SignIn signIn, GoogleUser user, Foundation.NSError error)
        {
            if (error == null)
            {
                var token = user.Authentication.AccessToken;
                googleEmail = user.Profile.Email;
                signIn.DisconnectUser();  // Disconnect user from Google.
                viewModel.TryLoginWithGoogle(token);
            }
            else if (error.Code != -5)     // Cancel error code.
            {
                new UIAlertView("WelcomeGoogleErrorTitle".Tr(), "WelcomeGoogleErrorMessage".Tr(), null, "WelcomeGoogleErrorOk".Tr(), null).Show();
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Info(Tag, "Failed to authenticate (G+) the user.");
                IsAuthenticating = false;
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            SignIn.SharedInstance.Delegate = this;
            SignIn.SharedInstance.UIDelegate = this;

            viewModel = new LoginVM();
            isAuthenticatingBinding = this.SetBinding(() => viewModel.IsAuthenticating, () => IsAuthenticating);
            resultBinding = this.SetBinding(() => viewModel.AuthResult).WhenSourceChanges(() =>
            {
                switch (viewModel.AuthResult)
                {
                    case AuthResult.None:
                        break;

                    case AuthResult.Success:
                        break;

                    // Error cases
                    default:
                        AuthErrorAlert.Show(this, googleEmail, viewModel.AuthResult, AuthErrorAlert.Mode.Login, true);
                        break;
                }
            });

            navController = NavigationController;
            if (navController != null)
            {
                navController.SetNavigationBarHidden(true, animated);
            }
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Welcome";
        }

        public override void ViewWillDisappear(bool animated)
        {
            if (navController != null)
            {
                navController.SetNavigationBarHidden(false, animated);
                navController = null;
            }

            if (SignIn.SharedInstance.Delegate == this)
            {
                SignIn.SharedInstance.Delegate = null;
                SignIn.SharedInstance.UIDelegate = this;
            }

            isAuthenticatingBinding.Detach();
            resultBinding.Detach();
            viewModel.Dispose();

            base.ViewWillDisappear(animated);
        }

        public bool IsAuthenticating
        {
            get { return !View.UserInteractionEnabled; }
            set
            {
                View.UserInteractionEnabled = !value;
                UIView.Animate(0.3, 0, UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseInOut,
                               () =>
                {
                    createButton.Alpha = value ? 0 : 1;
                    //googleButton.Alpha = value ? 0 : 1;
                }, () => {});
                passwordButton.SetTitle(value ? "WelcomeLoggingIn".Tr() : "WelcomePassword".Tr(), UIControlState.Normal);
            }
        }
    }
}
