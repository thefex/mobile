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
        private readonly IEnumerable<CommonProjectData> allTopProjects;

        private IEnumerable<CommonProjectData> cachedTopProjects = Enumerable.Empty<CommonProjectData>();

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
            TopProjects = new ObservableRangeCollection<CommonProjectData>(GetTopProjectsByWorkspace(workspaceId));

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
        private bool isSearchActive;
        private ObservableRangeCollection<CommonProjectData> topProjects;


        public List<IWorkspaceData> WorkspaceList { get; private set; }
        public ProjectsCollection ProjectList { get; private set; }

        public ObservableRangeCollection<CommonProjectData> TopProjects
        {
            get { return topProjects; }
            private set
            {
                topProjects = value;
                cachedTopProjects = topProjects.ToList();
            }
        }

        public int CurrentWorkspaceIndex { get; private set; }
        public Guid CurrentWorkspaceId { get; private set; }
        #endregion

        private event EventHandler<string> onSearch;


        public bool IsSearchActive
        {
            get { return isSearchActive; }
            set
            {
                if (isSearchActive == value)
                    return;
                isSearchActive = value;

                if (isSearchActive)
                    TopProjects.Clear();
                else if (!TopProjects.Any())
                    TopProjects.AddRange(cachedTopProjects);
            }
        }

        public void SearchByProjectName(string token)
        {
            IsSearchActive = !string.IsNullOrEmpty(token);
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
            TopProjects = new ObservableRangeCollection<CommonProjectData>(GetTopProjectsByWorkspace(CurrentWorkspaceId));
        }

        private IEnumerable<CommonProjectData> GetTopProjectsByWorkspace(Guid workspacedId)
        {
            return ProjectList.Count > 7
                   ? allTopProjects.Where(r => r.WorkspaceId == CurrentWorkspaceId).Take(maxTopProjects).ToList()
                   : new List<CommonProjectData>();
        }

        private IEnumerable<CommonProjectData> GetMostUsedProjects(AppState appstate) //Load all potential top projects at once.
        {
            IProjectData project;
            ITaskData task;
            string client;
            var topProjects = new List<CommonProjectData>();

            try
            {
                // TODO: find a better sort method.
                var pool = new Dictionary<Tuple<Guid, Guid>, int>();
                var items = appstate.TimeEntries.Values
                            .Where(x => x.Data.DeletedAt == null && x.Data.ProjectId != Guid.Empty)
                            .OrderBy(x => x.Data.ProjectId)
                            .Select(x => new Tuple<Guid, Guid> (x.Data.ProjectId, x.Data.TaskId));
                items.ForEach(item =>
                {
                    if (!pool.ContainsKey(item))
                        pool.Add(item, 1);
                    else
                        pool[item]++;
                });

                var orderedPool = pool.OrderByDescending(x => x.Value);

                foreach (var item in orderedPool)
                {
                    project = appstate.Projects.Values.FirstOrDefault(p => p.Id == item.Key.Item1);
                    task = appstate.Tasks.Values.FirstOrDefault(p => p.Id == item.Key.Item2);
                    client = project.ClientId == Guid.Empty ? string.Empty : appstate.Clients.Values.First(c => c.Id == project.ClientId).Name;
                    topProjects.Add(new CommonProjectData(project, client, task ?? null));
                }
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.Resolve<ILogger>();
                logger.Error(nameof(ProjectListVM), ex, "Error getting most used projects");
            }

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
