using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Foundation;
using UIKit;

namespace Toggl.Ross.DataSources
{
    public abstract class PlainObservableCollectionViewSource<TData> : UITableViewSource
    {
        protected readonly UITableView tableView;
        protected readonly ObservableCollection<TData> collection;
        protected UITableView TableView  { get { return tableView; }}

        public event EventHandler CollectionChanged;

        protected PlainObservableCollectionViewSource(UITableView tableView, ObservableCollection<TData> collection)
        {
            this.tableView = tableView;
            this.collection = collection;
            collection.CollectionChanged += OnCollectionChange;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                collection.CollectionChanged -= OnCollectionChange;
            }
            base.Dispose(disposing);
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return collection.Count;
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        private void OnCollectionChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            var collectionData = (ObservableCollection<TData>) sender;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                TableView.ReloadData();
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                TableView.InsertRows(new [] {NSIndexPath.FromRowSection(e.NewStartingIndex, 0)}, UITableViewRowAnimation.Automatic);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                TableView.DeleteRows(new [] {NSIndexPath.FromRowSection(e.OldStartingIndex, 0)}, UITableViewRowAnimation.Automatic);
            }

            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                TableView.ReloadRows(new [] {NSIndexPath.FromRowSection(e.NewStartingIndex, 0)}, UITableViewRowAnimation.None);
            }

            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                var fromIndexPath = NSIndexPath.FromRowSection(e.OldStartingIndex, 0);
                var toIndexPath = NSIndexPath.FromRowSection(e.NewStartingIndex, 0);
                TableView.MoveRow(fromIndexPath, toIndexPath);
            }

            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public UIView EmptyView { get; set; }

    }
}
