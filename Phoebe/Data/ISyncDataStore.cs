using System;
using System.Collections.Generic;
using SQLite.Net;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data
{
    public interface ISyncDataStore : IDisposable
    {
        TableQuery<T> Table<T> () where T : CommonData, new();
        IReadOnlyList<ICommonData> Update(Action<ISyncDataStoreContext> worker);
        void WipeTables();

        int GetVersion();
        int GetQueueSize(string queueId);
        bool TryEnqueue(string queueId, string json);
        bool TryDequeue(string queueId, out string json);
        bool TryPeekQueue(string queueId, out string json);
        int ResetQueue(string queueId);
    }

    public interface ISyncDataStoreContext
    {
        void Put(ICommonData obj);
        void Delete(ICommonData obj);
        ICommonData GetByColumn(Type type, string colName, object colValue);
        IReadOnlyList<ICommonData> UpdatedItems { get; }
        SQLiteConnection Connection { get; }
    }
}

