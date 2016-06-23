using UIKit;

namespace Toggl.Ross.Theme
{
    public static class Image
    {
        public static UIImage LoginBackground
        {
            get { return UIImage.FromBundle("bg"); }
        }

        public static UIImage TagBackground
        {
            get
            {
                return UIImage.FromBundle("bg-tag").CreateResizableImage(
                           new UIEdgeInsets(5f, 5f, 5f, 5f), UIImageResizingMode.Tile);
            }
        }

        public static UIImage TogglLogo => UIImage.FromBundle("togglLogo");

        public static UIImage CircleStart
        {
            get { return UIImage.FromBundle("circle-start"); }
        }

        public static UIImage CircleStartPressed
        {
            get { return UIImage.FromBundle("circle-start-pressed"); }
        }

        public static UIImage CircleStop
        {
            get { return UIImage.FromBundle("circle-stop"); }
        }

        public static UIImage CircleStopPressed
        {
            get { return UIImage.FromBundle("circle-stop-pressed"); }
        }

        public static UIImage IconArrowRight
        {
            get { return UIImage.FromBundle("icon-arrow-right"); }
        }

        public static UIImage IconBack
        {
            get { return UIImage.FromBundle("icon-back"); }
        }

        public static UIImage IconBillable
        {
            get { return UIImage.FromBundle("iconBillable"); }
        }

        public static UIImage IconCancel
        {
            get { return UIImage.FromBundle("icon-cancel"); }
        }

        public static UIImage IconDurationArrow
        {
            get { return UIImage.FromBundle("icon-duration-arrow"); }
        }

        public static UIImage IconNav
        {
            get { return UIImage.FromBundle("icon-nav"); }
        }

        public static UIImage IconNegative
        {
            get { return UIImage.FromBundle("icon-negative"); }
        }

        public static UIImage IconNegativeFilled
        {
            get { return UIImage.FromBundle("icon-negative-filled"); }
        }

        public static UIImage IconNeutral
        {
            get { return UIImage.FromBundle("icon-neutral"); }
        }

        public static UIImage IconNeutralFilled
        {
            get { return UIImage.FromBundle("icon-neutral-filled"); }
        }

        public static UIImage IconPositive
        {
            get { return UIImage.FromBundle("icon-positive"); }
        }

        public static UIImage IconPositiveFilled
        {
            get { return UIImage.FromBundle("icon-positive-filled"); }
        }

        public static UIImage IconRetry
        {
            get { return UIImage.FromBundle("icon-retry"); }
        }

        public static UIImage IconRunning
        {
            get { return UIImage.FromBundle("icon-running"); }
        }

        public static UIImage IconTag
        {
            get { return UIImage.FromBundle("iconTag"); }
        }

        public static UIImage Logo
        {
            get { return UIImage.FromBundle("logo"); }
        }

        public static UIImage ArrowEmptyState
        {
            get { return UIImage.FromBundle("arrowUp"); }
        }

        public static UIImage TimerButton
        {
            get { return UIImage.FromBundle("icon-timer"); }
        }

        public static UIImage TimerButtonPressed
        {
            get { return UIImage.FromBundle("icon-timer-green"); }
        }

        public static UIImage ReportsButton
        {
            get { return UIImage.FromBundle("icon-reports"); }
        }

        public static UIImage ReportsButtonPressed
        {
            get { return UIImage.FromBundle("icon-reports-green"); }
        }

        public static UIImage SettingsButton
        {
            get { return UIImage.FromBundle("icon-settings"); }
        }

        public static UIImage SettingsButtonPressed
        {
            get { return UIImage.FromBundle("icon-settings-green"); }
        }

        public static UIImage FeedbackButton
        {
            get { return UIImage.FromBundle("icon-feedback"); }
        }

        public static UIImage FeedbackButtonPressed
        {
            get { return UIImage.FromBundle("icon-feedback-green"); }
        }

        public static UIImage SignoutButton
        {
            get { return UIImage.FromBundle("icon-logout"); }
        }

        public static UIImage SignoutButtonPressed
        {
            get { return UIImage.FromBundle("icon-logout-green"); }
        }
    }
}
