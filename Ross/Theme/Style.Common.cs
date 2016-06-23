using System;
using UIKit;
using Toggl.Ross.Views;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static void Screen(UIView v)
        {
            v.BackgroundColor = Color.LightestGray;
        }

        public static void NavigationBar(UINavigationBar v)
        {
            v.BarTintColor = Color.BackgroundToolbar;
            var borderImage = v.GetBorderImage();
            if (borderImage != null)
            {
                borderImage.Hidden = true;
            }
        }

        public static void Toolbar(UIToolbar v)
        {
            v.BarTintColor = Color.BackgroundToolbar;
        }

        public static void NavLabelButton(UIBarButtonItem v)
        {
            v.SetTitleTextAttributes(new UITextAttributes
            {
                Font = UIFont.FromName("HelveticaNeue-Medium", 17f),
                TextColor = Color.Green,
            }, UIControlState.Normal);
        }

        public static void DisableNavLabelButton(UIBarButtonItem v)
        {
            v.SetTitleTextAttributes(new UITextAttributes
            {
                Font = UIFont.FromName("HelveticaNeue-Medium", 17f),
                TextColor = Color.Green.ColorWithAlpha(0.3f),
            }, UIControlState.Normal);
        }

        public static void TableViewHeader(UIView v)
        {
            v.BackgroundColor = Color.Background;
        }

        public static void CellSelectedBackground(UIView v)
        {
            v.BackgroundColor = Color.LightestGray;
        }
    }
}
