using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;

namespace Toggl.Ross.Views
{
    public class InfiniteScrollView<TView> : UIScrollView where TView : UIView
    {
        public event EventHandler OnChangePage;

        private nint _pageIndex;

        public nint PageIndex
        {
            get
            {
                return _pageIndex + (Convert.ToInt32(ContentOffset.X / pageWidth) - tmpOffset);
            }
        }

        private TView currentPage;

        public TView CurrentPage
        {
            get
            {
                var pos = ConvertPointToView(ContentOffset, _containerView).X;
                foreach (var view in pages)
                    if (Math.Abs(pos - view.Frame.X) <= pageWidth / 2)
                    {
                        currentPage = view;
                    }

                return currentPage;
            }
        }

        public List<TView> Pages
        {
            get
            {
                return pages;
            }
        }

        private nint rightIndexLimit;

        public nint RightIndexLimit
        {
            get
            {
                return rightIndexLimit;
            }
            set
            {
                rightIndexLimit = value;
            }
        }

        public InfiniteScrollView(IInfiniteScrollViewSource viewSource)
        {
            this.viewSource = viewSource;
            pages = new List<TView> ();
            _containerView = new UIView();
            Add(_containerView);

            ShowsHorizontalScrollIndicator = false;
            PagingEnabled = true;
            rightIndexLimit = 1;
        }

        List<TView> pages;
        UIView _containerView;
        IInfiniteScrollViewSource viewSource;

        private nfloat pageWidth = -1;
        private nint tmpOffset;
        private nint prevPageIndex = nint.MinValue;

        private void RecenterIfNeeded()
        {
            CGPoint currentOffset = ContentOffset;
            nfloat contentWidth = ContentSize.Width;
            nfloat centerOffsetX = (contentWidth - Bounds.Width) / 2.0f;
            nfloat distanceFromCenter = (nfloat)Math.Abs(currentOffset.X - centerOffsetX);

            if (distanceFromCenter > contentWidth / 4.0f && (distanceFromCenter - pageWidth / 2) % pageWidth == 0)
            {
                _pageIndex += Convert.ToInt32(ContentOffset.X / pageWidth - tmpOffset);
                ContentOffset = new CGPoint(centerOffsetX - pageWidth / 2, currentOffset.Y);
                foreach (var item in pages)
                {
                    CGPoint center = _containerView.ConvertPointToView(item.Center, this);
                    center.X += centerOffsetX - currentOffset.X - pageWidth / 2;
                    item.Center = ConvertPointToView(center, _containerView);
                }
            }
        }

        public async override void LayoutSubviews()
        {
            base.LayoutSubviews();

            // set correct page width
            if (pageWidth == -1)
            {
                pageWidth = Bounds.Width;
            }

            // simulate end of scrollView at right
            // condition to avoid new creations of views
            if (prevPageIndex >= rightIndexLimit)
            {
                var shouldTriggerUpdate = PageIndex < rightIndexLimit && prevPageIndex != PageIndex;

                prevPageIndex = PageIndex;
                if (PageIndex > rightIndexLimit)
                {

                    // disable user interaction
                    UserInteractionEnabled = false;

                    // animate scrollview to correct position
                    var offset = ContentOffset.X % pageWidth;
                    SetContentOffset(new CGPoint(ContentOffset.X - offset, ContentOffset.Y), true);

                    // wait for movement and enable user interaction
                    await Task.Delay(350);
                    UserInteractionEnabled = true;
                }

                if (shouldTriggerUpdate)
                {
                    OnChangePage?.Invoke(this, EventArgs.Empty);
                }

                return;
            }

            ContentSize = new CGSize(pageWidth * 20, Bounds.Height);
            _containerView.Frame = new CGRect(0, 0, ContentSize.Width, ContentSize.Height);
            RecenterIfNeeded();

            // tile content in visible bounds
            CGRect visibleBounds = ConvertRectToView(Bounds, _containerView);
            nfloat minimumVisibleX = CGRectGetMinX(visibleBounds);
            nfloat maximumVisibleX = CGRectGetMaxX(visibleBounds);
            TileViews(minimumVisibleX, maximumVisibleX);

            if (prevPageIndex != PageIndex)
            {
                prevPageIndex = PageIndex;
                OnChangePage?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetPageIndex(int offSet, bool animated)
        {
            var currentCOffset = ContentOffset;
            if (currentCOffset.X % pageWidth == 0)
            {
                currentCOffset.X += pageWidth * offSet;
                SetContentOffset(currentCOffset, animated);
            }
        }

        public void RefreshVisibleView()
        {
            if (Dragging)
            {
                return;
            }

            var currentView = pages.Find(v => v.Frame.X.CompareTo(ContentOffset.X) == 0);
            var center = currentView.Center;
            TView newView = InsertView();
            var offSetY = ContentSize.Height;
            var frame = currentView.Frame;

            frame.Y += offSetY;
            newView.Frame = frame;

            UIView.Animate(0.6, 0.4, UIViewAnimationOptions.CurveEaseIn, () => { currentView.Alpha = 0.25f; }, null);

            UIView.Animate(0.7, 0.5, UIViewAnimationOptions.CurveEaseIn,
                           () =>
            {
                currentView.Transform = CGAffineTransform.MakeScale(0.75f, 0.75f);
                currentView.Center = new CGPoint(center.X, center.Y + 105);
            }, null);

            UIView.Animate(0.7, 0.6, UIViewAnimationOptions.CurveEaseInOut,
                           () =>
            {
                newView.Center = center;
            }, () =>
            {
                foreach (var item in pages)
                {
                    viewSource.Dispose(item);
                    item.RemoveFromSuperview();
                }
                pages.Clear();
                pages.Add(newView);
                if (OnChangePage != null)
                {
                    OnChangePage.Invoke(this, new EventArgs());
                }
            });
        }

        public override bool GestureRecognizerShouldBegin(UIGestureRecognizer gestureRecognizer)
        {
            return viewSource.ShouldStartScroll();
        }

        private TView InsertView()
        {
            TView view = viewSource.CreateView();
            view.Frame = new CGRect(0, 0, pageWidth, Bounds.Height);
            _containerView.Add(view);
            return view;
        }

        private nfloat PlaceNewViewOnRight(nfloat rightEdge)
        {
            TView view = InsertView();
            pages.Add(view);  // add rightmost label at the end of the array

            CGRect viewFrame = view.Frame;
            viewFrame.X = rightEdge;
            view.Frame = viewFrame;
            return CGRectGetMaxX(viewFrame);
        }

        private nfloat PlaceNewViewOnLeft(nfloat leftEdge)
        {
            TView view = InsertView();
            pages.Insert(0, view);   // add leftmost label at the beginning of the array

            CGRect viewFrame = view.Frame;
            viewFrame.X = leftEdge - viewFrame.Width;
            view.Frame = viewFrame;
            return CGRectGetMinX(viewFrame);
        }

        private void TileViews(nfloat minX, nfloat maxX)
        {
            // the upcoming tiling logic depends on there already being at least one label in the visibleLabels array, so
            // to kick off the tiling we need to make sure there's at least one label
            if (pages.Count == 0)
            {
                tmpOffset = (nint)Convert.ToInt32(ContentOffset.X / pageWidth);
                PlaceNewViewOnRight(minX);
                currentPage = pages [0];
            }

            // add views that are missing on right side
            TView lastView = pages [pages.Count - 1];
            nfloat rightEdge = CGRectGetMaxX(lastView.Frame);
            while (rightEdge < maxX)
            {
                rightEdge = PlaceNewViewOnRight(rightEdge);
            }

            // add views that are missing on left side
            TView firstView = pages [0];
            nfloat leftEdge = CGRectGetMinX(firstView.Frame);
            while (leftEdge > minX)
            {
                leftEdge = PlaceNewViewOnLeft(leftEdge);
            }

            // remove views that have fallen off right edge
            lastView = pages.Last();
            while (lastView.Frame.X > maxX)
            {
                lastView.RemoveFromSuperview();
                viewSource.Dispose(lastView);
                pages.Remove(lastView);
                lastView = pages.Last();
            }

            // remove views that have fallen off left edge
            firstView = pages.First();
            while (CGRectGetMaxX(firstView.Frame) < minX)
            {
                firstView.RemoveFromSuperview();
                viewSource.Dispose(firstView);
                pages.Remove(firstView);
                firstView = pages.First();
            }
        }

        private nfloat CGRectGetMinX(CGRect rect)
        {
            return rect.X;
        }

        private nfloat CGRectGetMaxX(CGRect rect)
        {
            return rect.X + rect.Width;
        }

        public interface IInfiniteScrollViewSource
        {
            TView CreateView();

            void Dispose(TView view);

            bool ShouldStartScroll();
        }
    }
}