using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using XPlatUtils;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe.ViewModels
{
    public interface IOnTagSelectedHandler
    {
        void OnCreateNewTag(string newTagData);

        void OnModifyTagList(List<string> newTagList);
    }

    public class TagListVM : IDisposable
    {
        // This viewMode is apparently simple but
        // it needs the code related with the update of
        // the list. (subscription and reload of data
        // everytime a tag is changed/created/deleted

        public TagListVM(AppState appState, Guid workspaceId)
        {
            TagCollection = LoadTags(appState, workspaceId);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Tags";
        }

        public void Dispose()
        {
            TagCollection = null;
        }

        public ObservableRangeCollection<ITagData> TagCollection { get; set; }

        private ObservableRangeCollection<ITagData> LoadTags(AppState appState, Guid workspaceId)
        {
            var tagCollection = new ObservableRangeCollection<ITagData> ();
            var workspaceTags = appState.Tags.Values
                                .Where(r => r.DeletedAt == null && r.WorkspaceId == workspaceId)
                                .OrderBy(tag => tag.Name);
            tagCollection.AddRange(workspaceTags);
            return tagCollection;
        }
    }
}
