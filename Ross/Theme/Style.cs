using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static void Initialize()
        {
            // Set global appearances here:
            // UIView.Appearance.TintColor = UIColor.Red;

            UINavigationBar.Appearance.SetTitleTextAttributes(new UITextAttributes()
            {
                Font = UIFont.FromName("HelveticaNeue", 20f),
            });
            UINavigationBar.Appearance.TintColor = Color.Gray;
            UINavigationBar.Appearance.BackgroundColor = Color.LightestGray;

            UIBarButtonItem.AppearanceWhenContainedIn(typeof(UISearchBar)).TintColor = Color.LightishGreen;
        }
    }
}
