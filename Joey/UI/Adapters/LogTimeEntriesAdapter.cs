using System;
using System.Timers;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public class LogTimeEntriesAdapter : RecyclerCollectionDataAdapter<IHolder>, IUndoAdapter
    {
        public const int ViewTypeDateHeader = ViewTypeContent + 1;
        private static readonly int ContinueThreshold = 1;
        private DateTime lastTimeEntryContinuedTime;
        private int lastUndoIndex = -1;
        private LogTimeEntriesVM viewModel;
        private RecyclerLoadState footerState = RecyclerLoadState.Loading;

        public LogTimeEntriesAdapter(IntPtr a, Android.Runtime.JniHandleOwnership b) : base(a, b)
        {
        }

        public LogTimeEntriesAdapter(RecyclerView owner, LogTimeEntriesVM viewModel)
        : base(owner, viewModel.Collection)
        {
            this.viewModel = viewModel;
            lastTimeEntryContinuedTime = Time.UtcNow;
        }

        private void OnContinueTimeEntry(RecyclerView.ViewHolder viewHolder)
        {
            DeleteSelectedItem();

            // Don't continue a new TimeEntry before
            // x seconds has passed.
            if (Time.UtcNow < lastTimeEntryContinuedTime + TimeSpan.FromSeconds(ContinueThreshold))
            {
                return;
            }
            lastTimeEntryContinuedTime = Time.UtcNow;

            viewModel.ContinueTimeEntry(viewHolder.AdapterPosition);
        }

        public bool IsNoUserMode
        {
            get
            {
                return viewModel.IsNoUserMode;
            }
        }
        protected override RecyclerView.ViewHolder GetViewHolder(ViewGroup parent, int viewType)
        {
            View view;
            RecyclerView.ViewHolder holder;

            if (viewType == ViewTypeDateHeader)
            {
                view = LayoutInflater.FromContext(ServiceContainer.Resolve<Context> ()).Inflate(Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                holder = new HeaderListItemHolder(view);
            }
            else
            {
                view = LayoutInflater.FromContext(ServiceContainer.Resolve<Context> ()).Inflate(Resource.Layout.LogTimeEntryListItem, parent, false);
                holder = new TimeEntryListItemHolder(this, view);
            }

            return holder;
        }

        protected override void BindHolder(RecyclerView.ViewHolder holder, int position)
        {
            var headerListItemHolder = holder as HeaderListItemHolder;
            if (headerListItemHolder != null)
            {
                headerListItemHolder.Bind((DateHolder) GetItem(position));
                return;
            }

            var timeEntryListItemHolder = holder as TimeEntryListItemHolder;
            if (timeEntryListItemHolder != null)
            {
                timeEntryListItemHolder.Bind((ITimeEntryHolder) GetItem(position));
                // Set correct Undo state.
                if (position == lastUndoIndex)
                {
                    timeEntryListItemHolder.SetUndoState();
                }
                else
                {
                    timeEntryListItemHolder.SetNormalState();
                }
                return;
            }

            var footerHolder = holder as FooterHolder;
            if (footerHolder != null)
            {
                footerHolder.Bind(footerState);
            }
        }

        public override int GetItemViewType(int position)
        {
            var type = base.GetItemViewType(position);
            if (type != ViewTypeLoaderPlaceholder)
            {
                type = GetItem(position) is DateHolder ? ViewTypeDateHeader : ViewTypeContent;
            }
            return type;
        }

        public void SetFooterState(RecyclerLoadState state)
        {
            // TODO: Once the footer is in the "finished" state.
            // all remaining calls are rejected. Why? In a very special situations, scroll
            // loadMore call and initial LoadMore aren't called in the correct order.
            if (footerState == RecyclerLoadState.Finished)
            {
                return;
            }
            footerState = state;
            NotifyItemChanged(ItemCount - 1);
        }

        public static bool isNoUserMode
        {
            get
            {
                return String.IsNullOrEmpty(StoreManager.Singleton.AppState.User.ApiToken);
            }
        }

        #region IUndo interface implementation
        private void SetItemsToNormalPosition()
        {
            var linearLayout = (LinearLayoutManager)Owner.GetLayoutManager();
            var firstVisible = linearLayout.FindFirstVisibleItemPosition();
            var lastVisible = linearLayout.FindLastVisibleItemPosition();

            for (int i = 0; i < linearLayout.ItemCount; i++)
            {
                var holder = Owner.FindViewHolderForLayoutPosition(i);
                if (holder is TimeEntryListItemHolder)
                {
                    var tHolder = (TimeEntryListItemHolder)holder;
                    if (!tHolder.IsNormalState)
                    {
                        var withAnim = (firstVisible < i) && (lastVisible > i);
                        tHolder.SetNormalState(withAnim);
                    }
                }
            }
            lastUndoIndex = -1;
        }

        public void DeleteSelectedItem()
        {
            var linearLayout = (LinearLayoutManager)Owner.GetLayoutManager();
            var firstVisible = linearLayout.FindFirstVisibleItemPosition();
            var lastVisible = linearLayout.FindLastVisibleItemPosition();

            for (int i = firstVisible; i < lastVisible + 1; i++)
            {
                var holder = Owner.FindViewHolderForLayoutPosition(i);
                if (holder is TimeEntryListItemHolder)
                {
                    var tHolder = (TimeEntryListItemHolder)holder;
                    if (!tHolder.IsNormalState)
                    {
                        viewModel.RemoveTimeEntry(i);
                    }
                }
            }
            lastUndoIndex = -1;
        }

        public void SetItemToUndoPosition(RecyclerView.ViewHolder viewHolder)
        {
            // If another ViewHolder is visible and ready to Remove,
            // just Remove it.
            if (lastUndoIndex > -1)
            {
                viewModel.RemoveTimeEntry(lastUndoIndex);
            }

            // Save last selected ViewHolder index.
            lastUndoIndex = viewHolder.LayoutPosition;

            // Important!
            // Refresh holder (and tell to ItemTouchHelper
            // that actions ended over it.
            NotifyItemChanged(viewHolder.LayoutPosition);
        }

        public bool IsUndo(int index)
        {
            // Ask to the holder about if it is Undo or not.
            var holder =  Owner.FindViewHolderForLayoutPosition(index);
            if (holder is TimeEntryListItemHolder)
            {
                return !((TimeEntryListItemHolder)holder).IsNormalState;
            }
            return false;
        }
        #endregion

        protected override RecyclerView.ViewHolder GetFooterHolder(ViewGroup parent)
        {
            var view = LayoutInflater.FromContext(parent.Context).Inflate(
                           Resource.Layout.TimeEntryListFooter, parent, false);
            return new FooterHolder(view, viewModel);
        }

        [Shadow(ShadowAttribute.Mode.Top | ShadowAttribute.Mode.Bottom)]
        public class HeaderListItemHolder : RecyclerView.ViewHolder
        {
            public TextView DateGroupTitleTextView { get; private set; }
            public TextView DateGroupDurationTextView { get; private set; }

            private Timer timer;
            private bool isRunning;
            private TimeSpan duration;
            private DateTime date;

            public HeaderListItemHolder(IntPtr a, Android.Runtime.JniHandleOwnership b) : base(a, b)
            {
            }

            public HeaderListItemHolder(View root) : base(root)
            {
                DateGroupTitleTextView = root.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).SetFont(Font.RobotoMedium);
                DateGroupDurationTextView = root.FindViewById<TextView> (Resource.Id.DateGroupDurationTextView).SetFont(Font.Roboto);
            }

            public void Bind(DateHolder data)
            {
                date = data.Date;
                duration = data.TotalDuration;
                isRunning = data.IsRunning;
                SetContentData();
            }

            private void SetContentData()
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Elapsed -= OnDurationElapsed;
                    timer = null;
                }

                if (isRunning)
                {
                    timer = new Timer(60000 - duration.Seconds * 1000 - duration.Milliseconds);
                    timer.Elapsed += OnDurationElapsed;
                    timer.Start();
                }

                DateGroupTitleTextView.Text = GetRelativeDateString(date);
                DateGroupDurationTextView.Text = FormatDuration(duration);
            }

            private void OnDurationElapsed(object sender, ElapsedEventArgs e)
            {
                // Update duration with new time.
                duration = duration.Add(TimeSpan.FromMilliseconds(timer.Interval));
                BaseActivity.CurrentActivity.RunOnUiThread(() =>
                {
                    SetContentData();
                });
            }

            private static string GetRelativeDateString(DateTime dateTime)
            {
                var ctx = ServiceContainer.Resolve<Context> ();
                var ts = Time.Now.Date - dateTime.Date;
                switch (ts.Days)
                {
                    case 0:
                        return ctx.Resources.GetString(Resource.String.Today);
                    case 1:
                        return ctx.Resources.GetString(Resource.String.Yesterday);
                    case -1:
                        return ctx.Resources.GetString(Resource.String.Tomorrow);
                    default:
                        return dateTime.ToDeviceDateString();
                }
            }

            private string FormatDuration(TimeSpan dr)
            {
                var ctx = ServiceContainer.Resolve<Context> ();
                if (dr.TotalHours >= 1f)
                {
                    return string.Format(
                               ctx.Resources.GetString(Resource.String.LogHeaderDurationHoursMinutes),
                               (int)dr.TotalHours,
                               dr.Minutes
                           );
                }
                if (dr.Minutes > 0)
                {
                    return string.Format(
                               ctx.Resources.GetString(Resource.String.LogHeaderDurationMinutes),
                               dr.Minutes
                           );
                }
                return string.Empty;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Elapsed -= OnDurationElapsed;
                        timer = null;
                    }
                }
                base.Dispose(disposing);
            }
        }

        private class TimeEntryListItemHolder : RecyclerView.ViewHolder, View.IOnTouchListener
        {
            private readonly LogTimeEntriesAdapter owner;
            private Timer timer;
            private bool isRunning;
            private TimeSpan duration;

            public View ColorView { get; private set; }
            public TextView ProjectTextView { get; private set; }
            public TextView ClientTextView { get; private set; }
            public TextView TaskTextView { get; private set; }
            public TextView DescriptionTextView { get; private set; }
            public NotificationImageView TagsView { get; private set; }
            public View BillableView { get; private set; }
            public View NotSyncedView { get; private set; }
            public TextView DurationTextView { get; private set; }
            public ImageButton ContinueImageButton { get; private set; }

            public View SwipeLayout { get; private set; }
            public View PreUndoLayout { get; private set; }
            public View UndoLayout { get; private set; }
            public View UndoButton { get; private set; }

            public TimeEntryListItemHolder(IntPtr a, Android.Runtime.JniHandleOwnership b) : base(a, b)
            {
            }

            public TimeEntryListItemHolder(LogTimeEntriesAdapter owner, View root) : base(root)
            {
                this.owner = owner;

                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont(Font.RobotoMedium);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont(Font.RobotoMedium);
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView).SetFont(Font.RobotoMedium);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont(Font.Roboto);
                TagsView = root.FindViewById<NotificationImageView> (Resource.Id.TagsIcon);
                NotSyncedView = root.FindViewById<View> (Resource.Id.NotSyncedIcon);
                BillableView = root.FindViewById<View> (Resource.Id.BillableIcon);
                DurationTextView = root.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont(Font.RobotoLight);
                ContinueImageButton = root.FindViewById<ImageButton> (Resource.Id.ContinueImageButton);
                SwipeLayout = root.FindViewById<RelativeLayout> (Resource.Id.swipe_layout);
                PreUndoLayout = root.FindViewById<FrameLayout> (Resource.Id.pre_undo_layout);
                UndoButton = root.FindViewById<LinearLayout> (Resource.Id.undo_layout);
                UndoButton = root.FindViewById(Resource.Id.undo_button);
                UndoLayout = root.FindViewById(Resource.Id.undo_layout);

                ContinueImageButton.SetOnTouchListener(this);
                UndoButton.SetOnTouchListener(this);
            }

            bool View.IOnTouchListener.OnTouch(View v, MotionEvent e)
            {
                bool returnValue = true;
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        returnValue = !(v == ContinueImageButton);
                        break;

                    case MotionEventActions.Up:
                        if (v == ContinueImageButton)
                        {
                            owner.OnContinueTimeEntry(this);
                            returnValue = false;
                        }
                        if (v == UndoButton)
                        {
                            owner.SetItemsToNormalPosition();
                            returnValue = true;
                        }
                        break;
                }
                return returnValue;
            }

            #region Undo cell methods.
            public bool IsNormalState
            {
                get
                {
                    return SwipeLayout.TranslationX < 5;
                }
            }

            public void SetNormalState(bool animated = false)
            {
                SwipeLayout.Visibility = ViewStates.Visible;
                if (animated)
                {
                    SwipeLayout.Animate().TranslationX(0).SetDuration(150);
                }
                else
                {
                    SwipeLayout.SetX(0);
                }
                PreUndoLayout.Visibility = ViewStates.Visible;
                UndoLayout.Visibility = ViewStates.Gone;
            }

            public void SetUndoState()
            {
                // Show Undo layout for selected ViewHolder
                UndoLayout.Visibility = ViewStates.Visible;
                PreUndoLayout.Visibility = ViewStates.Gone;
                SwipeLayout.Visibility = ViewStates.Gone;
                SwipeLayout.SetX(ItemView.Width);
            }
            #endregion

            public void Bind(ITimeEntryHolder datasource)
            {
                if (datasource == null || Handle == IntPtr.Zero)
                {
                    return;
                }

                var color = Color.Transparent;
                var entryData = datasource.Entry.Data;
                var ctx = ServiceContainer.Resolve<Context> ();

                if (((LogTimeEntriesAdapter)owner).IsNoUserMode || (entryData.RemoteId.HasValue && entryData.SyncState == SyncState.Synced))
                {
                    NotSyncedView.Visibility = ViewStates.Gone;
                }
                else
                {
                    NotSyncedView.Visibility = ViewStates.Visible;
                }
                var notSyncedShape = NotSyncedView.Background as GradientDrawable;
                if (entryData.RemoteId.HasValue)
                {
                    notSyncedShape.SetColor(ctx.Resources.GetColor(Resource.Color.light_gray));
                }
                else
                {
                    notSyncedShape.SetColor(ctx.Resources.GetColor(Resource.Color.material_red));
                }

                var info = datasource.Entry.Info;
                if (!string.IsNullOrWhiteSpace(info.ProjectData.Name))
                {
                    color = Color.ParseColor(ProjectData.HexColors [info.Color % ProjectData.HexColors.Length]);
                    ProjectTextView.SetTextColor(color);
                    ProjectTextView.Text = info.ProjectData.Name;
                }
                else
                {
                    ProjectTextView.Text = ctx.GetString(Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor(ctx.Resources.GetColor(Resource.Color.dark_gray_text));
                }

                if (string.IsNullOrWhiteSpace(info.ClientData.Name))
                {
                    ClientTextView.Text = string.Empty;
                    ClientTextView.Visibility = ViewStates.Gone;
                }
                else
                {
                    ClientTextView.Text = string.Format("{0} • ", info.ClientData.Name);
                    ClientTextView.Visibility = ViewStates.Visible;
                }

                if (string.IsNullOrWhiteSpace(info.TaskData.Name))
                {
                    TaskTextView.Text = string.Empty;
                    TaskTextView.Visibility = ViewStates.Gone;
                }
                else
                {
                    TaskTextView.Text = string.Format("{0} • ", info.TaskData.Name);
                    TaskTextView.Visibility = ViewStates.Visible;
                }

                if (string.IsNullOrWhiteSpace(entryData.Description))
                {
                    DescriptionTextView.Text = ctx.GetString(Resource.String.RecentTimeEntryNoDescription);
                }
                else
                {
                    DescriptionTextView.Text = entryData.Description;
                }

                BillableView.Visibility = entryData.IsBillable ? ViewStates.Visible : ViewStates.Gone;

                var shape = ColorView.Background as GradientDrawable;
                if (shape != null)
                {
                    shape.SetColor(color);
                }

                // Show start/stop btn
                if (datasource.Entry.Data.State == TimeEntryState.Running)
                {
                    ContinueImageButton.SetImageResource(Resource.Drawable.IcStop);
                }
                else
                {
                    ContinueImageButton.SetImageResource(Resource.Drawable.IcPlayArrowGrey);
                }

                // Set tag number
                var numberOfTags = datasource.Entry.Data.Tags.Count;
                TagsView.BubbleCount = numberOfTags;
                TagsView.Visibility = numberOfTags > 0 ? ViewStates.Visible : ViewStates.Gone;

                // Set duration
                duration = datasource.GetDuration();
                isRunning = datasource.Entry.Data.State == TimeEntryState.Running;
                RebindDuration();
            }

            private void RebindDuration()
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Elapsed -= OnDurationElapsed;
                    timer = null;
                }

                if (isRunning)
                {
                    timer = new Timer(1000 - duration.Milliseconds);
                    timer.Elapsed += OnDurationElapsed;
                    timer.Start();
                }

                DurationTextView.Text = TimeEntryData.GetFormattedDuration(duration);
            }

            private void OnDurationElapsed(object sender, ElapsedEventArgs e)
            {
                // Update duration with new time.
                duration = duration.Add(TimeSpan.FromMilliseconds(timer.Interval));
                BaseActivity.CurrentActivity.RunOnUiThread(() => RebindDuration());
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Elapsed -= OnDurationElapsed;
                        timer = null;
                    }
                }
                base.Dispose(disposing);
            }
        }

        class FooterHolder : RecyclerView.ViewHolder
        {
            ProgressBar progressBar;
            RelativeLayout retryLayout;
            Button retryButton;

            public FooterHolder(View root, LogTimeEntriesVM viewModel) : base(root)
            {
                retryLayout = ItemView.FindViewById<RelativeLayout> (Resource.Id.RetryLayout);
                progressBar = ItemView.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
                retryButton = ItemView.FindViewById<Button> (Resource.Id.RetryButton);
                retryButton.Click += (sender, e) => viewModel.LoadMore();
            }

            public void Bind(RecyclerLoadState state)
            {
                progressBar.Visibility = ViewStates.Gone;
                retryLayout.Visibility = ViewStates.Gone;

                if (state == RecyclerLoadState.Loading)
                {
                    progressBar.Visibility = ViewStates.Visible;
                }
                else if (state == RecyclerLoadState.Retry)
                {
                    retryLayout.Visibility = ViewStates.Visible;
                }
            }
        }
    }
}