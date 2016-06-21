using System;
using UIKit;
using Toggl.Phoebe.Data;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using Toggl.Phoebe;

namespace Toggl.Ross.ViewControllers
{
    public class DurationChangeViewController : UIViewController
    {
        public interface IChangeDuration
        {
            void OnChangeDuration(TimeSpan newDuration);
        }

        private DurationView durationView;
        private UIBarButtonItem barButtonItem;
        private TimeSpan duration;
        private readonly IChangeDuration handler;

        public DurationChangeViewController(EditTimeEntryViewController editView)
        {
            duration = GetDuration(editView.StopDate, editView.StartDate);
            this.handler = editView;
        }

        public override void LoadView()
        {
            View = new UIImageView()
            {
                BackgroundColor = Color.Black,
                ContentMode = UIViewContentMode.Bottom,
            };
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Configure navigation item
            NavigationItem.TitleView = durationView = new DurationView()
            {
                Hint = new Duration(duration.Hours, duration.Minutes),
            } .Apply(Style.DurationEdit.DurationView);
            durationView.DurationChanged += (s, e) => Rebind();
            durationView.SizeToFit();

            barButtonItem = new UIBarButtonItem(
                "DurationSet".Tr(),
                UIBarButtonItemStyle.Plain,
                OnNavigationBarRightClicked
            ).Apply(Style.NavLabelButton);

            Rebind();
        }

        private void Rebind()
        {
            var isValid = durationView.EnteredDuration != Duration.Zero && durationView.EnteredDuration.IsValid;
            NavigationItem.RightBarButtonItem = isValid ? barButtonItem : null;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            durationView.BecomeFirstResponder();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            durationView.ResignFirstResponder();
        }

        private void OnNavigationBarRightClicked(object sender, EventArgs args)
        {
            var entered = durationView.EnteredDuration;
            duration = new TimeSpan(entered.Hours, entered.Minutes, 0);
            handler.OnChangeDuration(duration);
            NavigationController.PopViewController(true);
        }

        private TimeSpan GetDuration(DateTime stopTime, DateTime startTime)
        {
            if (startTime.IsMinValue())
            {
                return TimeSpan.Zero;
            }

            var duration = stopTime - startTime;
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }
            return duration;
        }

        #region Navigation classes

        public Type PreviousControllerType { get; set; }

        private static UIImage GetBlurred(UIView view)
        {
            UIGraphics.BeginImageContextWithOptions(view.Bounds.Size, true, view.Window.Screen.Scale);

            var frame = view.Frame;
            frame.Y = 0;
            view.DrawViewHierarchy(frame, false);

            var snapshotImage = UIGraphics.GetImageFromCurrentImageContext();
            snapshotImage = snapshotImage.ApplyDarkEffect();

            UIGraphics.EndImageContext();

            return snapshotImage;
        }

        public class PushAnimator : UIViewControllerAnimatedTransitioning
        {
            public override void AnimateTransition(IUIViewControllerContextTransitioning transitionContext)
            {
                var toController = (DurationChangeViewController)transitionContext.GetViewControllerForKey(UITransitionContext.ToViewControllerKey);
                var fromController = transitionContext.GetViewControllerForKey(UITransitionContext.FromViewControllerKey);
                var container = transitionContext.ContainerView;

                var imageView = (UIImageView)toController.View;
                imageView.Image = GetBlurred(fromController.View);

                toController.View.Frame = transitionContext.GetFinalFrameForViewController(toController);
                toController.View.Alpha = 0;
                container.InsertSubviewAbove(toController.View, fromController.View);

                UIView.Animate(TransitionDuration(transitionContext)
                               , () => toController.View.Alpha = 1
                                       , () =>
                {
                    if (!transitionContext.TransitionWasCancelled)
                    {
                        fromController.View.RemoveFromSuperview();
                    }
                    transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled);
                });
            }

            public override double TransitionDuration(IUIViewControllerContextTransitioning transitionContext)
            {
                return 0.4f;
            }
        }

        public class PopAnimator : UIViewControllerAnimatedTransitioning
        {
            public override void AnimateTransition(IUIViewControllerContextTransitioning transitionContext)
            {
                var toController = transitionContext.GetViewControllerForKey(UITransitionContext.ToViewControllerKey);
                var fromController = (DurationChangeViewController)transitionContext.GetViewControllerForKey(UITransitionContext.FromViewControllerKey);
                var container = transitionContext.ContainerView;

                toController.View.Frame = transitionContext.GetFinalFrameForViewController(toController);
                container.InsertSubviewBelow(toController.View, fromController.View);
                fromController.View.Alpha = 1;

                UIView.Animate(
                    TransitionDuration(transitionContext),
                    delegate
                {
                    fromController.View.Alpha = 0;
                },
                delegate
                {
                    if (!transitionContext.TransitionWasCancelled)
                    {
                        fromController.View.RemoveFromSuperview();
                    }
                    transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled);
                }
                );
            }

            public override double TransitionDuration(IUIViewControllerContextTransitioning transitionContext)
            {
                return 0.4f;
            }
        }

        #endregion
    }
}
