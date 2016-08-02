using UIKit;

namespace Toggl.Ross.Theme
{
    public static class Image
    {
        public static UIImage LoginBackground
            => UIImage.FromBundle("bg");

        public static UIImage TagBackground
            => UIImage.FromBundle("bg-tag").CreateResizableImage(
                           new UIEdgeInsets(5f, 5f, 5f, 5f), UIImageResizingMode.Tile);

        public static UIImage TogglLogo => UIImage.FromBundle("togglLogo");

        public static UIImage CircleStart
            => UIImage.FromBundle("circle-start");

        public static UIImage CircleStartPressed
            => UIImage.FromBundle("circle-start-pressed");

        public static UIImage CircleStop
            => UIImage.FromBundle("circle-stop");

        public static UIImage CircleStopPressed
            => UIImage.FromBundle("circle-stop-pressed");

        public static UIImage IconArrowRight
            => UIImage.FromBundle("icon-arrow-right");

        public static UIImage IconBack
            => UIImage.FromBundle("icon-back");

        public static UIImage IconBillable
            => UIImage.FromBundle("iconBillable");

        public static UIImage IconCancel
            => UIImage.FromBundle("icon-cancel");

        public static UIImage IconDurationArrow
            => UIImage.FromBundle("icon-duration-arrow");

        public static UIImage IconNav
            => UIImage.FromBundle("icon-nav");

        public static UIImage IconNegative
            => UIImage.FromBundle("icon-negative");

        public static UIImage IconNegativeFilled
            => UIImage.FromBundle("icon-negative-filled");

        public static UIImage IconNeutral
            => UIImage.FromBundle("icon-neutral");

        public static UIImage IconNeutralFilled
            => UIImage.FromBundle("icon-neutral-filled");

        public static UIImage IconPositive
            => UIImage.FromBundle("icon-positive");

        public static UIImage IconPositiveFilled
            => UIImage.FromBundle("icon-positive-filled");

        public static UIImage IconRetry
            => UIImage.FromBundle("icon-retry");

        public static UIImage IconRunning
            => UIImage.FromBundle("icon-running");

        public static UIImage IconTag
            => UIImage.FromBundle("iconTag");

        public static UIImage Logo
            => UIImage.FromBundle("logo");

        public static UIImage ArrowEmptyState
            => UIImage.FromBundle("iconArrowDown");

        public static UIImage TimerButton
            => UIImage.FromBundle("icon-timer");

        public static UIImage TimerButtonPressed
            => UIImage.FromBundle("icon-timer-green");

        public static UIImage ReportsButton
            => UIImage.FromBundle("icon-reports");

        public static UIImage ReportsButtonPressed
            => UIImage.FromBundle("icon-reports-green");

        public static UIImage SettingsButton
            => UIImage.FromBundle("icon-settings");

        public static UIImage SettingsButtonPressed
            => UIImage.FromBundle("icon-settings-green");

        public static UIImage FeedbackButton
            => UIImage.FromBundle("icon-feedback");

        public static UIImage FeedbackButtonPressed
            => UIImage.FromBundle("icon-feedback-green");

        public static UIImage SignoutButton
            => UIImage.FromBundle("icon-logout");

        public static UIImage SignoutButtonPressed
            => UIImage.FromBundle("icon-logout-green");

        public static UIImage AlreadyGotAnAccount
            => UIImage.FromBundle("alreadyGotAnAccou");

        public static UIImage HeyThere
            => UIImage.FromBundle("heyThere");

        public static UIImage IconHelloArrowDown
            => UIImage.FromBundle("iconHelloArrowDown");

        public static UIImage IconHelloArrowUp
            => UIImage.FromBundle("iconHelloArrowUp");

        public static UIImage IconHelloToggler
            => UIImage.FromBundle("iconHelloToggler");

        public static UIImage NewToToggl
            => UIImage.FromBundle("newToToggl");

        public static UIImage Toggler
            => UIImage.FromBundle("toggler2");

        public static UIImage LoginButton
            => UIImage.FromBundle("LoginButton");

        public static UIImage LoginButtonPressed
            => UIImage.FromBundle("LoginButtonPressed");

        public static UIImage SignUpButton
            => UIImage.FromBundle("SignUpButton");

        public static UIImage SignUpButtonPressed
            => UIImage.FromBundle("SignUpButtonPressed");
    }
}