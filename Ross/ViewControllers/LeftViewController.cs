using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cirrious.FluentLayouts.Touch;
using CoreGraphics;
using Foundation;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public sealed class LeftViewController : UIViewController
    {
        private static string DefaultUserName = "Loading...";
        private static string DefaultUserEmail = "Loading...";
        private static string DefaultImage = "profile.png";
        private static string DefaultRemoteImage = "https://assets.toggl.com/images/profile.png";

        public enum MenuOption
        {
            Timer = 0,
            Reports = 1,
            Settings = 2,
            Feedback = 3,
            Logout = 4,
            Login = 5,
            SignUp = 6
        }

        private UIButton logButton;
        private UIButton reportsButton;
        private UIButton settingsButton;
        private UIButton feedbackButton;
        private UIButton signOutButton;
        private UIButton loginButton;
        private UIButton signUpButton;
        private UIButton[] menuButtons;
        private UILabel usernameLabel;
        private UILabel emailLabel;

        private UIImageView userAvatarImage;
        private UIImageView separatorLineImage;
        private const int horizMargin = 15;
        private const int menuOffset = 60;
        private Action<MenuOption> buttonSelector;

        private IDisposable observer;

        public LeftViewController(Action<MenuOption> buttonSelector)
        {
            this.buttonSelector = buttonSelector;

            observer = StoreManager.Singleton
                            .Observe(x => x.State.User)
                            .StartWith(StoreManager.Singleton.AppState.User)
                            .ObserveOn(SynchronizationContext.Current)
                            .DistinctUntilChanged(x => x.Id)
                            .Subscribe(OnUserChanged);
        }

        public override void LoadView()
        {
            base.LoadView();
            View.BackgroundColor = UIColor.White;

            menuButtons = new[]
            {
                logButton = CreateDrawerButton("LeftPanelMenuLog", Image.TimerButton, Image.TimerButtonPressed),
                reportsButton = CreateDrawerButton("LeftPanelMenuReports", Image.ReportsButton, Image.ReportsButtonPressed),
                settingsButton = CreateDrawerButton("LeftPanelMenuSettings", Image.SettingsButton, Image.SettingsButtonPressed),
                feedbackButton = CreateDrawerButton("LeftPanelMenuFeedback", Image.FeedbackButton, Image.FeedbackButtonPressed),
                signOutButton = CreateDrawerButton("LeftPanelMenuSignOut", Image.SignoutButton, Image.SignoutButtonPressed),
                loginButton = CreateDrawerButton("LeftPanelMenuLogin", Image.LoginButton, Image.LoginButtonPressed),
                signUpButton = CreateDrawerButton("LeftPanelMenuSignUp", Image.SignUpButton, Image.SignUpButtonPressed),
            };

            logButton.SetImage(Image.TimerButtonPressed, UIControlState.Normal);
            logButton.SetTitleColor(Color.LightishGreen, UIControlState.Normal);

            View.AddSubview(usernameLabel = new UILabel().Apply(Style.LeftView.UserLabel));
            View.AddSubview(emailLabel = new UILabel().Apply(Style.LeftView.EmailLabel));

            userAvatarImage = new UIImageView();
            userAvatarImage.Layer.CornerRadius = 30f;
            userAvatarImage.Layer.MasksToBounds = true;
            View.AddSubview(userAvatarImage);

            separatorLineImage = new UIImageView(UIImage.FromFile("line.png"));
            if (View.Frame.Height > 480)
            {
                View.AddSubview(separatorLineImage);
            }

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            View.AddConstraints(MakeConstraints(View));
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            if (!NoUserHelper.IsLoggedIn)
            {
                emailLabel.Hidden = true;
				loginButton.Hidden = false;
                signUpButton.Hidden = false;
                signOutButton.Hidden = true;
                usernameLabel.Hidden = true;
                userAvatarImage.Hidden = true;
                separatorLineImage.Hidden = true;
                return;
            }

            emailLabel.Hidden = false;
			loginButton.Hidden = true;
			signUpButton.Hidden = true;
            signOutButton.Hidden = false;
            usernameLabel.Hidden = false;
            userAvatarImage.Hidden = false;
            separatorLineImage.Hidden = false;
        }

        private UIButton CreateDrawerButton(string text, UIImage normalImage, UIImage pressedImage)
        {
            var button = new UIButton();
            button.SetTitle(text.Tr(), UIControlState.Normal);
            button.SetImage(normalImage, UIControlState.Normal);
            button.SetImage(pressedImage, UIControlState.Highlighted);
            button.SetTitleColor(Color.LightishGreen, UIControlState.Highlighted);
            button.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
            button.Apply(Style.LeftView.Button);
            button.TouchUpInside += OnMenuButtonTouchUpInside;
            View.AddSubview(button);
            return button;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

			ConfigureUserData(DefaultUserName, DefaultUserEmail, DefaultImage);
        }

        public async void ConfigureUserData(string name, string email, string imageUrl)
        {
            usernameLabel.Text = name;
            emailLabel.Text = email;
            UIImage image;

            if (imageUrl == DefaultImage || imageUrl == DefaultRemoteImage)
            {
                userAvatarImage.Image = UIImage.FromFile(DefaultImage);
                return;
            }

            // Try to download the image from server
            // if user doesn't have image configured or
            // there is not connection, use a local image.
            try
            {
                image = await LoadImage(imageUrl);
            }
            catch
            {
                image = UIImage.FromFile(DefaultImage);
            }

            userAvatarImage.Image = image;
        }

        private void OnMenuButtonTouchUpInside(object sender, EventArgs e)
        {
            if (buttonSelector == null)
                return;

            if (sender == logButton)
            {
                buttonSelector.Invoke(MenuOption.Timer);
            }
            else if (sender == reportsButton)
            {
                buttonSelector.Invoke(MenuOption.Reports);
            }
            else if (sender == settingsButton)
            {
                buttonSelector.Invoke(MenuOption.Settings);
            }
            else if (sender == feedbackButton)
            {
                buttonSelector.Invoke(MenuOption.Feedback);
            }
            else if (sender == loginButton)
            {
                buttonSelector.Invoke(MenuOption.Login);
            }
            else if (sender == signUpButton)
            {
                buttonSelector.Invoke(MenuOption.SignUp);
            }
            else
            {
                buttonSelector.Invoke(MenuOption.Logout);
            }
        }

        public nfloat MinDraggingX => 0;

        public nfloat MaxDraggingX => View.Frame.Width - menuOffset;

        private IEnumerable<FluentLayout> MakeConstraints(UIView container)
        {
            UIView prev = null;
            const float startTopMargin = 60.0f;
            const float topMargin = 7f;

            //Common buttons
            foreach (var view in container.Subviews.OfType<UIButton>().Take(4))
            {
                if (prev == null)
                {
                    yield return view.AtTopOf(container, topMargin + startTopMargin);
                }
                else
                {
                    yield return view.Below(prev, topMargin);
                }

                yield return view.AtLeftOf(container, horizMargin);
                yield return view.AtRightOf(container, horizMargin + 20);

                prev = view;
            }

            //Case for logged users
            yield return signOutButton.Below(prev, topMargin);
            yield return signOutButton.AtLeftOf(container, horizMargin);
            yield return signOutButton.AtRightOf(container, horizMargin + 20);

            //Case for non-logged users
            yield return loginButton.Below(prev, topMargin);
            yield return loginButton.AtLeftOf(container, horizMargin);
            yield return loginButton.AtRightOf(container, horizMargin + 20);

            yield return signUpButton.Below(loginButton, topMargin);
            yield return signUpButton.AtLeftOf(container, horizMargin);
            yield return signUpButton.AtRightOf(container, horizMargin + 20);

            //Bottom part of the drawer
            const int bottomInformationMargin = 33;

            if (separatorLineImage.Superview != null)
            {
				yield return separatorLineImage.Above(userAvatarImage, 22);
            }

			yield return userAvatarImage.Width().EqualTo(60);
			yield return userAvatarImage.Height().EqualTo(60);
            yield return userAvatarImage.AtRightOf(container, startTopMargin + bottomInformationMargin);
			yield return userAvatarImage.AtBottomOf(container, bottomInformationMargin);

            yield return emailLabel.AtBottomOf(container, bottomInformationMargin + 5);
            yield return emailLabel.AtLeftOf(container, bottomInformationMargin);

            yield return usernameLabel.Above(emailLabel, 10);
            yield return usernameLabel.AtLeftOf(container, bottomInformationMargin);
        }

        private async Task<UIImage> LoadImage(string imageUrl)
        {
            var httpClient = new HttpClient();

            Task<byte[]> contentsTask = httpClient.GetByteArrayAsync(imageUrl);

            // await! control returns to the caller and the task continues to run on another thread
            var contents = await contentsTask;

            // load from bytes
            return UIImage.LoadFromData(NSData.FromArray(contents));
        }

        private void OnUserChanged(IUserData data)
            => UpdateLayout();
    }
}
