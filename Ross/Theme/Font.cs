using UIKit;

namespace Toggl.Ross.Theme
{
    public static class Font
    {
        public static UIFont Main(float height) => UIFont.SystemFontOfSize(height, UIFontWeight.Regular);
        public static UIFont MainLight(float height) => UIFont.SystemFontOfSize(height, UIFontWeight.Light);

        public static UIFont MinispacedDigits(float height) => UIFont.MonospacedDigitSystemFontOfSize(height, UIFontWeight.Regular);
        public static UIFont MinispacedDigitsLight(float height) => UIFont.MonospacedDigitSystemFontOfSize(height, UIFontWeight.Light);
    }
}

