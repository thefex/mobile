using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Cirrious.FluentLayouts.Touch;
using CoreGraphics;
using Foundation;
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

        public static readonly int TimerPageId = 0;
        public static readonly int ReportsPageId = 1;
        public static readonly int SettingsPageId = 2;
        public static readonly int FeedbackPageId = 3;
        public static readonly int LogoutPageId = 4;

        private UIButton logButton;
        private UIButton reportsButton;
        private UIButton settingsButton;
        private UIButton feedbackButton;
        private UIButton signOutButton;
        private UIButton[] menuButtons;
        private UILabel usernameLabel;
        private UILabel emailLabel;

        private UIImageView userAvatarImage;
        private UIImageView separatorLineImage;
        private const int horizMargin = 15;
        private const int menuOffset = 60;
        private Action<int> buttonSelector;


        public LeftViewController(Action<int> buttonSelector)
        {
            this.buttonSelector = buttonSelector;
        }

        public override void LoadView()
        {
            base.LoadView();
            View.BackgroundColor = UIColor.White;

            menuButtons = new[]
            {
                (logButton = new UIButton()),
                (reportsButton = new UIButton()),
                (settingsButton = new UIButton()),
                (feedbackButton = new UIButton()),
                (signOutButton = new UIButton()),
            };
            logButton.SetTitle(NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString(), UIControlState.Normal);
            logButton.SetImage(Image.TimerButton, UIControlState.Normal);
            logButton.SetImage(Image.TimerButtonPressed, UIControlState.Highlighted);

            reportsButton.SetTitle("LeftPanelMenuReports".Tr(), UIControlState.Normal);
            reportsButton.SetImage(Image.ReportsButton, UIControlState.Normal);
            reportsButton.SetImage(Image.ReportsButtonPressed, UIControlState.Highlighted);

            settingsButton.SetTitle("LeftPanelMenuSettings".Tr(), UIControlState.Normal);
            settingsButton.SetImage(Image.SettingsButton, UIControlState.Normal);
            settingsButton.SetImage(Image.SettingsButtonPressed, UIControlState.Highlighted);

            feedbackButton.SetTitle("LeftPanelMenuFeedback".Tr(), UIControlState.Normal);
            feedbackButton.SetImage(Image.FeedbackButton, UIControlState.Normal);
            feedbackButton.SetImage(Image.FeedbackButtonPressed, UIControlState.Highlighted);

            signOutButton.SetTitle("LeftPanelMenuSignOut".Tr(), UIControlState.Normal);
            signOutButton.SetImage(Image.SignoutButton, UIControlState.Normal);
            signOutButton.SetImage(Image.SignoutButtonPressed, UIControlState.Highlighted);

            logButton.HorizontalAlignment = reportsButton.HorizontalAlignment = settingsButton.HorizontalAlignment =
                                                feedbackButton.HorizontalAlignment = signOutButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;

            foreach (var button in menuButtons)
            {
                button.Apply(Style.LeftView.Button);
                button.TouchUpInside += OnMenuButtonTouchUpInside;
                View.AddSubview(button);
            }

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            View.AddConstraints(MakeConstraints(View));
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            usernameLabel = new UILabel().Apply(Style.LeftView.UserLabel);
            var imageStartingPoint = View.Frame.Width - menuOffset - 90f;
            usernameLabel.Frame = new CGRect(40, View.Frame.Height - 110f, height: 50f, width: imageStartingPoint - 40f);
            View.AddSubview(usernameLabel);
            emailLabel = new UILabel().Apply(Style.LeftView.EmailLabel);
            emailLabel.Frame = new CGRect(40f, View.Frame.Height - 80f, height: 50f, width: imageStartingPoint - 40f);
            View.AddSubview(emailLabel);

            userAvatarImage = new UIImageView(
                new CGRect(
                    imageStartingPoint,
                    View.Frame.Height - 100f,
                    60f,
                    60f
                ));
            userAvatarImage.Layer.CornerRadius = 30f;
            userAvatarImage.Layer.MasksToBounds = true;
            View.AddSubview(userAvatarImage);

            separatorLineImage = new UIImageView(UIImage.FromFile("line.png"));
            separatorLineImage.Frame = new CGRect(0f, View.Frame.Height - 140f, height: 1f, width: View.Frame.Width - menuOffset);
            if (View.Frame.Height > 480)
            {
                View.AddSubview(separatorLineImage);
            }

            // Set default values
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
                buttonSelector.Invoke(TimerPageId);
            }
            else if (sender == reportsButton)
            {
                buttonSelector.Invoke(ReportsPageId);
            }
            else if (sender == settingsButton)
            {
                buttonSelector.Invoke(SettingsPageId);
            }
            else if (sender == feedbackButton)
            {
                buttonSelector.Invoke(FeedbackPageId);
            }
            else
            {
                buttonSelector.Invoke(LogoutPageId);
            }
        }

        public nfloat MaxDraggingX
        {
            get
            {
                return View.Frame.Width - menuOffset;
            }
        }

        public nfloat MinDraggingX
        {
            get
            {
                return 0;
            }
        }

        private static IEnumerable<FluentLayout> MakeConstraints(UIView container)
        {
            UIView prev = null;
            const float startTopMargin = 60.0f;
            const float topMargin = 7f;

            foreach (var view in container.Subviews)
            {
                if (!(view is UIButton))
                {
                    continue;
                }

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
    }
}
