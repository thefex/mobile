using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data.Views
{
    public interface ICollectionDataView<T> : INotifyCollectionChanged, IDisposable
    {
        IEnumerable<T> Data { get; }

        int Count { get; }

        bool HasMore { get; }

        Task LoadMore (bool isInit = false);
    }
}