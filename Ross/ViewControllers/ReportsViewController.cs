using System;
using System.Collections.Generic;
using CoreGraphics;
using Toggl.Phoebe.Reports;
using Toggl.Phoebe.Analytics;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;
using XPlatUtils;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;

namespace Toggl.Ross.ViewControllers
{
    public class ReportsViewController : UIViewController, InfiniteScrollView<ReportView>.IInfiniteScrollViewSource
    {
        private ZoomLevel _zoomLevel;
        public ZoomLevel ZoomLevel
        {
            get
            {
                return _zoomLevel;
            }
            set
            {
                if (_zoomLevel == value) return;

                _zoomLevel = value;
                scrollView.RefreshVisibleView();
                SummaryReportView.SaveReportsState(ZoomLevel);
                TrackScreenView();
            }
        }

        private ReportsMenuController menuController;
        private DateSelectorView dateSelectorView;
        private TopBorder topBorder;
        private SummaryReportView dataSource;
        private InfiniteScrollView<ReportView> scrollView;
        private StatusView statusView;
        private List<ReportView> cachedReports;
        private nint _timeSpaceIndex;
        private bool showStatus;

        static readonly nfloat padding = 24;
        static readonly nfloat navBarHeight = 64;
        static readonly nfloat selectorHeight = 50;


        public ReportsViewController()
        {
            Title = "ReportsTitle".Tr();

            EdgesForExtendedLayout = UIRectEdge.None;
            menuController = new ReportsMenuController();
            dataSource = new SummaryReportView();
            cachedReports = new List<ReportView>();

            _zoomLevel = ZoomLevel.Week;
            _timeSpaceIndex = 0;
        }

        public bool IsLoggedIn
            => StoreManager.Singleton.AppState.User.Id != Guid.Empty;

        public override void LoadView()
        {
            View = IsLoggedIn ?
                    new UIView().Apply(Style.Screen) :
                    new NoUserEmptyView(NoUserEmptyView.Screen.Reports, GoToLogin);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (!IsLoggedIn) return;

            _zoomLevel = SummaryReportView.GetLastZoomViewed();
            View.BackgroundColor = UIColor.White;
            menuController.Attach(this);

            topBorder = new TopBorder();
            dateSelectorView = new DateSelectorView();
            dateSelectorView.LeftArrowPressed += (sender, e) => scrollView.SetPageIndex(-1, true);
            dateSelectorView.RightArrowPressed += (sender, e) =>
            {
                if (_timeSpaceIndex >= 1) { return; }
                scrollView.SetPageIndex(1, true);
            };

            scrollView = new InfiniteScrollView<ReportView>(this);
            scrollView.Delegate = new InfiniteScrollDelegate();
            scrollView.OnChangePage += (sender, e) => LoadReportData();

            statusView = new StatusView()
            {
                Retry = LoadReportData,
                Cancel = () => StatusBarShown = false,
                StatusFailText = "ReportsStatusFailText".Tr(),
                StatusSyncingText = "ReportsStatusSyncText".Tr()
            };

            Add(scrollView);
            Add(dateSelectorView);
            Add(topBorder);
            Add(statusView);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (!IsLoggedIn) return;

            topBorder.Frame = new CGRect(0.0f, 0.0f, View.Bounds.Width, 2.0f);
            dateSelectorView.Frame = new CGRect(0, View.Bounds.Height - selectorHeight, View.Bounds.Width, selectorHeight);
            scrollView.Frame = new CGRect(0.0f, 0.0f, View.Bounds.Width, View.Bounds.Height - selectorHeight);
            LayoutStatusBar();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (!IsLoggedIn) return;

            ((MainViewController)AppDelegate.TogglWindow.RootViewController).MenuEnabled = false;
            NavigationController.InteractivePopGestureRecognizer.Enabled = false;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (!IsLoggedIn) return;

            TrackScreenView();
        }

        public override void ViewWillDisappear(bool animated)
        {
            if (IsLoggedIn)
            {
                ((MainViewController)AppDelegate.TogglWindow.RootViewController).MenuEnabled = true;
                NavigationController.InteractivePopGestureRecognizer.Enabled = true;
            }

            base.ViewWillDisappear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            if (menuController != null)
            {
                menuController.Detach();
                menuController = null;
            }
        }

        private void GoToLogin()
            => NavigationController.PushViewController(new LoginViewController(), true);
       
        private void TrackScreenView()
        {
            var screen = "Reports";
            switch (ZoomLevel)
            {
                case ZoomLevel.Week:
                    screen = "Reports (Week)";
                    break;
                case ZoomLevel.Month:
                    screen = "Reports (Month)";
                    break;
                case ZoomLevel.Year:
                    screen = "Reports (Year)";
                    break;
            }

            ServiceContainer.Resolve<ITracker>().CurrentScreen = screen;
        }

        private void ChangeReportState()
        {
            dataSource.Period = _zoomLevel;
            dateSelectorView.DateContent = FormattedIntervalDate(_timeSpaceIndex);
        }

        private void LoadReportData()
        {
            _timeSpaceIndex = scrollView.PageIndex;
            var reportView = scrollView.CurrentPage;
            reportView.ZoomLevel = ZoomLevel;
            reportView.TimeSpaceIndex = (int)_timeSpaceIndex;
            StatusBarShown &= reportView.IsClean;
            reportView.LoadData();
            ChangeReportState();
        }

        private string FormattedIntervalDate(nint backDate)
        {
            string result = "";

            if (backDate == 0)
            {
                switch (ZoomLevel)
                {
                    case ZoomLevel.Week:
                        result = "ReportsThisWeekSelector".Tr();
                        break;
                    case ZoomLevel.Month:
                        result = "ReportsThisMonthSelector".Tr();
                        break;
                    case ZoomLevel.Year:
                        result = "ReportsThisYearSelector".Tr();
                        break;
                }
            }
            else if (backDate == -1)
            {
                switch (ZoomLevel)
                {
                    case ZoomLevel.Week:
                        result = "ReportsLastWeekSelector".Tr();
                        break;
                    case ZoomLevel.Month:
                        result = "ReportsLastMonthSelector".Tr();
                        break;
                    case ZoomLevel.Year:
                        result = "ReportsLastYearSelector".Tr();
                        break;
                }
            }
            else
            {
                var startDate = dataSource.ResolveStartDate((int)_timeSpaceIndex);
                var endDate = dataSource.ResolveEndDate(startDate);

                switch (ZoomLevel)
                {
                    case ZoomLevel.Week:
                        if (startDate.Month == endDate.Month)
                        {
                            result = startDate.ToString("ReportsStartWeekInterval".Tr()) + " - " + endDate.ToString("ReportsEndWeekInterval".Tr());
                        }
                        else
                        {
                            result = startDate.Day + "th " + startDate.ToString("MMM") + " - " + endDate.Day + "th " + startDate.ToString("MMM");
                        }
                        break;
                    case ZoomLevel.Month:
                        result = startDate.ToString("ReportsMonthInterval".Tr());
                        break;
                    case ZoomLevel.Year:
                        result = startDate.ToString("ReportsYearInterval".Tr());
                        break;
                }
            }
            return result;
        }

        #region StatusBar

        private void LayoutStatusBar()
        {
            var size = View.Frame.Size;
            var statusY = showStatus ? size.Height - selectorHeight : size.Height + 2f;
            statusView.Frame = new CGRect(0, statusY, size.Width, selectorHeight);
        }

        private bool StatusBarShown
        {
            get { return showStatus; }
            set
            {
                if (showStatus == value)
                {
                    return;
                }
                showStatus = value;
                UIView.Animate(0.5f, LayoutStatusBar);
            }
        }

        #endregion

        #region IInfiniteScrollViewSource implementation

        public ReportView CreateView()
        {
            ReportView view;
            if (cachedReports.Count == 0)
            {
                view = new ReportView();
            }
            else
            {
                view = cachedReports[0];
                cachedReports.RemoveAt(0);
            }
            if (scrollView.Pages.Count > 0)
            {
                view.Position = scrollView.CurrentPage.Position;
            }
            view.LoadStart += ReportLoadStart;
            view.LoadFinished += ReportLoadFinished;

            return view;
        }

        public void Dispose(ReportView view)
        {
            var reportView = view;
            if (reportView.IsClean)
            {
                reportView.StopReloadData();
            }
            view.LoadStart -= ReportLoadStart;
            view.LoadFinished -= ReportLoadFinished;
        }

        public bool ShouldStartScroll()
        {
            var currentReport = scrollView.CurrentPage;

            if (!currentReport.Dragging)
            {
                currentReport.ScrollEnabled = false;
                foreach (var item in scrollView.Pages)
                {
                    var report = item;
                    report.Position = currentReport.Position;
                }
            }
            return !currentReport.Dragging;
        }

        #endregion

        private void ReportLoadStart(object sender, EventArgs args)
        {
            statusView.IsSyncing |= StatusBarShown;
        }

        private void ReportLoadFinished(object sender, EventArgs args)
        {
            var report = (ReportView)sender;
            if (report.IsError)
            {
                // Make sure that error is shown
                statusView.IsSyncing = false;
                StatusBarShown = true;
            }
            else
            {
                // Successful sync, clear ignoring flag
                StatusBarShown = false;
            }
        }

        internal class InfiniteScrollDelegate : UIScrollViewDelegate
        {
            public override void DecelerationEnded(UIScrollView scrollView)
            {
                var infiniteScroll = (InfiniteScrollView<ReportView>)scrollView;
                infiniteScroll.CurrentPage.ScrollEnabled = true;
            }
        }

        internal class TopBorder : UIView
        {
            public TopBorder()
            {
                BackgroundColor = UIColor.Clear;
            }

            public override void Draw(CGRect rect)
            {
                using (CGContext g = UIGraphics.GetCurrentContext())
                {
                    Color.TimeBarBoderColor.SetColor();
                    g.FillRect(new CGRect(0.0f, 0.0f, rect.Width, 1.0f / ContentScaleFactor));
                }
            }
        }
    }
}