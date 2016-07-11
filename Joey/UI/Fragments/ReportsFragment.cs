using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Reports;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsFragment : Fragment
    {
        private static readonly string ReportPeriodArgument = "com.toggl.timer.report_period";
        private static readonly string ReportZoomArgument = "com.toggl.timer.report_zoom";

        private bool isLoading;
        private Controller controller;
        private Pool<Controller> controllerPool;
        private int position;

        public int Period
        {
            get
            {
                int period = 0;
                if (Arguments != null)
                {
                    period = Arguments.GetInt(ReportPeriodArgument);
                }
                return period;
            }
        }

        public ZoomLevel ZoomLevel
        {
            get
            {
                var zoomValue = ZoomLevel.Week;
                if (Arguments != null)
                {
                    zoomValue = (ZoomLevel)Arguments.GetInt(ReportZoomArgument, (int)zoomValue);
                }
                return zoomValue;
            }
        }

        public bool IsError
        {
            get;
            set;
        }

        public ReportsFragment(IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base(jref, xfer)
        {
        }

        public ReportsFragment()
        {
        }

        public static ReportsFragment NewInstace(int period, ZoomLevel zoom)
        {
            var fragment = new ReportsFragment();

            var args = new Bundle();
            args.PutInt(ReportPeriodArgument, period);
            args.PutInt(ReportZoomArgument, (int)zoom);
            fragment.Arguments = args;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var pagerFragment = ParentFragment as ReportsPagerFragment;
            if (pagerFragment == null)
            {
                return new View(Activity);
            }

            controllerPool = pagerFragment.ReportsControllers;

            controller = controllerPool.Obtain();
            controller.SnapPosition = position;
            controller.SnapPositionChanged += OnControllerSnapPositionChanged;

            return controller.View;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            if (controller != null)
            {
                controller.SnapPositionChanged -= OnControllerSnapPositionChanged;
                controllerPool.Release(controller);
                controller = null;
            }

            controllerPool = null;
        }

        public async override void OnResume()
        {
            base.OnResume();
            await EnsureLoaded();
        }

        private void OnControllerSnapPositionChanged(object sender, EventArgs e)
        {
            // Cascade event to our listeners
            if (PositionChanged != null)
            {
                PositionChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler PositionChanged;

        public event EventHandler<LoadReadyEventArgs> LoadReady;

        public int Position
        {
            get
            {
                if (controller == null)
                {
                    return position;
                }
                return controller.SnapPosition;
            }
            set
            {
                position = value;
                if (controller != null)
                {
                    controller.SnapPosition = position;
                }
            }
        }

        public async Task ReloadData()
        {
            if (isLoading || controller == null)
            {
                return;
            }
            controller.Data = null;
            await EnsureLoaded();
        }

        private async Task EnsureLoaded()
        {
            if (isLoading || controller == null || controller.Data != null)
            {
                return;
            }

            isLoading = true;
            try
            {
                var data = new SummaryReportView
                {
                    Period = ZoomLevel,
                };
                await data.Load(StoreManager.Singleton.AppState.User, Period);
                IsError = data.IsError;
                if (controller != null)
                {
                    controller.Data = data;
                }
            }
            finally
            {
                isLoading = false;
                if (LoadReady != null)
                {
                    LoadReady(this, new LoadReadyEventArgs(Period, IsError));
                }
            }
        }

        public sealed class Controller : Java.Lang.Object, ViewGroup.IOnHierarchyChangeListener
        {
            private readonly List<View> trackedProjectListItems = new List<View>();
            private Context ctx;
            private Pool<View> projectListItemPool;
            private View rootView;
            private SnappyLayout snappyLayout;
            private BarChart barChart;
            private PieChart pieChart;
            private TextView totalValue;
            private ListView listView;
            private TextView billableValue;
            private int focusedPosition = -1;
            private SummaryReportView data;

            public Controller(IntPtr a, Android.Runtime.JniHandleOwnership b) : base(a, b)
            {
            }

            public Controller(Context ctx, Pool<View> projectListItemPool)
            {
                this.ctx = ctx;
                this.projectListItemPool = projectListItemPool;
                var inflater = LayoutInflater.From(ctx);

                var view = rootView = inflater.Inflate(Resource.Layout.ReportsFragment, null, false);
                snappyLayout = view.FindViewById<SnappyLayout> (Resource.Id.SnappyLayout);
                barChart = view.FindViewById<BarChart> (Resource.Id.BarChart);
                pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);
                listView = view.FindViewById<ListView> (Resource.Id.ReportList);
                totalValue = view.FindViewById<TextView> (Resource.Id.TotalValue);
                billableValue = view.FindViewById<TextView> (Resource.Id.BillableValue);

                snappyLayout.ActiveChildChanged += OnSnappyActiveChildChanged;

                pieChart.ActiveSliceChanged += OnPieActiveSliceChanged;

                listView.SetClipToPadding(false);
                listView.ItemClick += OnListItemClick;
                listView.Touch += OnListTouch;
                listView.SetOnHierarchyChangeListener(this);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    snappyLayout.ActiveChildChanged -= OnSnappyActiveChildChanged;
                    pieChart.ActiveSliceChanged -= OnPieActiveSliceChanged;
                    listView.ItemClick -= OnListItemClick;
                    listView.SetOnHierarchyChangeListener(null);

                    DisposeAndNull(ref totalValue);
                    DisposeAndNull(ref billableValue);
                    DisposeAndNull(ref listView);
                    DisposeAndNull(ref barChart);
                    DisposeAndNull(ref pieChart);
                    DisposeAndNull(ref snappyLayout);
                    DisposeAndNull(ref rootView);
                }
            }

            private static void DisposeAndNull<T> (ref T disposable)
            where T : class, IDisposable
            {
                if (disposable != null)
                {
                    disposable.Dispose();
                    disposable = null;
                }
            }

            private void OnListTouch(object sender, View.TouchEventArgs e)
            {
                switch (e.Event.Action)
                {
                    case MotionEventActions.Down:
                        // Disable SnappyList intercepting list view scroll events
                        listView.Parent.RequestDisallowInterceptTouchEvent(true);
                        // Enable view pager to intercept swiping gesture
                        snappyLayout.Parent.RequestDisallowInterceptTouchEvent(false);
                        break;

                    case MotionEventActions.Up:
                    case MotionEventActions.Cancel:
                        listView.Parent.RequestDisallowInterceptTouchEvent(false);
                        break;
                }

                // Run the usual touch logic for ListView
                e.Handled = listView.OnTouchEvent(e.Event);
            }

            private void SetFocusedPosition(int value, bool scrollToPosition = false)
            {
                if (value == focusedPosition)
                {
                    return;
                }

                focusedPosition = value;

                var adapter = (ReportProjectAdapter)listView.Adapter;
                adapter.SetFocus(focusedPosition);
                pieChart.ActiveSlice = focusedPosition;

                if (scrollToPosition && focusedPosition >= 0)
                {
                    listView.SmoothScrollToPositionFromTop(focusedPosition, 0);
                }
            }

            private void OnListItemClick(object sender, AdapterView.ItemClickEventArgs args)
            {
                SetFocusedPosition(focusedPosition != args.Position ? args.Position : -1);
            }

            private void OnPieActiveSliceChanged(object sender, EventArgs args)
            {
                SetFocusedPosition(pieChart.ActiveSlice, scrollToPosition: true);
            }

            private void OnSnappyActiveChildChanged(object sender, EventArgs e)
            {
                // Cascade the event down to our listeners
                if (SnapPositionChanged != null)
                {
                    SnapPositionChanged(this, EventArgs.Empty);
                }
            }

            public View ObtainProjectListItem()
            {
                var v = projectListItemPool.Obtain();
                trackedProjectListItems.Add(v);
                return v;
            }

            void ViewGroup.IOnHierarchyChangeListener.OnChildViewAdded(View parent, View child)
            {
            }

            void ViewGroup.IOnHierarchyChangeListener.OnChildViewRemoved(View parent, View child)
            {
                // Monitor when the ListView is done with the child and release it back to the pool
                if (trackedProjectListItems.Remove(child))
                {
                    projectListItemPool.Release(child);
                }
            }

            public View View
            {
                get { return rootView; }
            }

            public Context Context
            {
                get { return ctx; }
            }

            public int SnapPosition
            {
                get { return snappyLayout.ActiveChild; }
                set { snappyLayout.ActiveChild = value; }
            }

            public event EventHandler SnapPositionChanged;

            public SummaryReportView Data
            {
                get { return data; }
                set
                {
                    if (value == data)
                    {
                        return;
                    }

                    data = value;

                    if (data == null)
                    {
                        // Reset everything to blank
                        totalValue.Text = string.Empty;
                        billableValue.Text = string.Empty;
                        barChart.Reset(null);
                        pieChart.Reset(null);
                        listView.Adapter = null;
                    }
                    else
                    {
                        // Bind the data to the view
                        totalValue.Text = data.TotalGrand;
                        billableValue.Text = data.TotalBillale;
                        barChart.Reset(data);
                        pieChart.Reset(data);
                        listView.Adapter = new ReportProjectAdapter(this, data.Projects);
                    }
                }
            }
        }

        private class ReportProjectAdapter : BaseAdapter<ReportProject>
        {
            private List<ReportProject> dataView;
            private int focus = -1;
            private Controller controller;

            public ReportProjectAdapter(IntPtr a, Android.Runtime.JniHandleOwnership b) : base(a, b)
            {
            }

            public ReportProjectAdapter(Controller controller, List<ReportProject> dataView)
            {
                this.controller = controller;
                this.dataView = dataView;
            }

            public override Java.Lang.Object GetItem(int position)
            {
                return null;
            }

            public override long GetItemId(int position)
            {
                return position;
            }

            public override ReportProject this [int position]
            {
                get { return dataView[position]; }
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                View view = convertView;

                if (convertView == null)
                {
                    view = controller.ObtainProjectListItem();
                }
                var holder = (ProjectListItemHolder)view.Tag;
                holder.Bind(dataView [position]);
                holder.SetFocus(focus, position);  // mmm...

                return view;
            }

            public void SetFocus(int selected)
            {
                focus = selected;
                NotifyDataSetChanged();
            }

            public override int Count
            {
                get
                {
                    return dataView.Count;
                }
            }
        }

        public class ProjectListItemHolder : BindableViewHolder<ReportProject>
        {
            private View _root;

            public View ColorSquareView  { get; private set; }

            public TextView NameTextView  { get; private set; }

            public TextView DurationTextView  { get; private set; }

            public ProjectListItemHolder(View root)  : base(root)
            {
                NameTextView = root.FindViewById<TextView> (Resource.Id.ProjectName).SetFont(Font.Roboto);

                ColorSquareView = root.FindViewById<View> (Resource.Id.ColorSquare);

                DurationTextView = root.FindViewById<TextView> (Resource.Id.ProjectDuration).SetFont(Font.Roboto);

                _root = root;
            }

            protected override void Rebind()
            {
                if (String.IsNullOrEmpty(DataSource.Project))
                {
                    NameTextView.SetText(Resource.String.ReportsListViewNoProject);
                }
                else if (DataSource.Color == ProjectData.GroupedProjectColorIndex)
                {
                    NameTextView.Text = _root.Context.Resources.GetQuantityString(
                                            Resource.Plurals.GroupedReportProjectCell,
                                            int.Parse(DataSource.Project),
                                            int.Parse(DataSource.Project)
                                        );
                }
                else
                {
                    NameTextView.Text = DataSource.Project;
                }

                DurationTextView.Text = DataSource.FormattedTotalTime;
                var squareDrawable = new GradientDrawable();
                squareDrawable.SetCornerRadius(5);
                var color = (DataSource.Color == ProjectData.GroupedProjectColorIndex) ? ProjectData.GroupedProjectColor : ProjectData.HexColors [ DataSource.Color % ProjectData.HexColors.Length];
                squareDrawable.SetColor(Color.ParseColor(color));
                ColorSquareView.SetBackgroundDrawable(squareDrawable);
            }

            public void SetFocus(int focus, int position)
            {
                var squareDrawable = (GradientDrawable)ColorSquareView.Background;
                if (focus != -1)
                {
                    _root.Alpha = (focus == position) ? 1 : 0.5f;
                    var radius = (focus == position) ? Convert.ToSingle(ColorSquareView.Height / 2) : 5.0f;
                    squareDrawable.SetCornerRadius(radius);
                }
                else
                {
                    _root.Alpha = 1;
                    squareDrawable.SetCornerRadius(5);
                }
            }
        }

        public class LoadReadyEventArgs : EventArgs
        {
            private int fragmentPeriod;
            private bool isError;

            public LoadReadyEventArgs(int period, bool error)
            {
                fragmentPeriod = period;
                isError = error;
            }

            public int Period
            {
                get
                {
                    return fragmentPeriod;
                }
            }

            public bool IsError
            {
                get
                {
                    return isError;
                }
            }
        }
    }
}
