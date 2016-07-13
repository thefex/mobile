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

namespace Toggl.Phoebe.ViewModels
{
    public interface IOnProjectSelectedHandler
    {
        void OnProjectSelected(Guid projectId, Guid taskId);
    }

    [ImplementPropertyChanged]
    public class ProjectListVM : ViewModelBase, IDisposable
    {
        const int maxTopProjects = 3;
        private IDisposable searchObservable;
        private readonly List<CommonProjectData> allTopProjects;

        public ProjectListVM(AppState appState, Guid workspaceId)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
            CurrentWorkspaceId = workspaceId;

            // Try to read sort from settings.
            ProjectsCollection.SortProjectsBy savedSort;
            if (!Enum.TryParse(appState.Settings.ProjectSort, out savedSort))
                savedSort = ProjectsCollection.SortProjectsBy.Clients;

            ProjectList = new ProjectsCollection(appState, savedSort, workspaceId);
            WorkspaceList = appState.Workspaces.Values.OrderBy(r => r.Name).ToList();
            CurrentWorkspaceIndex = WorkspaceList.IndexOf(p => p.Id == workspaceId);
            allTopProjects = GetMostUsedProjects(appState);
            TopProjects = GetTopProjectsByWorkspace(workspaceId);

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
        public ProjectsCollection ProjectList { get; private set; }
        public List<CommonProjectData> TopProjects { get; private set; }
        public int CurrentWorkspaceIndex { get; private set; }
        public Guid CurrentWorkspaceId { get; private set; }
        #endregion

        private event EventHandler<string> onSearch;

        public void SearchByProjectName(string token)
        {
            onSearch.Invoke(this, token);
        }

        public void ChangeListSorting(ProjectsCollection.SortProjectsBy sortBy)
        {
            // TODO: Danger! Mutating a property from a service
            ProjectList.SortBy = sortBy;
            RxChain.Send(new DataMsg.UpdateSetting(nameof(SettingsState.ProjectSort), sortBy.ToString()));
        }

        public void ChangeWorkspaceByIndex(int newIndex)
        {
            CurrentWorkspaceId = WorkspaceList [newIndex].Id;
            ProjectList.WorkspaceId = WorkspaceList [newIndex].Id;
            CurrentWorkspaceIndex = newIndex;
            TopProjects = GetTopProjectsByWorkspace(CurrentWorkspaceId);
        }

        private List<CommonProjectData> GetTopProjectsByWorkspace(Guid workspacedId)
        {
            return ProjectList.Count > 7
                   ? allTopProjects.Where(r => r.WorkspaceId == CurrentWorkspaceId).Take(maxTopProjects).ToList()
                   : new List<CommonProjectData>();
        }

        private List<CommonProjectData> GetMostUsedProjects(AppState appstate) //Load all potential top projects at once.
        {
            IProjectData project;
            ITaskData task;
            string client;
            var topProjects = new List<CommonProjectData>();
            var store = ServiceContainer.Resolve<ISyncDataStore>();

            store.Table<TimeEntryData>()
            .OrderByDescending(r => r.StartTime)
            .Where(r => r.DeletedAt == null && r.ProjectId != Guid.Empty)
            .GroupBy(p => new { p.ProjectId, p.TaskId })
            .Select(g => g.First())
            .ForEach(entry =>
            {
                project = appstate.Projects.Values.FirstOrDefault(p => p.Id == entry.ProjectId);
                task = appstate.Tasks.Values.FirstOrDefault(p => p.Id == entry.TaskId);
                client = project.ClientId == Guid.Empty ? string.Empty : appstate.Clients.Values.First(c => c.Id == project.ClientId).Name;
                topProjects.Add(new CommonProjectData(project, client, task ?? null));
            });

            return topProjects;
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
