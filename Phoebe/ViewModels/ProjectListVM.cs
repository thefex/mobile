using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using XPlatUtils;
using Toggl.Phoebe.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Toggl.Phoebe.ViewModels
{
    public interface IOnProjectSelectedHandler
    {
        void OnProjectSelected(Guid projectId, Guid taskId);
    }

    [ImplementPropertyChanged]
    public class ProjectListVM : ViewModelBase, IDisposable
    {
        private IDisposable searchObservable;

        public ProjectListVM(AppState appState, Guid workspaceId)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
            CurrentWorkspaceId = workspaceId;

            // Try to read sort from settings.
            ProjectsCollectionVM.SortProjectsBy savedSort;
            if (!Enum.TryParse(appState.Settings.ProjectSort, out savedSort))
                savedSort = ProjectsCollectionVM.SortProjectsBy.Clients;

            ProjectList = new ProjectsCollectionVM(appState, savedSort, workspaceId);
            WorkspaceList = appState.Workspaces.Values.OrderBy(r => r.Name).ToList();
            CurrentWorkspaceIndex = WorkspaceList.IndexOf(p => p.Id == workspaceId);

            PopulateMostUsedProjects();
            // Search stream
            searchObservable = Observable.FromEventPattern<string> (ev => onSearch += ev, ev => onSearch -= ev)
                               .Throttle(TimeSpan.FromMilliseconds(300))
                               .DistinctUntilChanged()
                               .ObserveOn(SynchronizationContext.Current)
                               .Subscribe(p => ProjectList.ProjectNameFilter = p.EventArgs,
                                          ex => ServiceContainer.Resolve<ILogger> ().Error("Search", ex, null));
        }

        public void Dispose()
        {
            searchObservable.Dispose();
            ProjectList.Dispose();
        }

        #region Observable properties
        public List<IWorkspaceData> WorkspaceList { get; private set; }
        public ProjectsCollectionVM ProjectList { get; private set; }
        public List<CommonProjectData> AllTopProjects { get; private set; }
        public List<CommonProjectData> TopProjects { get; private set; }
        public int CurrentWorkspaceIndex { get; private set; }
        public Guid CurrentWorkspaceId { get; private set; }
        #endregion

        private event EventHandler<string> onSearch;

        public void SearchByProjectName(string token)
        {
            onSearch.Invoke(this, token);
        }

        public void ChangeListSorting(ProjectsCollectionVM.SortProjectsBy sortBy)
        {
            // TODO RX: TODO TODO TODO: Danger! Mutating a property from a service
            ProjectList.SortBy = sortBy;
            RxChain.Send(new DataMsg.UpdateSetting(nameof(SettingsState.ProjectSort), sortBy.ToString()));
        }

        public void ChangeWorkspaceByIndex(int newIndex)
        {
            CurrentWorkspaceId = WorkspaceList [newIndex].Id;
            ProjectList.WorkspaceId = WorkspaceList [newIndex].Id;
            CurrentWorkspaceIndex = newIndex;
            TopProjects = ProjectList.Count > 7
                          ? AllTopProjects.Where(r => r.WorkspaceId == CurrentWorkspaceId).Take(3).ToList()
                          : new List<CommonProjectData>();
        }

        public void PopulateMostUsedProjects() //Load all potential top projects at once.
        {
            var store = ServiceContainer.Resolve<ISyncDataStore>();
            //var settingsStore = ServiceContainer.Resolve<SettingsStor>();
            var appstate = StoreManager.Singleton.AppState;

            var recentEntries = store.Table<TimeEntryData>()
                                .OrderByDescending(r => r.StartTime)
                                .Where(r => r.DeletedAt == null
                                       && r.UserId == appstate.User.Id
                                       && r.ProjectId != Guid.Empty)
                                .ToList();

            var uniqueRows = recentEntries
            .GroupBy(p => new { p.ProjectId, p.TaskId })
            .Select(g => g.First())
            .ToList();

            AllTopProjects = new List<CommonProjectData>();
            foreach (var entry in uniqueRows)
            {
                var project = appstate.Projects.Values.Where(p => p.Id == entry.ProjectId).FirstOrDefault();
                var task = appstate.Tasks.Values.Where(p => p.Id == entry.TaskId).FirstOrDefault();
                var client = project.ClientId == Guid.Empty ? string.Empty : appstate.Clients.Values.Where(c => c.Id == project.ClientId).First().Name;
                AllTopProjects.Add(new CommonProjectData(project, client, task ?? null));
            }
            TopProjects = AllTopProjects.Where(r => r.WorkspaceId == CurrentWorkspaceId).Take(3).ToList();
        }

        public class CommonProjectData : ProjectData
        {
            public string ClientName { get; private set; }
            public ITaskData Task { get; private set; }

            public CommonProjectData(IProjectData dataObject, string clientName, ITaskData task = null) : base((ProjectData)dataObject)
            {
                Task = task;
                ClientName = clientName;
            }
        }
    }
}
