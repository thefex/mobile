using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class OBMEmptyView
        {
            public static void TitleLabel(UILabel v)
            {
                v.Font = Font.Main(20);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.OffBlack;
            }

            public static void MessageLabel(UILabel v)
            {
                v.Font = Font.Main(16);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.OffSteel;
            }

            public static void ArrowImageView(UIImageView v)
            {
                v.Image = Image.ArrowEmptyState;
            }

            public static void TogglerImageView(UIImageView v)
            {
                v.Image = Image.Toggler;
            }
        }
    }
}
