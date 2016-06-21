using System.Linq;
using UIKit;

namespace Toggl.Ross
{
    static class UIExtensions
    {
        public static void PushViewControllers(this UINavigationController controller, bool animated, params UIViewController[] viewControllers)
        {
            controller.SetViewControllers(
                controller.ViewControllers
                .Concat(viewControllers.Where(vc => vc != null))
                .ToArray(),
                animated);
        }

        public static void PopViewControllers(this UINavigationController controller, bool animated, int count)
        {
            var vcs = controller.ViewControllers;

            controller.SetViewControllers(vcs.Take(vcs.Length - count).ToArray(), animated);
        }

        public static T WithDebugBackground<T>(this T view)
        where T : UIView
        {
#if DEBUG
            view.BackgroundColor = new UIColor(1, 0, 0, 0.3f);
#endif
            return view;
        }
    }
}

