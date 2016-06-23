using UIKit;

namespace Toggl.Ross.Theme
{
    public static class Color
    {
        public static readonly UIColor Black = UIColor.Black;
        public static readonly UIColor LightestGray = UIColor.FromRGB(0xF6, 0xF6, 0xF6);
        public static readonly UIColor LightGray = UIColor.FromRGB(0xBB, 0xBB, 0xBB);
        public static readonly UIColor Gray = UIColor.FromRGB(0x87, 0x87, 0x87);
        public static readonly UIColor DarkGray = UIColor.FromRGB(0x2D, 0x2D, 0x2D);
        public static readonly UIColor Green = UIColor.FromRGB(0x43, 0xD5, 0x52);
        public static readonly UIColor DarkGreen = UIColor.FromRGB(0x28, 0x80, 0x31);
        public static readonly UIColor Red = UIColor.FromRGB(0xFB, 0x20, 0x25);
        public static readonly UIColor DarkRed = UIColor.FromRGB(0xD7, 0x00, 0x08);
        public static readonly UIColor White = UIColor.White;
        public static readonly UIColor DonutInactiveGray = UIColor.FromRGB(0xDB, 0xDB, 0xDB);
        public static readonly UIColor TimeBarColor = UIColor.FromRGB(0x81, 0xD3, 0xF9);
        public static readonly UIColor TimeBarBoderColor = UIColor.FromRGB(0xe6, 0xe6, 0xe6);
        public static readonly UIColor MoneyBarColor = UIColor.FromRGB(0x03, 0xA9, 0xF3);
        public static readonly UIColor ChartTopLabel = UIColor.FromRGB(0x5c, 0x5c, 0x5c);

        // Migration screens:
        public static readonly UIColor DarkestGray = UIColor.FromRGB(46, 46, 46);
        public static readonly UIColor Steel = UIColor.FromRGB(142, 142, 142);
        public static readonly UIColor LightishGreen = UIColor.FromRGB(76, 217, 100);
        public static readonly UIColor DarkMint = UIColor.FromRGB(76, 190, 100);
        public static readonly UIColor PinkishGrey = UIColor.FromRGB(206, 206, 206);

        // Redesign:
        public static readonly UIColor OffSteel = rgb(142, 142, 147);
        public static readonly UIColor OffBlack = rgb(46, 46, 46);

        public static readonly UIColor StartButton = rgb(76, 217, 100);
        public static readonly UIColor StopButton = rgb(255, 59, 48);
        public static readonly UIColor AddManualButton = rgb(142, 142, 147);

        public static readonly UIColor Background = rgb(250, 251, 252);
        public static readonly UIColor BackgroundToolbar = rgb(251, 252, 253); // +1 compared to background to offset transparent effect
        public static readonly UIColor Border = rgb(236, 237, 237);
        public static readonly UIColor Transparent = rgb(0, 0, 0, 0);

        public static readonly UIColor TextInactive = rgb(142, 142, 147, 0.5);



        private static UIColor rgb(byte r, byte g, byte b)
        {
            return UIColor.FromRGB(r, g, b);
        }
        private static UIColor rgb(byte r, byte g, byte b, double a)
        {
            return UIColor.FromRGBA(r, g, b, (int)(255 * a));
        }
    }
}
