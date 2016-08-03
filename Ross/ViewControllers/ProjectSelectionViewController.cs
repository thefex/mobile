using System;
using System.Collections.ObjectModel;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class ProjectSelectionViewController : UITableViewController
    {
        private const string TopProjectsKey = "ProjectTopProjects";

        private readonly static NSString ClientHeaderId = new NSString("ClientHeaderId");
        private readonly static NSString ProjectCellId = new NSString("ProjectCellId");
        private readonly static NSString TaskCellId = new NSString("TaskCellId");

        private const float CellSpacing = 4f;
        private Guid workspaceId;
        private ProjectListVM viewModel;
        private readonly IOnProjectSelectedHandler handler;

        public ProjectSelectionViewController(EditTimeEntryViewController editView) : base(UITableViewStyle.Plain)
        {
            Title = "ProjectTitle".Tr();
            this.workspaceId = editView.WorkspaceId;
            this.handler = editView;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Apply(Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;

            TableView.RowHeight = 60f;
            TableView.RegisterClassForHeaderFooterViewReuse(typeof(SectionHeaderView), ClientHeaderId);
            TableView.RegisterClassForCellReuse(typeof(ProjectCell), ProjectCellId);
            TableView.RegisterClassForCellReuse(typeof(TaskCell), TaskCellId);
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;

            var defaultFooterView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Gray);
            defaultFooterView.Frame = new CGRect(0, 0, 50, 50);
            defaultFooterView.StartAnimating();
            TableView.TableFooterView = defaultFooterView;

            viewModel = new ProjectListVM(StoreManager.Singleton.AppState, workspaceId);
            TableView.Source = new Source(this, viewModel);

            var addBtn = new UIBarButtonItem(UIBarButtonSystemItem.Add, OnAddNewProject);
            if (viewModel.WorkspaceList.Count > 1)
            {
                var filterBtn = new UIBarButtonItem(UIImage.FromFile("filter_icon.png"), UIBarButtonItemStyle.Plain, OnShowWorkspaceFilter);
                NavigationItem.RightBarButtonItems = new[] { filterBtn, addBtn };
            }
            else
            {
                NavigationItem.RightBarButtonItem = addBtn;
            }

            TableView.TableFooterView = null;

            UpdateTopProjectsHeader();
        }

        internal void UpdateTopProjectsHeader()
        {
            //Enumerates only once
            var topProjects = viewModel.TopProjects?.ToList();

            var numberOfProjects = topProjects?.Count ?? 0;
            if (numberOfProjects == 0)
            {
                TableView.TableHeaderView = null;
                return;
            }

            const int labelXMargin = 20;
            const int labelYMargin = 5;
            const int labelFrameHeight = 85;

            const int labelHeight = labelFrameHeight - labelYMargin;
            var labelWidth = View.Frame.Width - labelXMargin * 2;

            var headerRect = new CGRect(0, 0, labelWidth, labelFrameHeight * (numberOfProjects + 1));
            var headerView = new UIView(headerRect);

            const int headerLabelHeight = 50;

            var headerLabelRect = new CGRect(labelXMargin, labelYMargin, labelWidth, headerLabelHeight);
            var headerLabel = new UILabel(headerLabelRect) { Text = TopProjectsKey.Tr() };

            headerView.AddSubview(headerLabel);

            for (int i = 0; i < numberOfProjects; i++)
            {
                var project = topProjects[i];

                var buttonRect = new CGRect(labelXMargin, labelFrameHeight * i + headerLabelHeight + labelYMargin, labelWidth, labelHeight);
                var button = new UIButton(buttonRect);

                button.BackgroundColor = UIColor.Clear.FromHex(ProjectData.HexColors[project.Color % ProjectData.HexColors.Length]);
                button.TouchUpInside += (s, e) => OnItemSelected(project);
                button.TitleEdgeInsets = new UIEdgeInsets(10, labelXMargin, 0, labelXMargin);

                var projectLabelText = project.Task != null ? $"{project.Name} - {project.Task.Name}" : project.Name;
                var projectLabel = new UILabel(new CGRect(labelXMargin, 15, labelWidth, 20));
                projectLabel.Text = projectLabelText;
                projectLabel.TextColor = UIColor.White;
                button.AddSubview(projectLabel);

                var clientLabel = new UILabel(new CGRect(labelXMargin, 45, labelWidth, 20));
                clientLabel.Text = string.IsNullOrEmpty(project.ClientName) ? "ProjectNoClient".Tr() : project.ClientName;
                clientLabel.TextColor = UIColor.White;
                button.AddSubview(clientLabel);

                headerView.AddSubview(button);
            }

            TableView.TableHeaderView = headerView;
        }

        protected void OnItemSelected(ICommonData m)
        {
            Guid projectId = Guid.Empty;
            Guid taskId = Guid.Empty;

            if (m is ProjectData)
            {
                if (!(m is ProjectsCollection.SuperProjectData) || !((ProjectsCollection.SuperProjectData)m).IsEmpty)
                {
                    projectId = m.Id;
                }

                if (m is ProjectListVM.CommonProjectData)
                {
                    var commonProjectData = (ProjectListVM.CommonProjectData)m;
                    if (commonProjectData.Task != null)
                    {
                        taskId = commonProjectData.Task.Id;
                    }
                }
            }
            else if (m is TaskData)
            {
                var task = (TaskData)m;
                projectId = task.ProjectId;
                taskId = task.Id;
            }

            handler.OnProjectSelected(projectId, taskId);
            NavigationController.PopViewController(true);
        }

        private void OnAddNewProject(object sender, EventArgs evt)
        {
            var newProjectController = new NewProjectViewController(viewModel.CurrentWorkspaceId, handler);
            NavigationController.PushViewController(newProjectController, true);
        }

        private void OnShowWorkspaceFilter(object sender, EventArgs evt)
        {
            var sourceRect = new CGRect(NavigationController.Toolbar.Bounds.Width - 45, NavigationController.Toolbar.Bounds.Height, 1, 1);

            bool hasPopover = ObjCRuntime.Class.GetHandle("UIPopoverPresentationController") != IntPtr.Zero;
            if (hasPopover)
            {
                var popoverController = new WorkspaceSelectorPopover(viewModel, UpdateTopProjectsHeader, sourceRect);
                PresentViewController(popoverController, true, null);
            }
            else
            {
                var nextWorkspace = viewModel.CurrentWorkspaceIndex + 1;
                if (nextWorkspace > viewModel.WorkspaceList.Count - 1)
                {
                    nextWorkspace = 0;
                }
                viewModel.ChangeWorkspaceByIndex(nextWorkspace);
            }
        }

        public class Source : ObservableCollectionViewSource<ICommonData, IClientData, IProjectData>
        {
            private readonly ProjectSelectionViewController owner;
            private readonly ProjectListVM viewModel;

            public Source(ProjectSelectionViewController owner, ProjectListVM viewModel) : base(owner.TableView, viewModel.ProjectList)
            {
                this.owner = owner;
                this.viewModel = viewModel;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var index = GetPlainIndexFromRow(collection, indexPath);
                var data = collection[index];

                if (data is ProjectData)
                {
                    var cell = (ProjectCell)tableView.DequeueReusableCell(ProjectCellId);
                    cell.Bind((ProjectsCollection.SuperProjectData)data, viewModel.ProjectList.AddTasks);
                    return cell;
                }
                else
                {
                    var cell = (TaskCell)tableView.DequeueReusableCell(TaskCellId);
                    cell.Bind((TaskData)data);
                    return cell;
                }
            }

            public override UIView GetViewForHeader(UITableView tableView, nint section)
            {
                var index = GetPlainIndexFromSection(collection, section);
                var data = (ClientData)collection[index];

                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView(ClientHeaderId);
                view.Bind(data);
                return view;
            }

            public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            {
                return EstimatedHeightForHeader(tableView, section);
            }

            public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override nfloat EstimatedHeightForHeader(UITableView tableView, nint section)
            {
                return 42f;
            }

            public override bool CanEditRow(UITableView tableView, NSIndexPath indexPath)
            {
                return false;
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                var index = GetPlainIndexFromRow(collection, indexPath);
                var data = collection[index];
                owner.OnItemSelected(data);

                tableView.DeselectRow(indexPath, true);
            }
        }

        class ProjectCell : UITableViewCell
        {
            private UIView textContentView;
            private UILabel projectLabel;
            private UILabel clientLabel;
            private UIButton tasksButton;
            private ProjectsCollection.SuperProjectData projectData;
            private Action<ProjectData> onPressedTagBtn;

            public ProjectCell(IntPtr handle) : base(handle)
            {
                this.Apply(Style.Screen);
                BackgroundView = new UIView();

                ContentView.Add(textContentView = new UIView());
                ContentView.Add(tasksButton = new UIButton().Apply(Style.ProjectList.TasksButtons));
                textContentView.Add(projectLabel = new UILabel().Apply(Style.ProjectList.ProjectLabel));
                textContentView.Add(clientLabel = new UILabel().Apply(Style.ProjectList.ClientLabel));

                var maskLayer = new CAGradientLayer
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
                        NSNumber.FromFloat(0.9f),
                        NSNumber.FromFloat(1f),
                    },
                };

                textContentView.Layer.Mask = maskLayer;
                tasksButton.TouchUpInside += OnTasksButtonTouchUpInside;
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();

                var contentFrame = new CGRect(0, CellSpacing / 2, Frame.Width, Frame.Height - CellSpacing);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                if (!tasksButton.Hidden)
                {
                    var virtualWidth = contentFrame.Height;
                    var buttonWidth = tasksButton.CurrentBackgroundImage.Size.Width;
                    var extraPadding = (virtualWidth - buttonWidth) / 2f;
                    tasksButton.Frame = new CGRect(
                        contentFrame.Width - virtualWidth + extraPadding, extraPadding,
                        buttonWidth, buttonWidth);
                    contentFrame.Width -= virtualWidth;
                }

                contentFrame.X += 13f;
                contentFrame.Width -= 13f;
                textContentView.Frame = contentFrame;
                textContentView.Layer.Mask.Bounds = contentFrame;

                contentFrame = new CGRect(CGPoint.Empty, contentFrame.Size);

                if (clientLabel.Hidden)
                {
                    // Only display single item, so make it fill the whole text frame
                    var bounds = GetBoundingRect(projectLabel);
                    projectLabel.Frame = new CGRect(
                        x: 0,
                        y: (contentFrame.Height - bounds.Height + projectLabel.Font.Descender) / 2f,
                        width: contentFrame.Width,
                        height: bounds.Height
                    );
                }
                else
                {
                    // Carefully craft the layout
                    var bounds = GetBoundingRect(projectLabel);
                    projectLabel.Frame = new CGRect(
                        x: 0,
                        y: (contentFrame.Height - bounds.Height + projectLabel.Font.Descender) / 2f,
                        width: bounds.Width,
                        height: bounds.Height
                    );

                    const float clientLeftMargin = 7.5f;
                    bounds = GetBoundingRect(clientLabel);
                    clientLabel.Frame = new CGRect(
                        x: projectLabel.Frame.X + projectLabel.Frame.Width + clientLeftMargin,
                        y: (float)Math.Floor(projectLabel.Frame.Y + projectLabel.Font.Ascender - clientLabel.Font.Ascender),
                        width: bounds.Width,
                        height: bounds.Height
                    );
                }
            }

            public void Bind(ProjectsCollection.SuperProjectData projectData, Action<ProjectData> onPressedTagBtn, bool showClient = false)
            {
                this.projectData = projectData;
                this.onPressedTagBtn = onPressedTagBtn;

                if (projectData.IsEmpty)
                {
                    projectLabel.Text = "ProjectNoProject".Tr();
                    clientLabel.Hidden = true;
                    tasksButton.Hidden = true;
                    BackgroundView.BackgroundColor = Color.Gray;
                    projectLabel.Apply(Style.ProjectList.NoProjectLabel);
                    return;
                }

                var color = UIColor.Clear.FromHex(ProjectData.HexColors[projectData.Color % ProjectData.HexColors.Length]);
                BackgroundView.BackgroundColor = color;

                projectLabel.Text = projectData.Name;
                clientLabel.Text = projectData.ClientName;
                clientLabel.Hidden = !showClient;
                tasksButton.Hidden = projectData.TaskNumber == 0;
                tasksButton.Selected = false;
                tasksButton.SetTitleColor(color, UIControlState.Normal);
                tasksButton.SetTitle(projectData.TaskNumber.ToString(), UIControlState.Normal);

                // Layout content.
                LayoutSubviews();
            }

            private void OnTasksButtonTouchUpInside(object sender, EventArgs e)
            {
                if (onPressedTagBtn != null && projectData != null)
                {
                    onPressedTagBtn.Invoke(projectData);
                }
            }

            private static CGRect GetBoundingRect(UILabel view)
            {
                var attrs = new UIStringAttributes()
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

        class TaskCell : UITableViewCell
        {
            private readonly UILabel nameLabel;
            private readonly UIView separatorView;

            public TaskCell(IntPtr handle) : base(handle)
            {
                this.Apply(Style.Screen);
                ContentView.Add(nameLabel = new UILabel().Apply(Style.ProjectList.TaskLabel));
                ContentView.Add(separatorView = new UIView().Apply(Style.ProjectList.TaskSeparator));
                BackgroundView = new UIView().Apply(Style.ProjectList.TaskBackground);
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();

                var contentFrame = new CGRect(0, 0, Frame.Width, Frame.Height);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                // Add padding
                contentFrame.X = 15f;
                contentFrame.Y = 0;
                contentFrame.Width -= 15f;

                nameLabel.Frame = contentFrame;
                separatorView.Frame = new CGRect(
                    contentFrame.X, contentFrame.Y + contentFrame.Height - 1f,
                    contentFrame.Width, 1f);
            }

            public void Bind(TaskData data)
            {
                var taskName = data.Name;
                if (string.IsNullOrWhiteSpace(taskName))
                {
                    taskName = "ProjectNoNameTask".Tr();
                }
                nameLabel.Text = taskName;
            }
        }

        class SectionHeaderView : UITableViewHeaderFooterView
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel nameLabel;

            public SectionHeaderView(IntPtr ptr) : base(ptr)
            {
                nameLabel = new UILabel().Apply(Style.Log.HeaderDateLabel);
                ContentView.AddSubview(nameLabel);
                BackgroundView = new UIView().Apply(Style.Log.HeaderBackgroundView);
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();
                var contentFrame = ContentView.Frame;

                nameLabel.Frame = new CGRect(
                    x: HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );
            }

            public void Bind(ClientData data)
            {
                nameLabel.Text = string.IsNullOrEmpty(data.Name) ? "ProjectNoClient".Tr() : data.Name;
            }
        }

        class WorkspaceSelectorPopover : ObservableTableViewController<IWorkspaceData>, IUIPopoverPresentationControllerDelegate
        {
            private readonly ProjectListVM viewModel;
            private readonly Action updateTopProjectsHeader;
            private const int cellHeight = 45;

            public WorkspaceSelectorPopover(ProjectListVM viewModel, Action updateTopProjectsHeader, CGRect sourceRect)
            {
                this.viewModel = viewModel;
                this.updateTopProjectsHeader = updateTopProjectsHeader;
                ModalPresentationStyle = UIModalPresentationStyle.Popover;

                PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Up;
                PopoverPresentationController.BackgroundColor = UIColor.LightGray;
                PopoverPresentationController.SourceRect = sourceRect;
                PopoverPresentationController.Delegate = this;

                var height = (viewModel.WorkspaceList.Count < 5) ? (viewModel.WorkspaceList.Count + 1) : 5;
                PreferredContentSize = new CGSize(200, height * cellHeight);
            }

            public override void ViewDidLoad()
            {
                base.ViewDidLoad();

                UILabel headerLabel = new UILabel();
                headerLabel.Text = "Workspaces";
                headerLabel.Bounds = new CGRect(0, 10, 200, 40);
                headerLabel.Apply(Style.ProjectList.WorkspaceHeader);
                TableView.TableHeaderView = headerLabel;

                TableView.RowHeight = cellHeight;
                CreateCellDelegate = CreateWorkspaceCell;
                BindCellDelegate = BindCell;
                DataSource = new ObservableCollection<IWorkspaceData>(viewModel.WorkspaceList);
                PopoverPresentationController.SourceView = TableView;
            }

            private UITableViewCell CreateWorkspaceCell(NSString cellIdentifier)
            {
                return new UITableViewCell(UITableViewCellStyle.Default, cellIdentifier);
            }

            private void BindCell(UITableViewCell cell, IWorkspaceData workspaceData, NSIndexPath path)
            {
                // Set selected tags.
                cell.Accessory = (path.Row == viewModel.CurrentWorkspaceIndex) ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;
                cell.TextLabel.Text = workspaceData.Name;
                cell.TextLabel.Apply(Style.ProjectList.WorkspaceLabel);
            }

            protected override void OnRowSelected(object item, NSIndexPath indexPath)
            {
                base.OnRowSelected(item, indexPath);
                TableView.DeselectRow(indexPath, true);
                if (indexPath.Row == viewModel.CurrentWorkspaceIndex)
                {
                    return;
                }

                viewModel.ChangeWorkspaceByIndex(indexPath.Row);
                // Set cell unselected
                foreach (var cell in TableView.VisibleCells)
                {
                    cell.Accessory = UITableViewCellAccessory.None;
                }

                updateTopProjectsHeader();
                TableView.CellAt(indexPath).Accessory = UITableViewCellAccessory.Checkmark;
                DismissViewController(true, null);
            }

            [Export("adaptivePresentationStyleForPresentationController:")]
            public UIModalPresentationStyle GetAdaptivePresentationStyle(UIPresentationController controller)
            {
                return UIModalPresentationStyle.None;
            }
        }
    }
}
