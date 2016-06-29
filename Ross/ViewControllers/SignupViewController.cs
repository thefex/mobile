using System;
using Cirrious.FluentLayouts.Touch;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Google.SignIn;
using MonoTouch.TTTAttributedLabel;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.ViewModels;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class SignupViewController : UIViewController, ISignInDelegate, ISignInUIDelegate
    {
        private const string Tag = "SignupViewController";

        private UIView inputsContainer;
        private UIView topBorder;
        private UIView middleBorder;
        private UIView bottomBorder;
        private UITextField emailTextField;
        private UITextField passwordTextField;
        private UIButton passwordActionButton;
        private UIButton googleActionButton;
        private TTTAttributedLabel legalLabel;
        private Binding<bool, bool> isAuthenticatingBinding;
        private Binding<AuthResult, AuthResult> resultBinding;

        private LoginVM viewModel {get; set;}

        public SignupViewController()
        {
            Title = "SignupTitle".Tr();
        }

        public override void LoadView()
        {
            View = new UIView()
            .Apply(Style.Screen);

            View.Add(inputsContainer = new UIView().Apply(Style.Signup.InputsContainer));

            inputsContainer.Add(topBorder = new UIView().Apply(Style.Signup.InputsBorder));

            inputsContainer.Add(emailTextField = new UITextField()
            {
                Placeholder = "SignupEmailHint".Tr(),
                AutocapitalizationType = UITextAutocapitalizationType.None,
                KeyboardType = UIKeyboardType.EmailAddress,
                ReturnKeyType = UIReturnKeyType.Next,
                ClearButtonMode = UITextFieldViewMode.Always,
                ShouldReturn = HandleShouldReturn,
            } .Apply(Style.Signup.EmailField));
            emailTextField.EditingChanged += OnTextFieldEditingChanged;

            inputsContainer.Add(middleBorder = new UIView().Apply(Style.Signup.InputsBorder));

            inputsContainer.Add(passwordTextField = new PasswordTextField()
            {
                Placeholder = "SignupPasswordHint".Tr(),
                AutocapitalizationType = UITextAutocapitalizationType.None,
                AutocorrectionType = UITextAutocorrectionType.No,
                SecureTextEntry = true,
                ReturnKeyType = UIReturnKeyType.Go,
                ShouldReturn = HandleShouldReturn,
            } .Apply(Style.Signup.PasswordField));
            passwordTextField.EditingChanged += OnTextFieldEditingChanged;

            inputsContainer.Add(bottomBorder = new UIView().Apply(Style.Signup.InputsBorder));

            View.Add(passwordActionButton = new UIButton()
            .Apply(Style.Signup.SignupButton));
            passwordActionButton.SetTitle("SignupSignupButtonText".Tr(), UIControlState.Normal);
            passwordActionButton.TouchUpInside += OnPasswordActionButtonTouchUpInside;

            View.Add(googleActionButton = new UIButton()
            .Apply(Style.Signup.GoogleButton));
            googleActionButton.SetTitle("SignupGoogleButtonText".Tr(), UIControlState.Normal);
            googleActionButton.TouchUpInside += OnGoogleActionButtonTouchUpInside;

            View.Add(legalLabel = new TTTAttributedLabel()
            {
                Delegate = new LegalLabelDelegate(),
            } .Apply(Style.Signup.LegalLabel));
            SetLegalText(legalLabel);

            inputsContainer.AddConstraints(
                topBorder.AtTopOf(inputsContainer),
                topBorder.AtLeftOf(inputsContainer),
                topBorder.AtRightOf(inputsContainer),
                topBorder.Height().EqualTo(1f),

                emailTextField.Below(topBorder),
                emailTextField.AtLeftOf(inputsContainer, 20f),
                emailTextField.AtRightOf(inputsContainer, 10f),
                emailTextField.Height().EqualTo(42f),

                middleBorder.Below(emailTextField),
                middleBorder.AtLeftOf(inputsContainer, 20f),
                middleBorder.AtRightOf(inputsContainer),
                middleBorder.Height().EqualTo(1f),

                passwordTextField.Below(middleBorder),
                passwordTextField.AtLeftOf(inputsContainer, 20f),
                passwordTextField.AtRightOf(inputsContainer),
                passwordTextField.Height().EqualTo(42f),

                bottomBorder.Below(passwordTextField),
                bottomBorder.AtLeftOf(inputsContainer),
                bottomBorder.AtRightOf(inputsContainer),
                bottomBorder.AtBottomOf(inputsContainer),
                bottomBorder.Height().EqualTo(1f)
            );

            inputsContainer.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

            View.AddConstraints(
                inputsContainer.AtTopOf(View, 80f),
                inputsContainer.AtLeftOf(View),
                inputsContainer.AtRightOf(View),

                passwordActionButton.Below(inputsContainer, 20f),
                passwordActionButton.AtLeftOf(View),
                passwordActionButton.AtRightOf(View),
                passwordActionButton.Height().EqualTo(60f),

                googleActionButton.Below(passwordActionButton, 5f),
                googleActionButton.AtLeftOf(View),
                googleActionButton.AtRightOf(View),
                googleActionButton.Height().EqualTo(60f),

                legalLabel.AtBottomOf(View, 30f),
                legalLabel.AtLeftOf(View, 40f),
                legalLabel.AtRightOf(View, 40f)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

            ResetSignupButtonState();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            SignIn.SharedInstance.Delegate = this;
            SignIn.SharedInstance.UIDelegate = this;

            viewModel = new LoginVM();
            if (viewModel.CurrentLoginMode == LoginVM.LoginMode.Login)
            {
                viewModel.ToggleLoginMode();
            }

            isAuthenticatingBinding = this.SetBinding(() => viewModel.IsAuthenticating, () => IsAuthenticating);
            resultBinding = this.SetBinding(() => viewModel.AuthResult).WhenSourceChanges(() =>
            {
                switch (viewModel.AuthResult)
                {
                    case AuthResult.None:
                        break;

                    case AuthResult.Success:
                        // TODO RX: Start the initial sync for the user
                        //ServiceContainer.Resolve<ISyncManager> ().Run ();
                        // Start the initial sync for the user
                        break;

                    // Error cases
                    default:
                        if (viewModel.CurrentLoginMode == LoginVM.LoginMode.Login)
                        {
                            if (viewModel.AuthResult == AuthResult.InvalidCredentials)
                            {
                                passwordTextField.Text = string.Empty;
                            }
                            passwordTextField.BecomeFirstResponder();
                        }
                        else
                        {
                            emailTextField.BecomeFirstResponder();
                        }
                        AuthErrorAlert.Show(this, emailTextField.Text, viewModel.AuthResult, AuthErrorAlert.Mode.Login);
                        break;
                }
            });
        }

        public override void ViewWillDisappear(bool animated)
        {
            isAuthenticatingBinding.Detach();
            resultBinding.Detach();
            viewModel.Dispose();
            base.ViewWillDisappear(animated);
        }

        private void OnTextFieldEditingChanged(object sender, EventArgs e)
        {
            ResetSignupButtonState();
        }

        private void ResetSignupButtonState()
        {
            var enabled = !IsAuthenticating
                          && !string.IsNullOrWhiteSpace(emailTextField.Text) && emailTextField.Text.Contains("@")
                          && !string.IsNullOrWhiteSpace(passwordTextField.Text) && passwordTextField.Text.Length >= 6;
            passwordActionButton.SetTitle("SignupSignupButtonText".Tr(), UIControlState.Disabled);
            passwordActionButton.Enabled = enabled;
        }

        private static void SetLegalText(TTTAttributedLabel label)
        {
            var template = "SignupLegal".Tr();
            var arg0 = "SignupToS".Tr();
            var arg1 = "SignupPrivacy".Tr();

            var arg0idx = string.Format(template, "{0}", arg1).IndexOf("{0}", StringComparison.Ordinal);
            var arg1idx = string.Format(template, arg0, "{1}").IndexOf("{1}", StringComparison.Ordinal);

            label.Text = (NSString)string.Format(template, arg0, arg1);
            label.AddLinkToURL(
                new NSUrl(Phoebe.Build.TermsOfServiceUrl.ToString()),
                new NSRange(arg0idx, arg0.Length));
            label.AddLinkToURL(
                new NSUrl(Phoebe.Build.PrivacyPolicyUrl.ToString()),
                new NSRange(arg1idx, arg1.Length));
        }

        private bool HandleShouldReturn(UITextField textField)
        {
            if (textField == emailTextField)
            {
                passwordTextField.BecomeFirstResponder();
            }
            else if (textField == passwordTextField)
            {
                textField.ResignFirstResponder();
                TryPasswordSignup();
            }
            else
            {
                return false;
            }
            return true;
        }

        private void OnPasswordActionButtonTouchUpInside(object sender, EventArgs e)
        {
            TryPasswordSignup();
        }

        private void OnGoogleActionButtonTouchUpInside(object sender, EventArgs e)
        {
            // Automatically sign in the user.
            SignIn.SharedInstance.SignInUser();
        }

        public void DidSignIn(SignIn signIn, GoogleUser user, NSError error)
        {
            if (error == null)
            {
                var token = user.Authentication.AccessToken;
                //googleEmail = user.Profile.Email;
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

        private void TryPasswordSignup()
        {
            viewModel.TryLogin(emailTextField.Text, passwordTextField.Text);
        }

        private bool isAuthenticating;
        public bool IsAuthenticating
        {
            get { return isAuthenticating; }
            set
            {
                isAuthenticating = value;
                emailTextField.Enabled = !isAuthenticating;
                passwordTextField.Enabled = !isAuthenticating;
                passwordActionButton.Enabled = !isAuthenticating;
                googleActionButton.Enabled = !isAuthenticating;

                passwordActionButton.SetTitle("SignupSignupProgressText".Tr(), UIControlState.Disabled);
            }
        }

        private class LegalLabelDelegate : TTTAttributedLabelDelegate
        {
            public override void DidSelectLinkWithURL(TTTAttributedLabel label, NSUrl url)
            {
                var stringUrl = url.AbsoluteString;
                var tosUrl = Phoebe.Build.TermsOfServiceUrl.ToString();
                var privacyUrl = Phoebe.Build.PrivacyPolicyUrl.ToString();
                if (stringUrl.Equals(tosUrl) || stringUrl.Equals(privacyUrl))
                {
                    url = new NSUrl(string.Format("{0}?simple=true", stringUrl));
                }
                WebViewController controller = new WebViewController(url);
                label.Window.RootViewController.PresentViewController(controller, true, null);
            }
        }
    }
}
