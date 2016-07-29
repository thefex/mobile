using System;
using CoreAnimation;
using CoreGraphics;
using Toggl.Ross.Views;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Timer
        {
            public static void StartButtonCircle(UIView v)
            {
                v.BackgroundColor = Color.StartButton;
                v.Layer.CornerRadius = 20;
                v.ClipsToBounds = true;
            }

            private static readonly UIFont labelFont = Font.Main(12);

            public static void TimerModeSwitchLabelInactive(UILabel v)
            {
                v.TextColor = Color.TextInactive;
                v.Font = labelFont;
            }
            public static void TimerModeSwitchLabelTimer(UILabel v)
            {
                v.TextColor = Color.StartButton;
                v.Font = labelFont;
            }
            public static void TimerModeSwitchLabelManual(UILabel v)
            {
                v.TextColor = Color.AddManualButton;
                v.Font = labelFont;
            }

            public static void TimerModeSwitch(UISwitch v)
            {
                v.OnTintColor = Color.AddManualButton;
                v.TintColor = Color.StartButton;
                v.BackgroundColor = Color.StartButton;
                v.Transform = new CoreGraphics.CGAffineTransform(0.54f, 0f, 0f, 0.54f, 0, 0);

                v.Layer.CornerRadius = 18;
            }

            public static void Bar(TimerBar v)
            {
                v.ClipsToBounds = true;
                v.BarTintColor = Color.Background;
            }

            public static void Border(UIView v)
            {
                v.BackgroundColor = Color.Border;
            }

            public static void TimerDurationLabel(UILabel v)
            {
                v.Text = "00:00:00";
                v.Font = Font.MonospacedDigitsLight(24);
                v.TextColor = Color.OffSteel;
                v.TextAlignment = UITextAlignment.Right;
            }


            public static void StartButtonHighlight(CALayer l)
            {
                l.Frame = new CGRect(0, 0, 40, 40);
                l.BackgroundColor = Color.Black.CGColor;
                l.Opacity = 0.2f;
                l.Hidden = true;
            }
        }

    }
}

