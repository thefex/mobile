using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.ViewModels.Timer;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class LogViewController : UIViewController
    {
        private const float DateHeaderHeight = 58;
        private const float TimeEntryHeight = 80;
        private const float ListInsetTop = 20;

        private const int StatusBarHeight = 60;
        private const string DefaultDurationText = " 00:00:00 ";
        readonly static NSString EntryCellId = new NSString("EntryCellId");
        readonly static NSString SectionCellId = new NSString("SectionCellId");

        private SimpleEmptyView defaultEmptyView;
        private UIView obmEmptyView;
        private UIView reloadView;
        private UIActivityIndicatorView defaultFooterView;
        private StatusView statusView;
        private UITableView tableView;
        private TimerBar timerBar;
        private SectionCell floatingHeader;
        private float heightOfTopBars;

        private Binding<LogTimeEntriesVM.LoadInfoType, LogTimeEntriesVM.LoadInfoType> loadInfoBinding, loadMoreBinding;
        private Binding<int, int> hasItemsBinding;
        private Binding<string, string> durationBinding;
        private Binding<bool, bool> syncErrorBinding, syncBinding, hasErrorBinding, isRunningBinding, constrainErrorBinding;
        private Binding<ObservableCollection<IHolder>, ObservableCollection<IHolder>> collectionBinding;

        protected LogTimeEntriesVM ViewModel { get; set; }

        public override void LoadView()
        {
            base.LoadView();

            NavigationItem.LeftBarButtonItem = new UIBarButtonItem(
                Image.IconNav.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal),
                UIBarButtonItemStyle.Plain, OnNavigationBtnPressed);

            heightOfTopBars = (float)(NavigationController.NavigationBar.Bounds.Height
                                      + UIApplication.SharedApplication.StatusBarFrame.Height);

            var tableFrame = View.Frame;
            tableFrame.Y -= heightOfTopBars;
            var tableInset = new UIEdgeInsets(heightOfTopBars, 0, 72, 0);
            var tableScrollInset = tableInset;
            tableScrollInset.Top += DateHeaderHeight - ListInsetTop;
            Add(tableView = new UITableView(tableFrame, UITableViewStyle.Plain)
            {
                ContentInset = tableInset,
                ScrollIndicatorInsets = tableScrollInset,
            } .Apply(Style.Log.EntryList));

            Add(floatingHeader = new FloatingSectionCell { Hidden = true});

            timerBar = new TimerBar
            {
                Frame = new CGRect(0, this.View.Frame.Height - 72 - heightOfTopBars, this.View.Bounds.Width, 72)
            };
            Add(timerBar);
            timerBar.StartButtonHit += onTimerStartButtonHit;

            statusView = new StatusView
            {
                Retry = OnStatusRetryBtnPressed,
                Cancel = () => StatusBarShown = false,
                StatusFailText = "ReportsStatusFailText".Tr(),
                StatusSyncingText = "ReportsStatusSyncText".Tr()
            };
            Add(statusView);

            defaultEmptyView = new SimpleEmptyView
            {
                Title = "LogEmptyTitle".Tr(),
                Message = "LogEmptyMessage".Tr(),
            };

            obmEmptyView = new OBMEmptyView
            {
                Title = "LogOBMEmptyTitle".Tr(),
                Message = "LogOBMEmptyMessage".Tr(),
            };

            reloadView = new ReloadTableViewFooter
            {
                SyncButtonPressedHandler = OnTryAgainBtnPressed
            };
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            EdgesForExtendedLayout = UIRectEdge.None;
            tableView.RegisterClassForCellReuse(typeof(TimeEntryCell), EntryCellId);
            tableView.RegisterClassForCellReuse(typeof(SectionCell), SectionCellId);
            tableView.SetEditing(false, true);

            // Create view model
            ViewModel = new LogTimeEntriesVM(StoreManager.Singleton.AppState);

            var headerView = new TableViewRefreshView();
            headerView.AdaptToTableView(tableView);
            headerView.ValueChanged += (sender, e) => ViewModel.TriggerFullSync();

            // Bindings
            syncBinding = this.SetBinding(() => ViewModel.IsFullSyncing).WhenSourceChanges(() =>
            {
                if (!ViewModel.IsFullSyncing)
                {
                    headerView.EndRefreshing();
                }
            });
            syncErrorBinding = this.SetBinding(() => ViewModel.HasSyncErrors).WhenSourceChanges(() =>
            {
                StatusBarShown = ViewModel.HasSyncErrors;
                if (ViewModel.HasSyncErrors)
                {
                    statusView.StatusFailText = StoreManager.Singleton.AppState.RequestInfo.ErrorInfo.Item1;
                }
            });
            hasItemsBinding = this.SetBinding(() => ViewModel.Collection.Count).WhenSourceChanges(SetCollectionState);
            loadMoreBinding = this.SetBinding(() => ViewModel.LoadInfo).WhenSourceChanges(LoadMoreIfNeeded);
            loadInfoBinding = this.SetBinding(() => ViewModel.LoadInfo).WhenSourceChanges(SetFooterState);
            constrainErrorBinding = this.SetBinding(() => ViewModel.HasCRUDError).WhenSourceChanges(() =>
            {
                ShowConstrainError(StoreManager.Singleton.AppState.RequestInfo.ErrorInfo);
            });
            collectionBinding = this.SetBinding(() => ViewModel.Collection).WhenSourceChanges(() =>
            {
                var source = new TimeEntriesSource(this, ViewModel, floatingHeader);
                if (tableView.Source != null)
                {
                    ((TimeEntriesSource)tableView.Source).Dispose();
                }
                source.Scroll += onTableViewScrolled;
                tableView.Source = source;
                updateFloatingHeader();
                ViewModel.Collection.CollectionChanged += (s, e) => updateFloatingHeader();
            });
            isRunningBinding = this.SetBinding(() => ViewModel.IsEntryRunning).WhenSourceChanges(SetStartStopButtonState);
            durationBinding = this.SetBinding(() => ViewModel.Duration).WhenSourceChanges(setDuration);
        }

        public override void ViewWillDisappear(bool animated)
        {
            if (IsMovingFromParentViewController)
            {
                ViewModel.Dispose();
            }
            base.ViewWillDisappear(animated);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            defaultEmptyView.Frame = new CGRect(0, (View.Frame.Size.Height - 200f) / 2, View.Frame.Size.Width, 200f);
            obmEmptyView.Frame = new CGRect(0, 15f, View.Frame.Size.Width, 200f);
            reloadView.Bounds = new CGRect(0f, 0f, View.Frame.Size.Width, 70f);
            reloadView.Center = new CGPoint(View.Center.X, reloadView.Center.Y);
            statusView.Frame = new CGRect(0, View.Frame.Height, View.Frame.Width, StatusBarHeight);
        }

        private void onTableViewScrolled(object sender, EventArgs e)
        {
            updateFloatingHeader();
        }

        private void updateFloatingHeader()
        {
            var point = new CGPoint(0, heightOfTopBars + tableView.ContentOffset.Y);
            var nsIndex = tableView.IndexPathForRowAtPoint(point);

            if (nsIndex == null)
            {
                floatingHeader.Hidden = true;
            }
            else
            {
                var source = (TimeEntriesSource)tableView.Source;

                var sectionIndex = source.GetSectionCellIndexForIndex(tableView, nsIndex.Row);

                var sectionVM = source.GetSectionViewModelAt(sectionIndex);

                var frame = getFloatingHeaderFrame(sectionVM);

                if (frame == null)
                {
                    floatingHeader.Hidden = true;
                    return;
                }

                floatingHeader.Bind(sectionVM);
                floatingHeader.Frame = frame.Value;
                floatingHeader.Hidden = false;
            }
        }

        private CGRect? getFloatingHeaderFrame(DateHolder displayedSectionVM)
        {
            var source = (TimeEntriesSource)tableView.Source;

            var point2 = new CGPoint(0, heightOfTopBars + tableView.ContentOffset.Y + DateHeaderHeight - 1 - ListInsetTop);
            var nsIndex2 = tableView.IndexPathForRowAtPoint(point2);

            var frame = new CGRect(0, 0, this.View.Frame.Width, DateHeaderHeight - ListInsetTop);

            if (nsIndex2 != null)
            {
                var nextSectionVM = source.GetSectionViewModelAt(nsIndex2.Row);

                if (nextSectionVM != null)
                {
                    if (nextSectionVM == displayedSectionVM)
                        return null;

                    var nextSectionRect = tableView.RectForRowAtIndexPath(nsIndex2);

                    frame.Y = nextSectionRect.Y - tableView.ContentOffset.Y - DateHeaderHeight - heightOfTopBars + ListInsetTop;
                }
            }

            return frame;
        }

        private void onTimerStartButtonHit(object sender, EventArgs e)
        {
            if (timerBar.IsManualModeSwitchOn)
            {
                openEditViewForManualEntry();
            }
            else
            {
                startStop();
            }
        }

        private async void startStop()
        {
            // Send experiment data.
            ViewModel.ReportExperiment(OBMExperimentManager.StartButtonActionKey,
                                       OBMExperimentManager.ClickActionValue);

            if (!ViewModel.IsEntryRunning)
            {
                var te = await ViewModel.StartNewTimeEntryAsync();

                openEditViewForNewEntry(te);
            }
            else
            {
                ViewModel.StopTimeEntry();
            }
        }

        private void openEditViewForNewEntry(ITimeEntryData te)
        {
            var editController = EditTimeEntryViewController.ForExistingEntry(te.Id);
            var projectViewController = getProjectViewControllerIfChooseProjectForNew(editController);

            NavigationController.PushViewControllers(true, editController, projectViewController);
        }

        private void openEditViewForManualEntry()
        {
            var editController = EditTimeEntryViewController.ForManualAddition();
            var now = DateTime.Now;
            var durationViewController = new DurationChangeViewController(editController);
            var projectViewController = getProjectViewControllerIfChooseProjectForNew(editController);

            NavigationController.PushViewControllers(true, editController, durationViewController, projectViewController);
        }

        private ProjectSelectionViewController getProjectViewControllerIfChooseProjectForNew(
            EditTimeEntryViewController editController)
        {
            return StoreManager.Singleton.AppState.Settings.ChooseProjectForNew
                   ? new ProjectSelectionViewController(editController)
                   : null;
        }

        private void SetStartStopButtonState()
        {
            if (ViewModel.IsEntryRunning)
            {
                this.timerBar.SetState(TimerBar.State.TimerRunning);
            }
            else
            {
                if (!this.timerBar.IsManualModeSwitchOn)
                {
                    this.timerBar.SetState(TimerBar.State.TimerInactive);
                }
            }
        }
        private void setDuration()
        {
            timerBar.SetDurationText(ViewModel.Duration);
        }

        private void LoadMoreIfNeeded()
        {
            // ATTENTION: Small hack due to the scroll needs more than the
            // 10 items to work correctly and load more items. With this conditions,
            // we avoid the scroll spinner to dissapear forcing
            // an extra load.
            if (ViewModel.Collection.Count > 0 &&
                    ViewModel.Collection.Count < 10 &&
                    ViewModel.LoadInfo.HasMore &&
                    !ViewModel.LoadInfo.HadErrors &&
                    !ViewModel.LoadInfo.IsSyncing)
            {
                ViewModel.LoadMore();
            }
        }

        private void SetFooterState()
        {

            if (ViewModel.LoadInfo.HasMore && !ViewModel.LoadInfo.HadErrors)
            {
                if (defaultFooterView == null)
                {
                    defaultFooterView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Gray);
                    defaultFooterView.Frame = new CGRect(0, 0, 50, 50);
                    defaultFooterView.StartAnimating();
                }
                tableView.TableFooterView = defaultFooterView;
            }
            else if (ViewModel.LoadInfo.HasMore && ViewModel.LoadInfo.HadErrors)
            {
                tableView.TableFooterView = reloadView;
            }
            else if (!ViewModel.LoadInfo.HasMore && !ViewModel.LoadInfo.HadErrors)
            {
                SetCollectionState();
            }
        }

        private void SetCollectionState()
        {
            // ATTENTION Needed condition to keep visible the list
            // while the first sync is finishing. Why? Because the scroll spinner
            // is used and we need the TableView visible.
            if (ViewModel.LoadInfo.IsSyncing && ViewModel.Collection.Count == 0)
            {
                return;
            }

            UIView emptyView = defaultEmptyView; // Default empty view.
            var showWelcome = ViewModel.ShowWelcomeScreen();
            var hasItems = ViewModel.Collection.Count > 0;
            var isInExperiment = ViewModel.IsInExperiment();

            // According to settings, show welcome message or no.
            ((SimpleEmptyView)emptyView).Title = showWelcome ? "LogWelcomeTitle".Tr() : "LogEmptyTitle".Tr();

            if (showWelcome && isInExperiment)
            {
                emptyView = obmEmptyView;
            }

            tableView.TableFooterView = hasItems ? new UIView() : emptyView;
        }

        private void OnCountinueTimeEntry(int index)
        {
            ViewModel.ContinueTimeEntry(index);
        }

        private void OnTryAgainBtnPressed()
        {
            ViewModel.LoadMore();
        }

        private void OnNavigationBtnPressed(object sender, EventArgs e)
        {
            var main = AppDelegate.TogglWindow.RootViewController as MainViewController;
            main.ToggleMenu();
        }

        private void OnStatusRetryBtnPressed()
        {
            ViewModel.TriggerFullSync();
        }

        private void ShowConstrainError(Tuple<string, Guid> lastErrorInfo)
        {
            if (ViewModel.HasCRUDError)
            {
                var alert = new UIAlertView(
                    "RequiredfieldMessage".Tr(),
                    lastErrorInfo.Item1,
                    null, "RequiredfieldEditBtn".Tr());
                alert.Clicked += (sender, e) =>
                {
                    var controllers = new List<UIViewController>(NavigationController.ViewControllers);
                    var editController = EditTimeEntryViewController.ForExistingEntry(lastErrorInfo.Item2);
                    controllers.Add(editController);
                    NavigationController.SetViewControllers(controllers.ToArray(), true);
                };
                alert.Show();
            }
        }

        private bool showStatus;

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

                var size = View.Frame.Size;
                // small hack to avoid a weird effect when
                // the status is defined before align subviews.
                if (statusView.Frame.Y < size.Height - StatusBarHeight)
                    statusView.Frame = new CGRect(0, View.Frame.Height, View.Frame.Width, StatusBarHeight);

                UIView.Animate(0.5f, () =>
                {
                    var statusY = showStatus ? size.Height - StatusBarHeight : size.Height + 2f;
                    statusView.Frame = new CGRect(0, statusY, size.Width, StatusBarHeight);
                });
            }
        }

        #region TableViewSource
        class TimeEntriesSource : PlainObservableCollectionViewSource<IHolder>
        {
            private bool isLoading;
            private readonly LogTimeEntriesVM VM;
            private LogViewController owner;
            private IDisposable durationSuscriber;
            private readonly SectionCell floatingHeader;

            public event EventHandler Scroll;

            public TimeEntriesSource(LogViewController owner, LogTimeEntriesVM viewModel, SectionCell floatingHeader)
            : base(owner.tableView, viewModel.Collection)
            {
                this.floatingHeader = floatingHeader;
                this.owner = owner;
                VM = viewModel;
                durationSuscriber = viewModel.TimerObservable.Subscribe(x => updateDuration());
            }

            private void updateDuration()
            {
                foreach (var item in tableView.VisibleCells)
                {
                    ((IDurationCell)item).UpdateDuration();
                }
                this.floatingHeader.UpdateDuration();
            }

            public int GetSectionCellIndexForIndex(UITableView tableView, int index)
            {
                var i = index;
                while (true)
                {
                    var holder = collection.ElementAt(i);
                    if (holder is ITimeEntryHolder)
                    {
                        i--;
                        continue;
                    }

                    return i;
                }
            }

            public DateHolder GetSectionViewModelAt(int index)
            {
                return collection.ElementAt(index) as DateHolder;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                UITableViewCell cell;
                var holder = collection.ElementAt(indexPath.Row);

                if (holder is ITimeEntryHolder)
                {
                    cell = tableView.DequeueReusableCell(EntryCellId, indexPath);
                    ((TimeEntryCell)cell).Bind((ITimeEntryHolder)holder, OnContinueTimeEntry);
                }
                else
                {
                    cell = tableView.DequeueReusableCell(SectionCellId, indexPath);
                    ((SectionCell)cell).Bind((DateHolder)holder);
                }

                return cell;
            }

            public override UIView GetViewForHeader(UITableView tableView, nint section)
            {
                return new UIView().Apply(Style.ProjectList.HeaderBackgroundView);
            }

            public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            {
                return EstimatedHeightForHeader(tableView, section);
            }

            public override nfloat EstimatedHeightForHeader(UITableView tableView, nint section)
            {
                return -1f;
            }

            public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath)
            {
                return TimeEntryHeight;
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                if (indexPath.Row == 0)
                {
                    return DateHeaderHeight - ListInsetTop;
                }

                var holder = collection.ElementAt(indexPath.Row);
                if (holder is DateHolder)
                {
                    return DateHeaderHeight;
                }
                return EstimatedHeight(tableView, indexPath);
            }

            public override bool CanFocusRow(UITableView tableView, NSIndexPath indexPath)
            {
                return collection.ElementAt(indexPath.Row) is ITimeEntryHolder;
            }

            public override bool CanEditRow(UITableView tableView, NSIndexPath indexPath)
            {
                return collection.ElementAt(indexPath.Row) is ITimeEntryHolder;
            }

            public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
            {
                if (editingStyle == UITableViewCellEditingStyle.Delete)
                {
                    VM.RemoveTimeEntry(indexPath.Row);
                }
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                var holder = collection.ElementAt(indexPath.Row) as ITimeEntryHolder;
                if (holder != null)
                    owner.NavigationController.PushViewController(EditTimeEntryViewController.ForExistingEntry(holder.Entry.Data.Id), true);
                tableView.DeselectRow(indexPath, true);
            }

            public override void Scrolled(UIScrollView scrollView)
            {
                var currentOffset = scrollView.ContentOffset.Y;
                var maximumOffset = scrollView.ContentSize.Height - scrollView.Frame.Height;

                if (isLoading)
                {
                    isLoading &= maximumOffset - currentOffset <= 200.0;
                }

                if (!isLoading && maximumOffset - currentOffset <= 200.0)
                {
                    isLoading = true;
                    VM.LoadMore();
                }

                this.Scroll?.Invoke(this, EventArgs.Empty);
            }

            private void OnContinueTimeEntry(TimeEntryCell cell)
            {
                var indexPath = TableView.IndexPathForCell(cell);
                if (indexPath != null)
                {
                    // TODO: Rx needs further test!
                    //TableView.ScrollToRow(NSIndexPath.FromRowSection(0, 0), UITableViewScrollPosition.Top, true);
                    VM.ContinueTimeEntry(indexPath.Row);
                }
                else
                {
                    Phoebe.Helpers.Util.Log(Phoebe.Logging.LogLevel.Warning,
                                            nameof(OnContinueTimeEntry), "Cannot find indexPath for cell");
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (durationSuscriber != null)
                    {
                        durationSuscriber.Dispose();
                        durationSuscriber = null;
                    }
                    this.Scroll = null;
                }
                base.Dispose(disposing);
            }
        }
        #endregion

        #region Cells
        interface IDurationCell
        {
            void UpdateDuration();
        }

        class TimeEntryCell : SwipableTimeEntryTableViewCell, IDurationCell
        {
            private readonly UIView textContentView;
            private readonly UILabel projectLabel;
            private readonly UILabel clientLabel;
            private readonly UILabel taskLabel;
            private readonly UILabel descriptionLabel;
            private readonly UIImageView billableImage;
            private readonly UIImageView tagsImage;
            private readonly UILabel durationLabel;
            private readonly CircleView runningCircle;
            private readonly UIView descriptionTaskSeparator;
            private readonly CircleView projectCircle;
            private readonly CALayer runningCirclePointer;

            private bool isRunning;
            private TimeSpan duration;
            private Action<TimeEntryCell> OnContinueAction;
            private DateTime startTime;

            public TimeEntryCell(IntPtr ptr) : base(ptr)
            {
                textContentView = new UIView();
                projectLabel = new UILabel().Apply(Style.Log.CellProjectLabel);
                clientLabel = new UILabel().Apply(Style.Log.CellClientLabel);
                taskLabel = new UILabel().Apply(Style.Log.CellTaskLabel);
                descriptionLabel = new UILabel().Apply(Style.Log.CellDescriptionLabel);

                descriptionTaskSeparator = new UIView().Apply(Style.TimeEntryCell.DescriptionTaskSeparator);
                projectCircle = new CircleView();

                billableImage = new UIImageView().Apply(Style.TimeEntryCell.BillableImage);
                tagsImage = new UIImageView().Apply(Style.TimeEntryCell.TagsImage);
                durationLabel = new UILabel().Apply(Style.Log.CellDurationLabel);
                runningCircle = new CircleView().Apply(Style.TimeEntryCell.RunningIndicator);
                runningCircle.Layer.AddSublayer(
                    this.runningCirclePointer = new CAShapeLayer().Apply(Style.TimeEntryCell.RunningIndicatorPointer)
                );
                updateRunningIndicatorTransform();

                textContentView.AddSubviews(
                    projectLabel, clientLabel,
                    taskLabel, descriptionLabel,
                    descriptionTaskSeparator, projectCircle
                );

                var maskLayer = new CAGradientLayer()
                {
                    AnchorPoint = CGPoint.Empty,
                    StartPoint = new CGPoint(0.0f, 0.0f),
                    EndPoint = new CGPoint(1.0f, 0.0f),
                    Colors = new[]
                    {
                        UIColor.FromWhiteAlpha(1, 1).CGColor,
                        UIColor.FromWhiteAlpha(1, 1).CGColor,
                        UIColor.FromWhiteAlpha(1, 0).CGColor,
                    },
                    Locations = new[]
                    {
                        NSNumber.FromFloat(0f),
                        NSNumber.FromFloat(0.7f),
                        NSNumber.FromFloat(1f),
                    },
                };
                textContentView.Layer.Mask = maskLayer;

                ActualContentView.AddSubviews(
                    textContentView,
                    billableImage,
                    tagsImage,
                    durationLabel,
                    runningCircle
                );

                PreservesSuperviewLayoutMargins = false;
                SeparatorInset = UIEdgeInsets.Zero;
                LayoutMargins = UIEdgeInsets.Zero;
            }

            public void Bind(ITimeEntryHolder dataSource, Action<TimeEntryCell> OnContinueAction)
            {
                this.OnContinueAction = OnContinueAction;

                var projectName = "LogCellNoProject".Tr();
                var projectColor = Color.Gray.CGColor;
                var clientName = string.Empty;
                var info = dataSource.Entry.Info;

                var hasProject = !string.IsNullOrWhiteSpace(info.ProjectData.Name);

                this.projectCircle.Hidden = !hasProject;

                if (hasProject)
                {
                    projectName = info.ProjectData.Name;
                    projectColor = UIColor.Clear.FromHex(ProjectData.HexColors[info.ProjectData.Color % ProjectData.HexColors.Length]).CGColor;

                    if (!string.IsNullOrWhiteSpace(info.ClientData.Name))
                    {
                        clientName = info.ClientData.Name;
                    }
                }

                if (projectCircle.Color != projectColor)
                {
                    projectCircle.Color = projectColor;
                    SetNeedsLayout();
                }
                if (projectLabel.Text != projectName)
                {
                    projectLabel.Text = projectName;
                    SetNeedsLayout();
                }
                if (clientLabel.Text != clientName)
                {
                    clientLabel.Text = clientName;
                    SetNeedsLayout();
                }

                var taskName = info.TaskData.Name;
                var taskHidden = string.IsNullOrWhiteSpace(taskName);
                var description = dataSource.Entry.Data.Description;
                var descHidden = string.IsNullOrWhiteSpace(description);

                if (taskHidden && descHidden)
                {
                    description = "LogCellNoDescription".Tr();
                    descHidden = false;
                }
                var taskDeskSepHidden = taskHidden || descHidden;

                if (taskLabel.Hidden != taskHidden || taskLabel.Text != taskName)
                {
                    taskLabel.Hidden = taskHidden;
                    taskLabel.Text = taskName;
                    SetNeedsLayout();
                }
                if (descriptionLabel.Hidden != descHidden || descriptionLabel.Text != description)
                {
                    descriptionLabel.Hidden = descHidden;
                    descriptionLabel.Text = description;
                    SetNeedsLayout();
                }
                if (descriptionTaskSeparator.Hidden != taskDeskSepHidden)
                {
                    descriptionTaskSeparator.Hidden = taskDeskSepHidden;
                    SetNeedsLayout();
                }


                // Set duration
                duration = dataSource.GetDuration();
                startTime = dataSource.GetStartTime();
                isRunning = dataSource.Entry.Data.State == TimeEntryState.Running;


                if (isRunning)
                {
                    setSwipeText("SwipeTimeEntryStop".Tr());
                    setSwipeBackgroundColor(Color.StopButton);
                }
                else
                {
                    setSwipeText("SwipeTimeEntryContinue".Tr());
                    setSwipeBackgroundColor(Color.StartButton);
                }

                RebindTags(dataSource);
                RebindDuration();
                LayoutIfNeeded();
            }

            public void UpdateDuration()
            {
                if (isRunning)
                {
                    duration = Time.UtcNow.Truncate(TimeSpan.TicksPerSecond) - startTime.ToUtc();
                }
                RebindDuration();
            }

            // Rebind duration with the saved state "lastDataSource"
            // TODO: Try to find a stateless method.
            private void RebindDuration()
            {
                runningCircle.Hidden = !isRunning;
                durationLabel.Text = string.Format("{0:D2}:{1:mm}:{1:ss}", (int)duration.TotalHours, duration);
                updateRunningIndicatorTransform();
            }

            private void updateRunningIndicatorTransform()
            {
                if (isRunning)
                {
                    runningCirclePointer.AffineTransform = CGAffineTransform.Rotate(
                            CGAffineTransform.MakeTranslation(
                                Style.TimeEntryCell.RunningIndicatorRadius + 1,
                                Style.TimeEntryCell.RunningIndicatorRadius + 1),
                            duration.Seconds / 60f * (float)Math.PI * 2f
                                                           );
                }
                else
                {
                    runningCirclePointer.AffineTransform = CGAffineTransform.MakeTranslation(
                            Style.TimeEntryCell.RunningIndicatorRadius + 1,
                            Style.TimeEntryCell.RunningIndicatorRadius + 1);
                }
            }

            private void RebindTags(ITimeEntryHolder dataSource)
            {
                this.tagsImage.Hidden = dataSource.Entry.Data.Tags.Count == 0;
                this.billableImage.Hidden = !dataSource.Entry.Data.IsBillable;
            }

            protected override void OnContinueGestureFinished()
            {
                if (OnContinueAction != null)
                {
                    OnContinueAction.Invoke(this);
                }
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();

                var contentFrame = ContentView.Frame;

                const float labelHeight = 24;
                const float paddingX = 16;

                var centerY = contentFrame.Height / 2;

                var topRowY = centerY - labelHeight;

                // duration, icons
                {
                    const float durationLabelWidth = 80f;
                    durationLabel.Frame = new CGRect(
                        x: contentFrame.Width - durationLabelWidth - paddingX,
                        y: topRowY,
                        width: durationLabelWidth,
                        height: labelHeight
                    );

                    nfloat offsetX = contentFrame.Width - paddingX;

                    var rowCenterY = centerY + labelHeight / 2;

                    if (!runningCircle.Hidden)
                    {
                        offsetX -= Style.TimeEntryCell.RunningIndicatorRadius * 2 + 3;
                        runningCircle.SetFrame(
                            offsetX,
                            rowCenterY - Style.TimeEntryCell.RunningIndicatorRadius,
                            Style.TimeEntryCell.RunningIndicatorRadius * 2,
                            Style.TimeEntryCell.RunningIndicatorRadius * 2
                        );
                        offsetX -= 3;
                    }

                    if (!billableImage.Hidden)
                    {
                        offsetX -= Style.TimeEntryCell.IconSize;
                        billableImage.Frame = new CGRect(
                            offsetX,
                            rowCenterY - Style.TimeEntryCell.IconSize / 2,
                            Style.TimeEntryCell.IconSize,
                            Style.TimeEntryCell.IconSize
                        );
                    }
                    if (!tagsImage.Hidden)
                    {
                        offsetX -= Style.TimeEntryCell.IconSize;
                        tagsImage.Frame = new CGRect(
                            offsetX,
                            rowCenterY - Style.TimeEntryCell.IconSize / 2,
                            Style.TimeEntryCell.IconSize,
                            Style.TimeEntryCell.IconSize
                        );
                    }
                }

                // text container
                {
                    textContentView.Frame = new CGRect(
                        x: 0, y: 0,
                        width: contentFrame.Width - 90,
                        height: contentFrame.Height
                    );
                    textContentView.Layer.Mask.Bounds = textContentView.Frame;
                }

                // project, client
                {
                    nfloat offsetX = paddingX;

                    var bounds = GetBoundingRect(projectLabel);
                    projectLabel.Frame = new CGRect(
                        x: offsetX,
                        y: topRowY,
                        width: bounds.Width,
                        height: labelHeight
                    );
                    offsetX += bounds.Width + 6;

                    if (!projectCircle.Hidden)
                    {
                        projectCircle.SetFrame(
                            offsetX - 2,
                            topRowY + labelHeight / 2 - Style.TimeEntryCell.ProjectCircleRadius + 1,
                            Style.TimeEntryCell.ProjectCircleRadius * 2,
                            Style.TimeEntryCell.ProjectCircleRadius * 2
                        );
                        offsetX += Style.TimeEntryCell.ProjectCircleRadius * 2 + 6;
                    }

                    bounds = GetBoundingRect(clientLabel);
                    clientLabel.Frame = new CGRect(
                        x: offsetX,
                        y: topRowY,
                        width: bounds.Width,
                        height: labelHeight
                    );
                }

                // description, task
                {
                    nfloat offsetX = paddingX;

                    if (!descriptionLabel.Hidden)
                    {
                        var bounds = GetBoundingRect(descriptionLabel);
                        descriptionLabel.Frame = new CGRect(
                            x: offsetX,
                            y: centerY,
                            width: bounds.Width,
                            height: labelHeight
                        );

                        offsetX += bounds.Width + 4;
                    }

                    if (!descriptionTaskSeparator.Hidden)
                    {
                        descriptionTaskSeparator.Frame = new CGRect(
                            offsetX - 1,
                            centerY + labelHeight / 2 - Style.TimeEntryCell.DescriptionTaskSeparatorRadius + 1,
                            Style.TimeEntryCell.DescriptionTaskSeparatorRadius * 2,
                            Style.TimeEntryCell.DescriptionTaskSeparatorRadius * 2
                        );
                        offsetX += Style.TimeEntryCell.DescriptionTaskSeparatorRadius * 2 + 4;
                    }

                    if (!taskLabel.Hidden)
                    {
                        var bounds = GetBoundingRect(taskLabel);
                        taskLabel.Frame = new CGRect(
                            x: offsetX,
                            y: centerY,
                            width: bounds.Width,
                            height: labelHeight
                        );
                    }
                }
            }

            private static CGRect GetBoundingRect(UILabel view)
            {
                var attrs = new UIStringAttributes
                {
                    Font = view.Font,
                };
                var rect = ((NSString)(view.Text ?? string.Empty)).GetBoundingRect(
                               new CGSize(float.MaxValue, float.MaxValue),
                               NSStringDrawingOptions.UsesLineFragmentOrigin,
                               attrs, null);
                rect.Height = (float)Math.Ceiling(rect.Height);
                return rect;
            }
        }

        private class FloatingSectionCell : SectionCell
        {
            private UIView bottomBorder;

            protected override void makeBackground()
            {
                BackgroundView = new UIToolbar().Apply(Style.Toolbar);
                Add(bottomBorder = new UIView().Apply(Style.Log.SectionBorder));
                ClipsToBounds = true;
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();
                var contentFrame = ContentView.Frame;

                bottomBorder.Frame = new CGRect(0, contentFrame.Height - 1, contentFrame.Width, 1);
            }
        }

        private class SectionCell : UITableViewCell, IDurationCell
        {
            private const float HorizSpacing = 16f;
            private UILabel dateLabel;
            private UILabel totalDurationLabel;
            private bool isRunning;
            private TimeSpan duration;
            private DateTime date;

            public SectionCell(IntPtr handle) : base(handle)
            {
                this.setupUI();
            }

            protected SectionCell()
            {
                this.setupUI();
            }

            private void setupUI()
            {
                dateLabel = new UILabel().Apply(Style.Log.HeaderDateLabel);
                ContentView.AddSubview(dateLabel);

                totalDurationLabel = new UILabel().Apply(Style.Log.HeaderDurationLabel);
                ContentView.AddSubview(totalDurationLabel);

                PreservesSuperviewLayoutMargins = false;
                SeparatorInset = UIEdgeInsets.Zero;
                LayoutMargins = UIEdgeInsets.Zero;

                makeBackground();
            }

            protected virtual void makeBackground()
            {
                UserInteractionEnabled = false;
                BackgroundView = new UIView().Apply(Style.Log.HeaderBackgroundView);
            }

            public void UpdateDuration()
            {
                if (isRunning)
                {
                    duration = duration.Add(TimeSpan.FromSeconds(1));
                    SetContentData();
                }
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
                dateLabel.Text = date.ToLocalizedDateString();
                totalDurationLabel.Text = FormatDuration(duration);
            }

            private string FormatDuration(TimeSpan dr)
            {
                if (dr.TotalHours >= 1f)
                {
                    return string.Format(
                               "LogHeaderDurationHoursMinutes".Tr(),
                               (int)dr.TotalHours,
                               dr.Minutes
                           );
                }

                if (dr.Minutes > 0)
                {
                    return string.Format(
                               "LogHeaderDurationMinutes".Tr(),
                               dr.Minutes
                           );
                }

                return string.Empty;
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();
                var contentFrame = ContentView.Frame;

                var h = 14 + 18 * 2;
                var w = (contentFrame.Width - 3 * HorizSpacing) / 2;
                var y = contentFrame.Height - h;

                dateLabel.Frame = new CGRect(
                    x: HorizSpacing,
                    y: y,
                    width: w,
                    height: h
                );

                totalDurationLabel.Frame = new CGRect(
                    x: w + HorizSpacing * 2,
                    y: y,
                    width: w,
                    height: h
                );
            }
        }
        #endregion
    }
}
