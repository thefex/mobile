﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.ViewModels;
using Android.Support.V4.View;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using SearchView = Android.Support.V7.Widget.SearchView;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class ProjectListFragment : Fragment,
        Toolbar.IOnMenuItemClickListener,
        TabLayout.IOnTabSelectedListener,
        SearchView.IOnQueryTextListener
    {
        private static readonly string WorkspaceIdArgument = "workspace_id_param";
        private static readonly int ProjectCreatedRequestCode = 1;

        private RecyclerView recyclerView;
        private TabLayout tabLayout;
        private Toolbar toolBar;
        private FloatingActionButton newProjectFab;
        private LinearLayout emptyStateLayout;
        private ProjectListVM viewModel;
        private Binding<ObservableRangeCollection<ProjectListVM.CommonProjectData>,
                ObservableRangeCollection<ProjectListVM.CommonProjectData>> topProjectBinding;


        private Guid WorkspaceId
        {
            get
            {
                Guid id;
                Guid.TryParse(Arguments.GetString(WorkspaceIdArgument), out id);
                return id;
            }
        }

        public ProjectListFragment()
        {
        }

        public ProjectListFragment(IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base(jref, xfer)
        {
        }

        public static ProjectListFragment NewInstance(string workspaceId)
        {
            var fragment = new ProjectListFragment();

            var args = new Bundle();
            args.PutString(WorkspaceIdArgument, workspaceId);
            fragment.Arguments = args;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.ProjectListFragment, container, false);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.ProjectListRecyclerView);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, DividerItemDecoration.VerticalList));

            emptyStateLayout = view.FindViewById<LinearLayout> (Resource.Id.ProjectListEmptyState);
            tabLayout = view.FindViewById<TabLayout> (Resource.Id.WorkspaceTabLayout);
            newProjectFab = view.FindViewById<AddProjectFab> (Resource.Id.AddNewProjectFAB);
            toolBar = view.FindViewById<Toolbar> (Resource.Id.ProjectListToolbar);

            var activity = (Activity)Activity;
            activity.SetSupportActionBar(toolBar);
            activity.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            activity.SupportActionBar.SetTitle(Resource.String.ChooseTimeEntryProjectDialogTitle);

            HasOptionsMenu = true;
            newProjectFab.Click += OnNewProjectFabClick;
            tabLayout.SetOnTabSelectedListener(this);

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            viewModel = new ProjectListVM(Phoebe.Reactive.StoreManager.Singleton.AppState, WorkspaceId);

            var adapter = new ProjectListAdapter(recyclerView, viewModel);
            adapter.HandleItemSelection = OnItemSelected;
            recyclerView.SetAdapter(adapter);

            topProjectBinding = this.SetBinding(() => viewModel.TopProjects)
                                .WhenSourceChanges(() => adapter.NotifyItemChanged(0));

            ConfigureUIViews();
            CreateWorkspaceTabs();
        }

        private void ConfigureUIViews()
        {
            // Set toolbar scrollable or not.
            var _params = new AppBarLayout.LayoutParams(toolBar.LayoutParameters);

            if (viewModel.WorkspaceList.Any())
            {
                tabLayout.Visibility = ViewStates.Visible;
                _params.ScrollFlags  = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagEnterAlways;
            }
            else
            {
                tabLayout.Visibility = ViewStates.Gone;
                _params.ScrollFlags = 0;
            }
            toolBar.LayoutParameters = _params;

            // Hide or show recyclerview.
            var haveProjects = viewModel.ProjectList.OfType<ProjectData> ().Any();
            recyclerView.Visibility = haveProjects ? ViewStates.Visible : ViewStates.Gone;
            emptyStateLayout.Visibility = haveProjects ? ViewStates.Gone : ViewStates.Visible;
        }

        private void CreateWorkspaceTabs()
        {
            // Create tabs
            if (viewModel.WorkspaceList.Any())
            {
                foreach (var ws in viewModel.WorkspaceList)
                {
                    var tab = tabLayout.NewTab().SetText(ws.Name);
                    tabLayout.AddTab(tab);
                    if (ws.Id == WorkspaceId)
                    {
                        tab.Select();
                    }
                }
            }
        }

        private void OnNewProjectFabClick(object sender, EventArgs e)
        {
            // Show create project activity instead
            var intent = new Intent(Activity, typeof(NewProjectActivity));
            intent.PutExtra(NewProjectActivity.WorkspaceIdArgument, viewModel.CurrentWorkspaceId.ToString());
            StartActivityForResult(intent, ProjectCreatedRequestCode);
        }

        private void OnItemSelected(ICommonData m)
        {
            // TODO: valorate to work only with IDs.
            Guid projectId = Guid.Empty;
            Guid taskId = Guid.Empty;

            if (m is ProjectData)
            {
                if (!(m is ProjectsCollection.SuperProjectData && ((ProjectsCollection.SuperProjectData)m).IsEmpty))
                {
                    projectId = m.Id;
                }
            }
            else if (m is TaskData)
            {
                var task = (TaskData)m;
                projectId = task.ProjectId;
                taskId = task.Id;
            }
            // Return selected data inside the
            // intent.
            var resultIntent = new Intent();

            resultIntent.PutExtra(BaseActivity.IntentProjectIdArgument, projectId.ToString());
            resultIntent.PutExtra(BaseActivity.IntentTaskIdArgument, taskId.ToString());
            Activity.SetResult(Result.Ok, resultIntent);
            Activity.Finish();
        }

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // Bypass to close the activity
            // if the project is created in NewProject activity,
            // close the Project list activity
            if (requestCode == ProjectCreatedRequestCode)
            {
                if (resultCode == (int)Result.Ok)
                {
                    data.PutExtra(BaseActivity.IntentTaskIdArgument, Guid.Empty.ToString());
                    Activity.SetResult(Result.Ok, data);
                    Activity.Finish();
                }
            }
        }

        public override void OnDestroyView()
        {
            viewModel.Dispose();
            base.OnDestroyView();
        }

        #region Option menu
        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.ProjectListToolbarMenu, menu);
            var searchMenuItem = menu.FindItem(Resource.Id.projectSearch);
            MenuItemCompat.SetOnActionExpandListener(searchMenuItem, new SearchMenuItemExpandListener(viewModel));

            var searchView = Android.Runtime.Extensions.JavaCast<SearchView> (searchMenuItem.ActionView);
            searchView.SetOnQueryTextListener(this);
            toolBar.SetOnMenuItemClickListener(this);
        }

        public bool OnQueryTextChange(string newText)
        {
            viewModel.SearchByProjectName(newText);
            return true;
        }

        public bool OnQueryTextSubmit(string query)
        {
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                Activity.OnBackPressed();
            }
            return base.OnOptionsItemSelected(item);
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.SortByClients:
                    viewModel.ChangeListSorting(ProjectsCollection.SortProjectsBy.Clients);
                    return true;
                case Resource.Id.SortByProjects:
                    viewModel.ChangeListSorting(ProjectsCollection.SortProjectsBy.Projects);
                    return true;
            }
            return false;
        }
        #endregion

        #region Workspace Tablayout
        public void OnTabSelected(TabLayout.Tab tab)
        {
            viewModel.ChangeWorkspaceByIndex(tab.Position);
            ConfigureUIViews();
        }

        public void OnTabReselected(TabLayout.Tab tab)  { }

        public void OnTabUnselected(TabLayout.Tab tab) { }


        #endregion

        class SearchMenuItemExpandListener : Java.Lang.Object, Android.Support.V4.View.MenuItemCompat.IOnActionExpandListener
        {
            private ProjectListVM viewModel;

            public SearchMenuItemExpandListener(ProjectListVM viewModel)
            {
                this.viewModel = viewModel;
            }

            public SearchMenuItemExpandListener(IntPtr ptr, JniHandleOwnership transfer) : base(ptr, transfer)
            {
                
            }

            public bool OnMenuItemActionCollapse(IMenuItem item)
            {
                viewModel.IsSearchActive = false;
                return true;
            }

            public bool OnMenuItemActionExpand(IMenuItem item)
            {
                return true;
            }
        }
    }
}

