using UIKit;

namespace Toggl.Ross.Theme
{
    public static class Font
    {
        // supporting multiple versions of ios makes code sad.
        private static readonly bool ios82available = UIDevice.CurrentDevice.CheckSystemVersion(8, 2);
        private static readonly bool ios90available = UIDevice.CurrentDevice.CheckSystemVersion(9, 0);


        public static UIFont Main(float height) => getMainFont(height, UIFontWeight.Regular);
        public static UIFont MainLight(float height) => getMainFont(height, UIFontWeight.Light);

        public static UIFont MonospacedDigits(float height) => getMonoSpacedDigitFont(height, UIFontWeight.Regular);
        public static UIFont MonospacedDigitsLight(float height) => getMonoSpacedDigitFont(height, UIFontWeight.Light);

        private static UIFont getMainFont(float height, UIFontWeight weight)
        {
            if (ios82available)
            {
                return UIFont.SystemFontOfSize(height, weight);
            }
            return getFallbackFont(height, weight);
        }

        private static UIFont getMonoSpacedDigitFont(float height, UIFontWeight weight)
        {
            if (ios90available)
            {
                return UIFont.MonospacedDigitSystemFontOfSize(height, weight);
            }
            if (ios82available)
            {
                return UIFont.SystemFontOfSize(height, weight).withMonospacedDigits();
            }
            return getFallbackFont(height, weight);
        }

        private static UIFont getFallbackFont(float height, UIFontWeight weight)
        {
            switch (weight)
            {
                case UIFontWeight.Regular:
                    return UIFont.FromName("HelveticaNeue", height);
                case UIFontWeight.Light:
                    return UIFont.FromName("HelveticaNeue-Light", height);
                default:
                    throw new System.Exception($"font weight '{weight}' not supported");
            }
        }

        private static UIFont withMonospacedDigits(this UIFont font)
        {
            var descriptor = font.FontDescriptor;

            var feature = new UIFontFeature(CoreText.CTFontFeatureNumberSpacing.Selector.MonospacedNumbers);

            var attribute = new UIFontAttributes(feature);

            var d = descriptor.CreateWithAttributes(attribute);

            return UIFont.FromDescriptor(d, font.FontDescriptor.Size.Value);
        }

    }
}

