using System;
using System.Collections.Generic;
using System.Linq;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Gms.Auth;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using XPlatUtils;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class LoginFragment : Fragment,
        GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener
    {
        const string LogTag = "LoginFragment";

        const int RC_SIGN_IN = 9001;
        const string KEY_IS_RESOLVING = "is_resolving";
        const string KEY_SHOULD_RESOLVE = "should_resolve";
        const string ExtraShowPassword = "com.toggl.timer.show_password";

        private GoogleApiClient mGoogleApiClient;
        // State variables for Google Login, not exactly needed.
        // more info here: https://github.com/xamarin/monodroid-samples/blob/master/google-services/SigninQuickstart/SigninQuickstart/MainActivity.cs
        private bool mIsResolving;
        private bool mShouldResolve;
        private bool hasGoogleAccounts;
        private bool showPassword;
        private ISpannable formattedLegalText;
        protected LoginVM ViewModel { get; private set; }
        protected ScrollView ScrollView { get; private set; }
        protected TextInputLayout EmailInputLayout { get; private set; }
        protected TextInputLayout PasswordInputLayout { get; private set; }
        protected AutoCompleteTextView EmailEditText { get; private set; }
        protected EditText PasswordEditText { get; private set; }
        protected Button PasswordToggleButton { get; private set; }
        protected Button SubmitButton { get; private set; }
        protected ImageView SpinningImage { get; private set; }
        protected TextView LegalTextView { get; private set; }
        protected FrameLayout GoogleLoginButton { get; private set; }
        protected TextView GoogleLoginText { get; private set; }
        protected TextView GoogleIntroText { get; private set; }
        protected Toolbar LoginToolbar { get; private set; }

        private LoginVM.LoginMode mode = LoginVM.LoginMode.Login;
        private Binding<bool, bool> isAuthencticatedBinding, isAuthenticatingBinding;
        private Binding<LoginVM.LoginMode, LoginVM.LoginMode> modeBinding;
        private Binding<AuthResult, AuthResult> resultBinding;

        private ArrayAdapter<string> MakeEmailsAdapter()
        {
            var am = AccountManager.Get(Activity);
            var emails = am.GetAccounts()
                         .Select(a => a.Name)
                         .Where(a => a.Contains("@"))
                         .Distinct()
                         .ToList();
            return new ArrayAdapter<string> (Activity, Android.Resource.Layout.SelectDialogItem, emails);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.ConnectLayout, container, false);

            EmailInputLayout = view.FindViewById<TextInputLayout> (Resource.Id.EmailInputLayout);
            EmailEditText = view.FindViewById<AutoCompleteTextView> (Resource.Id.EmailAutoCompleteTextView).SetFont(Font.RobotoLight);
            PasswordInputLayout = view.FindViewById<TextInputLayout> (Resource.Id.PasswordInputLayout);
            PasswordEditText = view.FindViewById<EditText> (Resource.Id.PasswordEditText).SetFont(Font.RobotoLight);
            PasswordToggleButton = view.FindViewById<Button> (Resource.Id.PasswordToggleButton).SetFont(Font.Roboto);
            SubmitButton = view.FindViewById<Button> (Resource.Id.SubmitButton).SetFont(Font.Roboto);
            SpinningImage = view.FindViewById<ImageView>(Resource.Id.RegisterLoadingImageView);
            LegalTextView = view.FindViewById<TextView> (Resource.Id.LegalTextView).SetFont(Font.RobotoLight);
            GoogleLoginButton = view.FindViewById<FrameLayout> (Resource.Id.GoogleLoginButton);
            GoogleLoginText = view.FindViewById<TextView> (Resource.Id.GoogleLoginText).SetFont(Font.Roboto);
            GoogleIntroText = view.FindViewById<TextView> (Resource.Id.GoogleIntroText);

            EmailInputLayout.HintEnabled = false;
            EmailInputLayout.ErrorEnabled = true;
            PasswordInputLayout.HintEnabled = false;
            PasswordInputLayout.ErrorEnabled = true;

            SubmitButton.Click += OnLoginButtonClick;
            GoogleLoginButton.Click += OnGoogleLoginButtonClick;
            EmailEditText.Adapter = MakeEmailsAdapter();
            EmailEditText.Threshold = 1;
            EmailEditText.FocusChange += (sender, e) => ValidateEmailField();
            PasswordEditText.TextChanged += OnPasswordEditTextTextChanged;
            PasswordEditText.FocusChange += (sender, e) => ValidatePasswordField();
            PasswordToggleButton.Click += OnPasswordToggleButtonClick;
            hasGoogleAccounts = GoogleAccounts.Count > 0;
            GoogleLoginButton.Visibility = hasGoogleAccounts ? ViewStates.Visible : ViewStates.Gone;
            GoogleIntroText.Visibility = hasGoogleAccounts ? ViewStates.Visible : ViewStates.Gone;

            var spinningImageAnimation = AnimationUtils.LoadAnimation(Activity.BaseContext, Resource.Animation.SpinningAnimation);
            SpinningImage.StartAnimation(spinningImageAnimation);
            SpinningImage.ImageAlpha = 0;

            if (savedInstanceState != null)
            {
                showPassword = savedInstanceState.GetBoolean(ExtraShowPassword);
                mIsResolving = savedInstanceState.GetBoolean(KEY_IS_RESOLVING);
                mShouldResolve = savedInstanceState.GetBoolean(KEY_SHOULD_RESOLVE);
            }

            // Google API client
            GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestEmail()
            .Build();
            mGoogleApiClient = new GoogleApiClient.Builder(Activity)
            .AddConnectionCallbacks(this)
            .AddOnConnectionFailedListener(this)
            .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
            .Build();

            ViewModel = new LoginVM();
            ViewModel.ChangeLoginMode(mode);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            isAuthenticatingBinding = this.SetBinding(() => ViewModel.IsAuthenticating).WhenSourceChanges(SetViewState);
            modeBinding = this.SetBinding(() => ViewModel.CurrentLoginMode).WhenSourceChanges(SetViewState);
            resultBinding = this.SetBinding(() => ViewModel.AuthResult).WhenSourceChanges(() =>
            {
                ViewModel.SetAuthenticating(false);
                switch (ViewModel.AuthResult)
                {
                    case AuthResult.None:
                        SetViewState();
                        break;

                    case AuthResult.Success:
                        EmailEditText.Text = PasswordEditText.Text = string.Empty;
                        break;

                    // Error cases
                    default:
                        if (ViewModel.CurrentLoginMode == LoginVM.LoginMode.Login)
                        {
                            if (ViewModel.AuthResult == AuthResult.InvalidCredentials)
                            {
                                PasswordEditText.Text = string.Empty;
                            }
                            PasswordEditText.RequestFocus();
                        }
                        else
                        {
                            EmailEditText.RequestFocus();
                        }
                        ShowAuthError(EmailEditText.Text, ViewModel.AuthResult, ViewModel.CurrentLoginMode);
                        break;
                }
            });
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean(ExtraShowPassword, showPassword);
            outState.PutBoolean(KEY_IS_RESOLVING, mIsResolving);
            outState.PutBoolean(KEY_SHOULD_RESOLVE, mIsResolving);
        }

        #region View state utils

        public LoginVM.LoginMode Mode
        {
            get
            {
                return ViewModel.CurrentLoginMode;
            }
            set
            {
                if (mode == value)
                    return;
                mode = value;

                if (ViewModel != null)
                {
                    ViewModel.ChangeLoginMode(mode);
                    SetViewState();
                }
            }
        }

        private void SetViewState()
        {
            // Views not loaded yet/anymore?
            if (SubmitButton == null)
            {
                return;
            }

            if (ViewModel.CurrentLoginMode == LoginVM.LoginMode.Login)
            {
                SubmitButton.SetText(ViewModel.IsAuthenticating ? Resource.String.LoginButtonProgressText : Resource.String.LoginButtonText);
                LegalTextView.Visibility = ViewStates.Gone;
                GoogleLoginText.SetText(Resource.String.LoginGoogleButtonText);
            }
            else
            {
                SubmitButton.SetText(ViewModel.IsAuthenticating ? Resource.String.LoginButtonSignupProgressText : Resource.String.LoginSignupButtonText);
                LegalTextView.SetText(FormattedLegalText, TextView.BufferType.Spannable);
                LegalTextView.MovementMethod = Android.Text.Method.LinkMovementMethod.Instance;
                LegalTextView.Visibility = ViewStates.Visible;
                GoogleLoginText.SetText(Resource.String.LoginSignupGoogleButtonText);
            }

            EmailEditText.Enabled = !ViewModel.IsAuthenticating;
            PasswordEditText.Enabled = !ViewModel.IsAuthenticating;
            GoogleLoginButton.Enabled = !ViewModel.IsAuthenticating;
            SubmitButton.Text = ViewModel.IsAuthenticating ? String.Empty : Activity.Resources.GetString(Resource.String.LoginButtonText);
            SpinningImage.ImageAlpha = ViewModel.IsAuthenticating ? 255 : 0;

            SetPasswordVisibility();
            ValidateEmailField();
            ValidatePasswordField();
        }

        private void ValidateEmailField(bool tolerateEmpty = true)
        {

            if (EmailEditText.Text.Length == 0 && tolerateEmpty)
            {
                EmailInputLayout.Error = null;
                return;
            }

            bool isError = !ViewModel.IsEmailValid(EmailEditText.Text);

            EmailInputLayout.Error = isError ? GetText(Resource.String.LoginEmailError) : null;
        }

        private void ValidatePasswordField(bool tolerateEmpty = true)
        {
            if (PasswordEditText.Text.Length == 0 && tolerateEmpty)
            {
                PasswordInputLayout.Error = null;
                return;
            }
            bool isError = (ViewModel.CurrentLoginMode == LoginVM.LoginMode.Signup && !ViewModel.IsPassValid(PasswordEditText.Text));
            PasswordInputLayout.Error = isError ? GetText(Resource.String.LoginPasswordError) : null;
        }

        private void SetPasswordVisibility()
        {
            if (PasswordEditText.Text.Length == 0)
            {
                PasswordToggleButton.Visibility = ViewStates.Gone;
                showPassword = false;
            }
            else if (showPassword)
            {
                PasswordToggleButton.SetText(Resource.String.LoginHideButtonText);
                PasswordToggleButton.Visibility = ViewStates.Visible;
            }
            else
            {
                PasswordToggleButton.SetText(Resource.String.LoginShowButtonText);
                PasswordToggleButton.Visibility = ViewStates.Visible;
            }

            int selectionStart = PasswordEditText.SelectionStart;
            int selectionEnd = PasswordEditText.SelectionEnd;
            var passwordInputType = PasswordEditText.InputType;
            if (showPassword)
            {
                passwordInputType = (passwordInputType & ~InputTypes.TextVariationPassword) | InputTypes.TextVariationVisiblePassword;
            }
            else
            {
                passwordInputType = (passwordInputType & ~InputTypes.TextVariationVisiblePassword) | InputTypes.TextVariationPassword;
            }

            if (PasswordEditText.InputType != passwordInputType)
            {
                PasswordEditText.InputType = passwordInputType;
                PasswordEditText.SetFont(Font.RobotoLight);
                PasswordEditText.SetSelection(selectionStart, selectionEnd);
            }
        }

        private void OnLoginButtonClick(object sender, EventArgs e)
        {
            // Small UI trick to permit OBM testers
            // interact with the staging API
            if (EmailEditText.Text == "staging")
            {
                var isStaging = !Settings.IsStaging;
                Settings.IsStaging = isStaging;
                var msg = !isStaging ? "You're in Normal Mode" : "You're in Staging Mode";
                new AlertDialog.Builder(Activity)
                .SetTitle("Staging Mode")
                .SetMessage(msg + "\nRestart the app to continue.")
                .SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null)
                .Show();
                return;
            }
            if (ViewModel.IsAuthenticating) return;

            ViewModel.SetAuthenticating(true);

            if (ViewModel.CurrentLoginMode == LoginVM.LoginMode.Login)
            {
                if (ViewModel.IsEmailValid(EmailEditText.Text))
                {
                    if (Reducers.HasAnyData())
                    {
                        var confirm = new AreYouSureDialogFragment(this);
                        confirm.Show(FragmentManager, "confirm_reset_dialog");
                    }
                    else
                    {
                        StartLogin();
                    }
                }
                else
                {
                    ValidateEmailField(false);
                    ValidatePasswordField();
                }
            }
            else if (ViewModel.CurrentLoginMode == LoginVM.LoginMode.Signup)
            {
                if (ViewModel.IsPassValid(PasswordEditText.Text) && ViewModel.IsEmailValid(EmailEditText.Text))
                {
                    StartLogin();
                }
                else
                {
                    ValidatePasswordField(false);
                    ValidateEmailField(false);
                }

            }
        }

        public void StartLogin()
        {
            ViewModel.TryLogin(EmailEditText.Text, PasswordEditText.Text);
        }

        private void OnGoogleLoginButtonClick(object sender, EventArgs e)
        {
            mShouldResolve = true;
            Intent signInIntent = Auth.GoogleSignInApi.GetSignInIntent(mGoogleApiClient);
            StartActivityForResult(signInIntent, RC_SIGN_IN);
        }

        private void OnPasswordEditTextTextChanged(object sender, TextChangedEventArgs e)
        {
            SetPasswordVisibility();
        }

        private void OnEmailFocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            ValidateEmailField();
        }

        private void OnPasswordFocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            ValidatePasswordField();
        }

        private void OnPasswordToggleButtonClick(object sender, EventArgs e)
        {
            showPassword = !showPassword;
            SetPasswordVisibility();
        }
        #endregion

        #region Google Login methods

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == RC_SIGN_IN)
            {
                mIsResolving = false;
                GoogleSignInResult result = Auth.GoogleSignInApi.GetSignInResultFromIntent(data);
                if (result.IsSuccess)
                {
                    GoogleSignInAccount acct = result.SignInAccount;
                    ViewModel.TryLoginWithGoogle(acct.Id);
                }
                else
                {
                    ShowAuthError(string.Empty, AuthResult.GoogleError, LoginVM.LoginMode.Login);
                }
            }
        }

        public void OnConnected(Bundle connectionHint)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            log.Info(LogTag, "Login with Google. Success : " + connectionHint);
        }

        public void OnConnectionSuspended(int cause)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            log.Info(LogTag, new Exception(), "Failed to login with Google. onConnectionSuspended:" + cause);
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            log.Info(LogTag, "OnConnectionFailed:" + result);

            if (!mIsResolving && mShouldResolve)
            {
                if (result.HasResolution)
                {
                    try
                    {
                        result.StartResolutionForResult(Activity, RC_SIGN_IN);
                        mIsResolving = true;
                    }
                    catch (IntentSender.SendIntentException e)
                    {
                        log.Error(LogTag, e, "Could not resolve ConnectionResult.");
                        mIsResolving = false;
                        mGoogleApiClient.Connect();
                    }
                }
                else
                {
                    ShowAuthError(string.Empty, AuthResult.GoogleError, LoginVM.LoginMode.Login);
                }
            }
        }

        private List<string> GoogleAccounts
        {
            get
            {
                var am = AccountManager.Get(Activity);
                return am.GetAccounts()
                       .Where(a => a.Type == GoogleAuthUtil.GoogleAccountType)
                       .Select(a => a.Name)
                       .Distinct()
                       .ToList();
            }
        }
        #endregion

        private void ShowAuthError(string email, AuthResult res, LoginVM.LoginMode mode)
        {
            DialogFragment dia = null;

            switch (res)
            {
                case AuthResult.InvalidCredentials:
                    if (mode == LoginVM.LoginMode.Login)
                    {
                        ShowMsgDialog(Resource.String.LoginInvalidCredentialsDialogTitle, Resource.String.LoginInvalidCredentialsDialogText);
                    }
                    else if (mode == LoginVM.LoginMode.Signup)
                    {
                        ShowMsgDialog(Resource.String.LoginSignupFailedDialogTitle, Resource.String.LoginSignupFailedDialogText);
                    }
                    break;
                case AuthResult.NoGoogleAccount:
                    ShowMsgDialog(Resource.String.LoginNoAccountDialogTitle, Resource.String.LoginNoAccountDialogText);
                    break;
                case AuthResult.GoogleError:
                    ShowMsgDialog(Resource.String.LoginGoogleErrorTitle, Resource.String.LoginGoogleErrorText);
                    break;
                case AuthResult.NoDefaultWorkspace:
                    dia = new NoWorkspaceDialogFragment(email);
                    break;
                case AuthResult.NetworkError:
                    ShowMsgDialog(Resource.String.LoginNetworkErrorDialogTitle, Resource.String.LoginNetworkErrorDialogText);
                    break;
                default:
                    ShowMsgDialog(Resource.String.LoginSystemErrorDialogTitle, Resource.String.LoginSystemErrorDialogText);
                    break;
            }

            if (dia != null)
            {
                dia.Show(Activity.SupportFragmentManager, "auth_result_dialog");
            }
        }

        private void ShowMsgDialog(int title, int message)
        {
            var dialog = new AlertDialog.Builder(Activity)
            .SetTitle(title)
            .SetMessage(message)
            .SetPositiveButton(Resource.String.LoginInvalidCredentialsDialogOk, delegate {})
            .Create();
            dialog.Show();
        }

        public class AreYouSureDialogFragment : DialogFragment
        {
            private LoginFragment fragment;

            public AreYouSureDialogFragment(LoginFragment frag)
            {
                fragment = frag;
            }

            public override Dialog OnCreateDialog(Bundle savedInstanceState)
            {
                return new AlertDialog.Builder(Activity)
                       .SetTitle(Resource.String.SettingsClearDataTitle)
                       .SetMessage(Resource.String.SettingsClearDataText)
                       .SetPositiveButton(Resource.String.SettingsClearDataOKButton, OnOkButtonClicked)
                       .SetNegativeButton(Resource.String.SettingsClearDataCancelButton, OnCancelButtonClicked)
                       .Create();
            }

            private void OnCancelButtonClicked(object sender, DialogClickEventArgs args)
            {
                fragment.ViewModel.SetAuthenticating(false);
            }

            private async void OnOkButtonClicked(object sender, DialogClickEventArgs args)
            {
                fragment.StartLogin();
            }
        }

        public class NoWorkspaceDialogFragment : DialogFragment
        {
            private const string EmailKey = "com.toggl.timer.email";

            public NoWorkspaceDialogFragment()
            {
            }

            public NoWorkspaceDialogFragment(string email)
            {
                var args = new Bundle();
                args.PutString(EmailKey, email);

                Arguments = args;
            }

            private string Email
            {
                get
                {
                    if (Arguments == null)
                    {
                        return String.Empty;
                    }
                    return Arguments.GetString(EmailKey);
                }
            }

            public override Dialog OnCreateDialog(Bundle savedInstanceState)
            {
                return new AlertDialog.Builder(Activity)
                       .SetTitle(Resource.String.LoginNoWorkspaceDialogTitle)
                       .SetMessage(Resource.String.LoginNoWorkspaceDialogText)
                       .SetPositiveButton(Resource.String.LoginNoWorkspaceDialogOk, OnOkButtonClicked)
                       .SetNegativeButton(Resource.String.LoginNoWorkspaceDialogCancel, OnCancelButtonClicked)
                       .Create();
            }

            private void OnOkButtonClicked(object sender, DialogClickEventArgs args)
            {
                var intent = new Intent(Intent.ActionSend);
                intent.SetType("message/rfc822");
                intent.PutExtra(Intent.ExtraEmail, new[] { Resources.GetString(Resource.String.LoginNoWorkspaceDialogEmail) });
                intent.PutExtra(Intent.ExtraSubject, Resources.GetString(Resource.String.LoginNoWorkspaceDialogSubject));
                intent.PutExtra(Intent.ExtraText, string.Format(Resources.GetString(Resource.String.LoginNoWorkspaceDialogBody), Email));
                StartActivity(Intent.CreateChooser(intent, (string)null));
            }

            private void OnCancelButtonClicked(object sender, DialogClickEventArgs args)
            {
            }
        }

        private class TogglURLSPan : URLSpan
        {
            public TogglURLSPan(String url) : base(url)
            {
            }

            public override void UpdateDrawState(TextPaint ds)
            {
                base.UpdateDrawState(ds);
                ds.UnderlineText = true;
            }
        }


        private ISpannable FormattedLegalText
        {
            get
            {
                if (formattedLegalText == null)
                {
                    var template = Activity.Resources.GetText(Resource.String.LoginSignupLegalText);
                    var arg0 = Activity.Resources.GetText(Resource.String.LoginSignupLegalTermsText);
                    var arg1 = Activity.Resources.GetText(Resource.String.LoginSignupLegalPrivacyText);

                    var arg0idx = string.Format(template, "{0}", arg1).IndexOf("{0}", StringComparison.Ordinal);
                    var arg1idx = string.Format(template, arg0, "{1}").IndexOf("{1}", StringComparison.Ordinal);

                    var s = formattedLegalText = new SpannableString(string.Format(template, arg0, arg1));
                    var mode = SpanTypes.InclusiveExclusive;
                    s.SetSpan(
                        new TogglURLSPan(Phoebe.Build.TermsOfServiceUrl.ToString()),
                        arg0idx,
                        arg0idx + arg0.Length,
                        mode
                    );

                    s.SetSpan(
                        new TogglURLSPan(Phoebe.Build.PrivacyPolicyUrl.ToString()),
                        arg1idx,
                        arg1idx + arg1.Length,
                        mode
                    );
                }

                return formattedLegalText;
            }
        }
    }
}
