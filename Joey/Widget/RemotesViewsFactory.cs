using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;


namespace Toggl.Joey.Widget
{
    [Service(Exported = false, Permission = Android.Manifest.Permission.BindRemoteviews)]
    public class RemotesViewsFactoryService : RemoteViewsService
    {
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new RemotesViewsFactory(ApplicationContext);
        }
    }

    public class RemotesViewsFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
    {
        private Context context;
        private IEnumerable<RichTimeEntry> entries;

        public RemotesViewsFactory(Context ctx)
        {
            context = ctx;
        }

        public long GetItemId(int position)
        {
            return position;
        }

        public void OnCreate()
        {
        }

        public void OnDataSetChanged()
        {
            entries = StoreManager.Singleton.AppState.TimeEntries.Values
                      .Where(d => d.Data.State != TimeEntryState.Running && d.Data.DeletedAt == null)
                      .OrderByDescending(d => d.Data.StartTime)
                      .Take(3);
        }

        public void OnDestroy()
        {
        }

        public RemoteViews GetViewAt(int position)
        {
            var remoteView = new RemoteViews(context.PackageName, Resource.Layout.widget_list_item);
            var rowData = entries.ElementAt(position);
            var isRunning = rowData.Data.State == TimeEntryState.Running;

            // set if is running
            if (isRunning)
            {
                remoteView.SetImageViewResource(Resource.Id.WidgetContinueImageButton, Resource.Drawable.IcWidgetStop);
            }
            else
            {
                remoteView.SetImageViewResource(Resource.Id.WidgetContinueImageButton, Resource.Drawable.IcWidgetPlay);
            }

            // set color
            var color = ProjectData.HexColors[rowData.Info.ProjectData.Color];
            remoteView.SetInt(Resource.Id.WidgetColorView, "setColorFilter", Color.ParseColor(color));
            remoteView.SetOnClickFillInIntent(Resource.Id.WidgetContinueImageButton, GetFillIntent(isRunning: isRunning, id: rowData.Data.Id));

            // set content
            remoteView.SetTextViewText(
                Resource.Id.DescriptionTextView,
                string.IsNullOrWhiteSpace(rowData.Data.Description) ?
                context.Resources.GetString(Resource.String.RunningWidgetNoDescription) :
                rowData.Data.Description);
            remoteView.SetTextViewText(
                Resource.Id.ProjectTextView,
                string.IsNullOrWhiteSpace(rowData.Info.ProjectData.Name) ?
                context.Resources.GetString(Resource.String.RunningWidgetNoProject) :
                rowData.Info.ProjectData.Name);

            var duration = string.Format("{0:D2}:{1:mm}:{1:ss}",
                                         (int)rowData.Data.GetDuration().TotalHours, rowData.Data.GetDuration());
            remoteView.SetTextViewText(Resource.Id.DurationTextView, duration);

            return remoteView;
        }

        private Intent GetFillIntent(bool isRunning, Guid id)
        {
            var intent = new Intent();
            intent.PutExtra(WidgetProvider.TimeEntryIdParameter, id.ToString());

            if (isRunning)
            {
                intent.SetAction(WidgetProvider.StopAction);
            }
            else
            {
                intent.SetAction(WidgetProvider.ContinueAction);
            }

            return intent;
        }

        public int Count
        {
            get
            {
                return entries.Count();
            }
        }

        public bool HasStableIds
        {
            get
            {
                return true;
            }
        }

        public RemoteViews LoadingView
        {
            get
            {
                // TODO: return an spinner?
                return null;
            }
        }

        public int ViewTypeCount
        {
            get
            {
                return 1;
            }
        }
    }
}