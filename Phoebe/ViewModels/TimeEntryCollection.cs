using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Data.Diff;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels.Timer;
using XPlatUtils;
using System.Threading;

namespace Toggl.Phoebe.ViewModels
{
    public class TimeEntryCollection : ObservableRangeCollection<IHolder>, IDisposable
    {
        #region Nested classes
        class InnerState
        {
            public IList<IHolder> Holders { get; }
            public IList<DiffSection<IHolder>> Diffs { get; }

            public InnerState(IList<IHolder> holders = null, IList<DiffSection<IHolder>> diffs = null)
            {
                Holders = holders ?? new List<IHolder> ();
                Diffs = diffs ?? new List<DiffSection<IHolder>> ();
            }
        }
        #endregion

        IDisposable disposable;
        readonly TimeEntryGrouper grouper;

        public TimeEntryCollection(TimeEntryGroupMethod groupMethod, SynchronizationContext uiContext)
        {
            grouper = new TimeEntryGrouper(groupMethod);
            disposable = StoreManager
                         .Singleton
                         .Observe(x => x.State.TimeEntries)
                         .DistinctUntilChanged()
                         .ObserveOn(uiContext)
                         .Select(x => x.Values)
                         .Scan(new InnerState(), GetDiffsFromNewValues)
                         .Subscribe(state => UpdateCollection(state.Diffs));
        }

        public void Dispose()
        {
            if (disposable != null)
            {
                disposable.Dispose();
                disposable = null;
            }
        }

        private InnerState GetDiffsFromNewValues(InnerState state, IEnumerable<RichTimeEntry> entries)
        {
            try
            {
                var timeHolders = entries.Select(x => new TimeEntryHolder(x)).ToList();

                // Create the new item collection from holders (sort and add headers...)
                var newItemCollection = CreateItemCollection(timeHolders);

                // Check diffs, modify ItemCollection and notify changes
                var diffs = Diff.Calculate(state.Holders, newItemCollection);

                // Swap remove events to delete normal items before headers.
                // iOS requierement.
                diffs = Diff.SortRemoveEvents<IHolder, DateHolder> (diffs);

                return new InnerState(newItemCollection, diffs);

            }
            catch (Exception ex)
            {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error(GetType().Name, ex, "Failed to update collection");
                return new InnerState(state.Holders);
            }
        }

        private void UpdateCollection(IList<DiffSection<IHolder>> diffs)
        {
            foreach (var diff in diffs)
            {
                switch (diff.Type)
                {
                    case DiffType.Add:
                        Insert(diff.NewIndex, diff.NewItem);
                        break;
                    case DiffType.Remove:
                        RemoveAt(diff.NewIndex);
                        break;
                    case DiffType.Replace:
                        this[diff.NewIndex] = diff.NewItem;
                        break;
                    case DiffType.Move:
                        Move(diff.OldIndex, diff.NewIndex, diff.NewItem);
                        break;
                }
            }
        }

        private List<IHolder> CreateItemCollection(IEnumerable<TimeEntryHolder> timeHolders)
        {
            return grouper.Group(timeHolders)
                   .OrderByDescending(x => x.GetStartTime())
                   .GroupBy(x => x.GetStartTime().ToLocalTime().Date)
                   .SelectMany(gr => gr.Cast<IHolder>().Prepend(new DateHolder(gr.Key, gr.Cast<ITimeEntryHolder> ())))
                   .ToList();
        }
    }
}
