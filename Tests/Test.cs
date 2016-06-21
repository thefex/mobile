using System.IO;
using System.Threading;
using NUnit.Framework;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Tests
{
    public abstract class Test
    {
        protected string databasePath;
        protected readonly string databaseDir = Path.GetDirectoryName(Path.GetTempFileName());

        [OneTimeSetUp]
        public virtual void Init()
        {
            databasePath = DatabaseHelper.GetDatabasePath(databaseDir, SyncSqliteDataStore.DB_VERSION);
            ServiceContainer.Register<ISyncDataStore> (
                new SyncSqliteDataStore(databasePath, new SQLitePlatformGeneric()));

            ServiceContainer.Register<MessageBus>(new MessageBus());
            ServiceContainer.Register<ITimeProvider> (new DefaultTimeProvider());
            ServiceContainer.Register<TimeCorrectionManager> (new TimeCorrectionManager());
            ServiceContainer.Register<LogStore> ((LogStore)null);
            ServiceContainer.Register<ILoggerClient> ((ILoggerClient)null);
            ServiceContainer.Register<ILogger> (new VoidLogger());
            ServiceContainer.Register<INetworkPresence> (new Reactive.NetWorkPresenceMock());
        }

        [OneTimeTearDown]
        public virtual void Cleanup()
        {
            ServiceContainer.Clear();
            if (databasePath != null)
            {
                DatabaseHelper.DeleteDatabase(databasePath);
                databasePath = null;
            }
        }

        [SetUp]
        public virtual void SetUp()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
        }
    }
}
