using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    [ImplementPropertyChanged]
    public class NewProjectVM : ViewModelBase, IDisposable
    {
        private IProjectData model;
        private readonly AppState appState;
        private readonly IWorkspaceData workspace;
        private IClientData clientData;

        public NewProjectVM(AppState appState, Guid workspaceId)
        {
            this.appState = appState;
            this.workspace = appState.Workspaces[workspaceId];
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Project";
        }

        public void Dispose()
        {
        }

        public string ClientName { get; private set; }

        public void SetClient(IClientData clientData)
        {
            this.clientData = clientData;
            ClientName = clientData.Name;
        }

        public Task<IProjectData> SaveProjectAsync(string projectName, int projectColor, RxChain.Continuation continuationOptions = null)
        {
            var tcs = new TaskCompletionSource<IProjectData> ();
            model = ProjectData.Create(x =>
            {
                x.Name = projectName;
                x.Color = projectColor;
                x.WorkspaceId = workspace.Id;
                x.WorkspaceRemoteId = workspace.RemoteId.HasValue ? workspace.RemoteId.Value : 0;
                x.IsActive = true;
                x.IsPrivate = true;
                x.ClientId = clientData != null ? clientData.Id : Guid.Empty;
                x.ClientRemoteId = clientData != null ? clientData.RemoteId : null;
            });
            // ATTENTION  ProjectUserData is not used
            // because no admin features are implemented.
            // Just save the project and wait for the state update.
            RxChain.Send(new DataMsg.ProjectDataPut(model), new RxChain.Continuation((state) =>
            {
                var projectData = state.Projects.Values.First(x => x.Name == model.Name && x.ClientId == model.ClientId);
                tcs.SetResult(projectData);
            }));
            return tcs.Task;
        }

        public bool ExistProjectWithName(string projectName)
        {
            Guid clientId = clientData != null ? clientData.Id : Guid.Empty;
            return appState.Projects.Values.Any(r => r.Name == projectName && r.ClientId == clientId);
        }

        public bool ContainsClients(Guid workspaceId)
        {
            return appState.Clients.Values.Any(r => r.DeletedAt == null && r.WorkspaceId == workspaceId);
        }
    }
}
