using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Misc;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Reactive
{
    public interface IReducer
    {
        DataSyncMsg<object> Reduce(object state, DataMsg msg);
    }

    public class Reducer<T> : IReducer
    {
        readonly Func<T, DataMsg, DataSyncMsg<T>> reducer;

        public virtual DataSyncMsg<T> Reduce(T state, DataMsg msg)
        {
            return reducer(state, msg);
        }

        DataSyncMsg<object> IReducer.Reduce(object state, DataMsg msg)
        {
            return Reduce((T)state, msg).Cast<object> ();
        }

        protected Reducer() { }

        public Reducer(Func<T, DataMsg, DataSyncMsg<T>> reducer)
        {
            this.reducer = reducer;
        }
    }

    public class TagCompositeReducer<T> : Reducer<T>, IReducer
    {
        readonly Dictionary<Type, Reducer<T>> reducers = new Dictionary<Type, Reducer<T>> ();

        public TagCompositeReducer<T> Add(Type msgType, Func<T, DataMsg, DataSyncMsg<T>> reducer)
        {
            return Add(msgType, new Reducer<T> (reducer));
        }

        public TagCompositeReducer<T> Add(Type msgType, Reducer<T> reducer)
        {
            reducers.Add(msgType, reducer);
            return this;
        }

        public override DataSyncMsg<T> Reduce(T state, DataMsg msg)
        {
            Reducer<T> reducer;
            if (reducers.TryGetValue(msg.GetType(), out reducer))
            {
                return reducer.Reduce(state, msg);
            }
            else
            {
                return DataSyncMsg.Create(state);
            }
        }

        DataSyncMsg<object> IReducer.Reduce(object state, DataMsg msg)
        {
            return Reduce((T)state, msg).Cast<object> ();
        }
    }

    public static class Reducers
    {
        public static Reducer<AppState> Init()
        {
            return new TagCompositeReducer<AppState>()
                   .Add(typeof(DataMsg.ServerRequest), ServerRequest)
                   .Add(typeof(DataMsg.ServerResponse), ServerResponse)
                   .Add(typeof(DataMsg.TimeEntriesLoad), TimeEntriesLoad)
                   .Add(typeof(DataMsg.TimeEntryPut), TimeEntryPut)
                   .Add(typeof(DataMsg.TimeEntriesRemove), TimeEntryRemove)
                   .Add(typeof(DataMsg.TimeEntryContinue), TimeEntryContinue)
                   .Add(typeof(DataMsg.TimeEntryStart), TimeEntryStart)
                   .Add(typeof(DataMsg.TimeEntryStop), TimeEntryStop)
                   .Add(typeof(DataMsg.TagsPut), TagsPut)
                   .Add(typeof(DataMsg.ClientDataPut), ClientDataPut)
                   .Add(typeof(DataMsg.ProjectDataPut), ProjectDataPut)
                   .Add(typeof(DataMsg.UserDataPut), UserDataPut)
                   .Add(typeof(DataMsg.ResetState), Reset)
                   .Add(typeof(DataMsg.InitStateAfterMigration), InitStateAfterMigration)
                   .Add(typeof(DataMsg.UpdateSetting), UpdateSettings)
                   .Add(typeof(DataMsg.RegisterPush), RegisterPush)
                   .Add(typeof(DataMsg.UnregisterPush), UnregisterPush);
        }

        static DataSyncMsg<AppState> RegisterPush(AppState state, DataMsg msg)
        {
            var pushMsg = msg as DataMsg.RegisterPush;
            var pushToken = state.Settings.PushToken;

            if (string.IsNullOrEmpty(pushToken))
            {
                pushToken = pushMsg.DeviceToken;
                // Try to register PushToken in server silently
                // Maybe is better to include the process inside
                // SyncManager
                var pushClient = ServiceContainer.Resolve<IPushClient>();
                IgnoreTaskErrors(
                    pushClient.Register(state.User.ApiToken, pushToken),
                    "Failed to register push token to server.");
            }

            return DataSyncMsg.Create(state.With(settings: state.Settings.With(pushToken: pushToken)));
        }

        static DataSyncMsg<AppState> UnregisterPush(AppState state, DataMsg msg)
        {
            if (!string.IsNullOrEmpty(state.User.ApiToken) &&
                    !string.IsNullOrEmpty(state.Settings.PushToken))
            {
                var pushClient = ServiceContainer.Resolve<IPushClient>();
                IgnoreTaskErrors(
                    pushClient.Unregister(state.User.ApiToken, state.Settings.PushToken),
                    "Failed to unregister push token to server.");
            }

            return DataSyncMsg.Create(state.With(settings: state.Settings.With(pushToken: string.Empty)));
        }

        static DataSyncMsg<AppState> ServerRequest(AppState state, DataMsg msg)
        {
            var req = (msg as DataMsg.ServerRequest).Data;
            RequestInfo reqInfo;

            // ATTENTION If ApiToken doesn't exist and the request
            // is not related with authentication, user is not connecte
            // just return an empty and anyoying answer :)
            if (!(req is ServerRequest.Authenticate) &&
                    string.IsNullOrEmpty(state.User.ApiToken))
            {
                reqInfo = state.RequestInfo.With(
                              hadErrors: false,
                              errorInfo: null,
                              hasMore: false,
                              running: new List<ServerRequest>());

                return DataSyncMsg.Create(state.With(requestInfo: reqInfo));
            }

            reqInfo = state.RequestInfo.With(
                          hadErrors: false,
                          errorInfo: null,
                          running: state.RequestInfo.Running.Append(req).ToList());

            if (req is ServerRequest.Authenticate)
                reqInfo = reqInfo.With(authResult: AuthResult.None);

            if (req is ServerRequest.GetChanges)
                reqInfo = reqInfo.With(getChangesLastRun: state.Settings.GetChangesLastRun);

            return DataSyncMsg.Create(req, state.With(requestInfo: reqInfo));
        }

        static DataSyncMsg<AppState> TimeEntriesLoad(AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            var endDate = state.RequestInfo.NextDownloadFrom;

            var startDate = GetDatesByDays(dataStore, endDate, Literals.TimeEntryLoadDays);
            var dbEntries = dataStore
                            .Table<TimeEntryData>()
                            .Where(r =>
                                   r.State != TimeEntryState.New &&
                                   r.StartTime >= startDate && r.StartTime < endDate &&
                                   r.DeletedAt == null)
                            .Take(Literals.TimeEntryLoadMaxInit)
                            .OrderByDescending(r => r.StartTime)
                            .ToList();

            ServerRequest req;
            RequestInfo reqInfo;

            // If ApiToken doesn't exist user is not connected
            if (string.IsNullOrEmpty(state.User.ApiToken))
            {
                reqInfo = state.RequestInfo.With(
                              hadErrors: false,
                              errorInfo: null,
                              hasMore: false,
                              running: new List<ServerRequest>());

                return DataSyncMsg.Create(state.With(
                                              requestInfo: reqInfo,
                                              timeEntries: state.UpdateTimeEntries(dbEntries)));
            }
            else
            {
                req = new ServerRequest.DownloadEntries();
                reqInfo = state.RequestInfo.With(
                              running: state.RequestInfo.Running.Append(req).ToList(),
                              downloadFrom: endDate,
                              nextDownloadFrom: dbEntries.Any() ? dbEntries.Min(x => x.StartTime) : endDate);

                return DataSyncMsg.Create(req, state.With(
                                              requestInfo: reqInfo,
                                              timeEntries: state.UpdateTimeEntries(dbEntries)));
            }
        }

        static DataSyncMsg<AppState> ServerResponse(AppState state, DataMsg msg)
        {
            var serverMsg = msg as DataMsg.ServerResponse;
            return serverMsg.Data.Match(
                       receivedData => serverMsg.Request.MatchType(
                           (ServerRequest.CRUD _) =>
            {
                var reqInfo = state.RequestInfo.With(
                                  errorInfo: null,
                                  running: new List<ServerRequest>(),
                                  hadErrors: false);
                state = UpdateStateWithNewData(state, receivedData);
                return DataSyncMsg.Create(state.With(requestInfo: reqInfo));
            },
            (ServerRequest.DownloadEntries req) =>
            {
                state = UpdateStateWithNewData(state, receivedData);
                var reqInfo = state.RequestInfo.With(
                                  errorInfo: null,
                                  running: new List<ServerRequest>(),
                                  hasMore: receivedData.OfType<TimeEntryData>().Any(),
                                  hadErrors: false);
                return DataSyncMsg.Create(state.With(requestInfo: reqInfo));
            },
            (ServerRequest.GetChanges req) =>
            {
                state = UpdateStateWithNewData(state, receivedData);

                // Update user
                var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
                UserData user = serverMsg.User;
                user.Id = state.User.Id;
                user.DefaultWorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == user.DefaultWorkspaceRemoteId).Id;
                // TODO: OBM data that comes in user object from this changes
                // is totally wrong. In that way, we should keep this info before
                // before process the object.
                user.ExperimentIncluded = state.User.ExperimentIncluded;
                user.ExperimentNumber = state.User.ExperimentNumber;

                var userUpdated = (UserData)dataStore.Update(ctx => ctx.Put(user)).Single();

                var reqInfo = state.RequestInfo.With(
                                  errorInfo: null,
                                  hadErrors: false,
                                  running: new List<ServerRequest>(),
                                  getChangesLastRun: serverMsg.Timestamp);

                return DataSyncMsg.Create(state.With(
                                              user: userUpdated,
                                              requestInfo: reqInfo,
                                              settings: state.Settings.With(getChangesLastRun: serverMsg.Timestamp)));
            },
            (ServerRequest.GetCurrentState req) =>
            {
                state = UpdateStateWithNewData(state, receivedData);

                // Update user
                var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
                UserData user = serverMsg.User;
                user.Id = state.User.Id;
                user.DefaultWorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == user.DefaultWorkspaceRemoteId).Id;
                // TODO: OBM data that comes in user object from this changes
                // is totally wrong. In that way, we should keep this info before
                // before process the object.
                user.ExperimentIncluded = state.User.ExperimentIncluded;
                user.ExperimentNumber = state.User.ExperimentNumber;



                var userUpdated = (UserData)dataStore.Update(ctx => ctx.Put(user)).Single();
                if (HasAnyData())
                {
                    state = MergeOfflineDb(state, user.DefaultWorkspaceId, user.DefaultWorkspaceRemoteId, user.Id, user.RemoteId);
                    RxChain.Send(new ServerRequest.UploadData());
                }

                var reqInfo = state.RequestInfo.With(
                                  errorInfo: null,
                                  hadErrors: false,
                                  running: new List<ServerRequest>(),// .Append(request).ToList(),
                                  getChangesLastRun: serverMsg.Timestamp);

                return DataSyncMsg.Create(state.With(
                                              user: userUpdated,
                                              requestInfo: reqInfo,
                                              settings: state.Settings.With(getChangesLastRun: serverMsg.Timestamp)));

            },
            (ServerRequest.Authenticate _) =>
            {
                // TODO RX: Right now, Authenticate responses send UserDataPut messages
                throw new NotImplementedException();
            },
            (ServerRequest.UploadData req) =>
            {
                throw new NotImplementedException();
            }),

            ex =>
            {
                // TODO Rx Clean running array?

                var errorInfo = state.RequestInfo.ErrorInfo;
                if (ex is DataMsg.ServerResponse.TimeConstrainsException)
                {
                    var exc = (DataMsg.ServerResponse.TimeConstrainsException)ex;
                    errorInfo = new Tuple<string, Guid>(exc.ReadableMsg, exc.CommonDataId);
                }
                else if (!(ex is TaskCanceledException))
                {
                    errorInfo = new Tuple<string, Guid>(ex.Message, Guid.Empty);
                }

                var reqInfo = state.RequestInfo.With(
                                  errorInfo: errorInfo,
                                  running: new List<ServerRequest>(),
                                  hadErrors: true);
                return DataSyncMsg.Create(state.With(requestInfo: reqInfo));
            });
        }

        static DataSyncMsg<AppState> TimeEntryPut(AppState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            // TODO Rx Poor use of "DataMsg.TimeEntryPut" and the plain TagNames property?
            var tagList = (msg as DataMsg.TimeEntryPut).TagNames;

            var updated = dataStore.Update(ctx =>
            {
                // ATTENTION Create tags in a
                // different workspace if they don't exists.
                if (tagList.Any())
                {
                    var existingTags = state.Tags.Values.Where(x => x.WorkspaceId == entryData.WorkspaceId);
                    foreach (var item in tagList)
                    {
                        if (!existingTags.Any(x => x.Name == item))
                        {
                            var newTag = TagData.Create(x =>
                            {
                                x.Name = item;
                                x.WorkspaceId = entryData.WorkspaceId;
                                x.WorkspaceRemoteId = entryData.WorkspaceRemoteId;
                            });
                            ctx.Put(newTag);
                        }
                    }
                }

                // ATTENTION If the AppState doesn't contains the entry
                // with this Id yet, be sure we create a fresh entry
                // with state = CreatePending.
                var existing = ctx.Connection.Table<TimeEntryData>().FirstOrDefault(x => x.Id == entryData.Id);
                if (existing == null && entryData.SyncState == SyncState.UpdatePending)
                    entryData = TimeEntryData.Create(null, entryData);

                // TODO: Entry sanity check
                ctx.Put(entryData);
            });
            return DataSyncMsg.Create(updated, state.With(timeEntries: state.UpdateTimeEntries(updated),
                                      tags: state.Update(state.Tags, updated),
                                      settings: state.Settings.With(showWelcome: false)));
        }

        static DataSyncMsg<AppState> TimeEntryRemove(AppState state, DataMsg msg)
        {
            // The TEs should have been already removed from AppState but try to remove them again just in case
            var entriesData = (msg as DataMsg.TimeEntriesRemove).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update(ctx =>
            {
                foreach (var entryData in entriesData)
                {
                    ctx.Delete(entryData.With(x => x.DeletedAt = Time.UtcNow));
                }
            });

            return DataSyncMsg.Create(updated, state.With(timeEntries: state.UpdateTimeEntries(updated)));
        }

        static DataSyncMsg<AppState> TagsPut(AppState state, DataMsg msg)
        {
            var tags = (msg as DataMsg.TagsPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update(ctx =>
            {
                foreach (var tag in tags)
                {
                    ctx.Put(tag);
                }
            });

            return DataSyncMsg.Create(updated, state.With(tags: state.Update(state.Tags, updated)));
        }

        static DataSyncMsg<AppState> ClientDataPut(AppState state, DataMsg msg)
        {
            var data = (msg as DataMsg.ClientDataPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update(ctx => ctx.Put(data));

            return DataSyncMsg.Create(updated, state.With(clients: state.Update(state.Clients, updated)));
        }

        static DataSyncMsg<AppState> ProjectDataPut(AppState state, DataMsg msg)
        {
            var data = (msg as DataMsg.ProjectDataPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update(ctx => ctx.Put(data));

            return DataSyncMsg.Create(updated, state.With(projects: state.Update(state.Projects, updated)));
        }

        static DataSyncMsg<AppState> UserDataPut(AppState state, DataMsg msg)
        {
            return (msg as DataMsg.UserDataPut).Data.Match(
                       userData =>
            {
                var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

                var updated = dataStore.Update(ctx => { ctx.Put(userData); });
                var runningState = state.RequestInfo.Running.Where(x => !(x is ServerRequest.Authenticate)).ToList();

                // This will throw an exception if user hasn't been correctly updated
                var userDataInDb = updated.OfType<UserData>().Single();

                // ATTENTION After succesful login, send
                // a request to get data state from server.
                var req = new ServerRequest.GetCurrentState();
                runningState.Add(req);
                return DataSyncMsg.Create(req, state.With(
                                              user: userDataInDb,
                                              requestInfo: state.RequestInfo.With(authResult: AuthResult.Success, running: runningState),
                                              workspaces: state.Update(state.Workspaces, updated),
                                              settings: state.Settings.With(userId: userDataInDb.Id)));
            },
            ex =>
            {
                var runningState = state.RequestInfo.Running.Where(x => !(x is ServerRequest.Authenticate)).ToList();
                return DataSyncMsg.Create(state.With(
                                              user: new UserData(),
                                              requestInfo: state.RequestInfo.With(authResult: ex.AuthResult, running: runningState)));
            });
        }

        static void CheckTimeEntryState(ITimeEntryData entryData, TimeEntryState expected, string action)
        {
            if (entryData.State != expected)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot {0} a time entry ({1}) in {2} state.",
                                  action, entryData.Id, entryData.State));
            }
        }

        static DataSyncMsg<AppState> TimeEntryContinue(AppState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryContinue).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            var isStartedNew = (msg as DataMsg.TimeEntryContinue).StartedByFAB;

            var updated = dataStore.Update(ctx =>
            {
                // Stop ActiveEntry if necessary
                var prev = state.ActiveEntry.Data;
                if (prev.Id != Guid.Empty && prev.State == TimeEntryState.Running)
                {
                    ctx.Put(prev.With(x =>
                    {
                        x.State = TimeEntryState.Finished;
                        x.StopTime = Time.UtcNow;
                    }));
                }

                ITimeEntryData draft = null;
                if (entryData.Id == Guid.Empty)
                {
                    draft = state.GetTimeEntryDraft();
                }
                else
                {
                    CheckTimeEntryState(entryData, TimeEntryState.Finished, "continue");
                    draft = entryData;
                }

                ctx.Put(TimeEntryData.Create(draft: draft, transform: x =>
                {
                    x.RemoteId = null;
                    x.State = TimeEntryState.Running;
                    x.StartTime = Time.UtcNow.Truncate(TimeSpan.TicksPerSecond);
                    x.StopTime = null;
                    x.Tags = x.Tags == null || x.Tags.Count == 0
                             ? (state.Settings.UseDefaultTag ? TimeEntryData.DefaultTags : new List<string>())
                             : x.Tags;
                }));
            });

            return DataSyncMsg.Create(updated, state.With(timeEntries: state.UpdateTimeEntries(updated),
                                      settings: state.Settings.With(showWelcome: false)));
        }

        static DataSyncMsg<AppState> TimeEntryStart(AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            var updated = dataStore.Update(ctx =>
            {
                // Stop ActiveEntry if necessary
                var prev = state.ActiveEntry.Data;
                if (prev.Id != Guid.Empty && prev.State == TimeEntryState.Running)
                {
                    ctx.Put(prev.With(x =>
                    {
                        x.State = TimeEntryState.Finished;
                        x.StopTime = Time.UtcNow;
                    }));
                }

                ITimeEntryData draft = state.GetTimeEntryDraft();
                ctx.Put(TimeEntryData.Create(draft: draft, transform: x =>
                {
                    x.RemoteId = null;
                    x.State = TimeEntryState.Running;
                    x.StartTime = Time.UtcNow.Truncate(TimeSpan.TicksPerSecond);
                    x.StopTime = null;
                    x.Tags = state.Settings.UseDefaultTag ? TimeEntryData.DefaultTags : new List<string>();
                }));
            });

            return DataSyncMsg.Create(updated, state.With(timeEntries: state.UpdateTimeEntries(updated),
                                      settings: state.Settings.With(showWelcome: false)));
        }

        static DataSyncMsg<AppState> TimeEntryStop(AppState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryStop).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            CheckTimeEntryState(entryData, TimeEntryState.Running, "stop");

            var updated = dataStore.Update(ctx => ctx.Put(entryData.With(x =>
            {
                x.State = TimeEntryState.Finished;
                x.StopTime = Time.UtcNow;
            })));
            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create(updated, state.With(timeEntries: state.UpdateTimeEntries(updated)));
        }

        static DataSyncMsg<AppState> Reset(AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            dataStore.WipeTables();

            // Clear platform settings.
            Settings.SerializedSettings = string.Empty;

            // Reset state
            var appState = AppState.Init();

            // TODO: Ping analytics?
            // TODO: Call Log service?

            return DataSyncMsg.Create(appState);
        }

        static DataSyncMsg<AppState> InitStateAfterMigration(AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            var userData = new UserData();
            var projects = new Dictionary<Guid, IProjectData>();
            var projectUsers = new Dictionary<Guid, IProjectUserData>();
            var workspaces = new Dictionary<Guid, IWorkspaceData>();
            var workspaceUserData = new Dictionary<Guid, IWorkspaceUserData>();
            var clients = new Dictionary<Guid, IClientData>();
            var tasks = new Dictionary<Guid, ITaskData>();
            var tags = new Dictionary<Guid, ITagData>();

            userData = dataStore.Table<UserData>().First();
            dataStore.Table<WorkspaceData>().ForEach(x => workspaces.Add(x.Id, x));
            dataStore.Table<WorkspaceUserData>().ForEach(x => workspaceUserData.Add(x.Id, x));
            dataStore.Table<ProjectData>().ForEach(x => projects.Add(x.Id, x));
            dataStore.Table<ProjectUserData>().ForEach(x => projectUsers.Add(x.Id, x));
            dataStore.Table<ClientData>().ForEach(x => clients.Add(x.Id, x));
            dataStore.Table<TaskData>().ForEach(x => tasks.Add(x.Id, x));
            dataStore.Table<TagData>().ForEach(x => tags.Add(x.Id, x));

            var settings = state.Settings;
            IOldSettingsStore oldSettings;
            UserData userDataUpdated = userData;

            if (ServiceContainer.TryResolve(out oldSettings) &&
                    oldSettings.UserId != null &&
                    oldSettings.UserId != Guid.Empty)
            {
                // Set api token.
                userData.ApiToken = oldSettings.ApiToken;
                userDataUpdated = (UserData)dataStore.Update(ctx => ctx.Put(userData)).Single();

                // projectDefault: default value used
                // lastReportZoom: Default value used
                settings = settings.With(
                               userId: oldSettings.UserId.Value,
                               useTag: oldSettings.UseDefaultTag,
                               lastAppVersion: oldSettings.LastAppVersion,
                               groupedEntries: oldSettings.GroupedTimeEntries,
                               showWelcome: oldSettings.ShowWelcome,
                               chooseProjectForNew: oldSettings.ChooseProjectForNew,
                               getChangesLastRun: oldSettings.SyncLastRun.HasValue ? oldSettings.SyncLastRun.Value : DateTime.MinValue,
                               // iOS only values
                               rossReadDurOnlyNotice : oldSettings.RossReadDurOnlyNotice,
                               // Android only values
                               pushToken : oldSettings.GcmRegistrationId,
                               idleNotification : oldSettings.IdleNotification,
                               showNotification : oldSettings.ShowNotification);
            }

            return DataSyncMsg.Create(state.With(
                                          settings: settings,
                                          requestInfo: RequestInfo.Empty,
                                          user: userDataUpdated,
                                          workspaces: workspaces,
                                          projects: projects,
                                          workspaceUsers: workspaceUserData,
                                          projectUsers: projectUsers,
                                          clients: clients,
                                          tasks: tasks,
                                          tags: tags,
                                          timeEntries: new Dictionary<Guid, RichTimeEntry>()));
        }

        static DataSyncMsg<AppState> UpdateSettings(AppState state, DataMsg msg)
        {
            var info = (msg as DataMsg.UpdateSetting).Data.ForceLeft();
            SettingsState newSettings = state.Settings;

            switch (info.Item1)
            {
                case nameof(SettingsState.ShowWelcome):
                    newSettings = newSettings.With(showWelcome: (bool)info.Item2);
                    break;
                case nameof(SettingsState.ProjectSort):
                    newSettings = newSettings.With(projectSort: (string)info.Item2);
                    break;
                case nameof(SettingsState.ShowNotification):
                    newSettings = newSettings.With(showNotification: (bool)info.Item2);
                    break;
                case nameof(SettingsState.IdleNotification):
                    newSettings = newSettings.With(idleNotification: (bool)info.Item2);
                    break;
                case nameof(SettingsState.ChooseProjectForNew):
                    newSettings = newSettings.With(chooseProjectForNew: (bool)info.Item2);
                    break;
                case nameof(SettingsState.UseDefaultTag):
                    newSettings = newSettings.With(useTag: (bool)info.Item2);
                    break;
                case nameof(SettingsState.GroupedEntries):
                    newSettings = newSettings.With(groupedEntries: (bool)info.Item2);
                    break;

                    // TODO: log invalid/unknowns?
            }

            return DataSyncMsg.Create(state.With(settings: newSettings));
        }

        #region Util

        static bool MergeOfflineTable<T>(ISyncDataStoreContext ctx, Tuple<Guid, long> ws = null, Tuple < Guid, long?> user = null)
        where T : CommonData
        {
            if (ctx == null) { throw new ArgumentNullException(nameof(ctx)); }
            if (ws == null && user == null) { throw new ArgumentNullException($"{nameof(ws)} && {nameof(user)}"); }

            var tableName = ctx.Connection.GetMapping<T>().TableName;

            var sql = new List<string>();
            var args = new List<object>();

            if (ws != null)
            {
                var wsIdCol = Util.GetPropertyName<TimeEntryData, Guid>(x => x.WorkspaceId);
                var wsRemoteIdCol = Util.GetPropertyName<TimeEntryData, long>(x => x.WorkspaceRemoteId);
                var syncStateCol = Util.GetPropertyName<TimeEntryData, SyncState>(x => x.SyncState);

                sql.Add($"{wsIdCol}=?, {wsRemoteIdCol}=?, {syncStateCol}=?");
                args.Add(ws.Item1);
                args.Add(ws.Item2);
                args.Add(SyncState.CreatePending);
            }

            if (user != null)
            {
                var userIdCol = Util.GetPropertyName<TimeEntryData, Guid>(x => x.UserId);
                var userRemoteIdCol = Util.GetPropertyName<TimeEntryData, long>(x => x.UserRemoteId);
                sql.Add($"{userIdCol}=?, {userRemoteIdCol}=?");
                args.Add(user.Item1);
                args.Add(user.Item2);
            }

            return ctx.Connection.Execute($"UPDATE {tableName} SET {string.Join(", ", sql)}", args.ToArray()) > 0;
        }

        static IReadOnlyDictionary<Guid, T> MergeOfflineAppState<T>(
            bool edit, IReadOnlyDictionary<Guid, T> oldItems, Func<T, Tuple<Guid, T>> mapper)
        {
            // If there've been no edits, return the state untouched
            if (!edit)
                return oldItems;

            return oldItems.Values.Select(mapper).ToDictionary(x => x.Item1, x => x.Item2);
        }

        static AppState MergeOfflineDb(AppState state, Guid wsId, long wsRemoteId, Guid userId, long? userRemoteId)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            dataStore.Update(ctx =>
            {
                var ws = Tuple.Create(wsId, wsRemoteId);
                var user = Tuple.Create(userId, userRemoteId);

                // We're not using ctx.Put/Delete here so we need to update the state in-memory ourselves
                // For performance, only touch the app state if there've been actual changes in the db
                var edit1 = MergeOfflineTable<ProjectData>(ctx, ws);
                var edit2 = MergeOfflineTable<ClientData>(ctx, ws);
                var edit3 = MergeOfflineTable<TagData>(ctx, ws);
                var edit4 = MergeOfflineTable<ProjectUserData>(ctx, user: user); // Attention, named parameter
                var edit5 = MergeOfflineTable<WorkspaceUserData>(ctx, ws, user);
                var edit6 = MergeOfflineTable<TimeEntryData>(ctx, ws, user);

                ctx.Connection.Table<WorkspaceData> ().Delete(x => x.Id != wsId);

                // Attention! We're modifying directly the objects in memory by unsafe casting
                // This should be forbidden but there's no simple alternative in this case
                state = state.With(
                            projects: MergeOfflineAppState(edit1, state.Projects, x =>
                {
                    var y = (ProjectData)x;
                    y.WorkspaceId = wsId;
                    y.WorkspaceRemoteId = wsRemoteId;
                    return Tuple.Create(y.Id, (IProjectData)y);
                }),
                clients: MergeOfflineAppState(edit2, state.Clients, x =>
                {
                    var y = (ClientData)x;
                    y.WorkspaceId = wsId;
                    y.WorkspaceRemoteId = wsRemoteId;
                    return Tuple.Create(y.Id, (IClientData)y);
                }),
                tags: MergeOfflineAppState(edit3, state.Tags, x =>
                {
                    var y = (TagData)x;
                    y.WorkspaceId = wsId;
                    y.WorkspaceRemoteId = wsRemoteId;
                    return Tuple.Create(y.Id, (ITagData)y);
                }),
                projectUsers: MergeOfflineAppState(edit4, state.ProjectUsers, x =>
                {
                    var y = (ProjectUserData)x;
                    y.UserId = userId;
                    y.UserRemoteId = userRemoteId ?? 0;
                    return Tuple.Create(y.Id, (IProjectUserData)y);
                }),
                workspaceUsers: MergeOfflineAppState(edit5, state.WorkspaceUsers, x =>
                {
                    var y = (WorkspaceUserData)x;
                    y.WorkspaceId = wsId;
                    y.WorkspaceRemoteId = wsRemoteId;
                    y.UserId = userId;
                    y.UserRemoteId = userRemoteId ?? 0;
                    return Tuple.Create(y.Id, (IWorkspaceUserData)y);
                }),
                timeEntries: MergeOfflineAppState(edit6, state.TimeEntries, x =>
                {
                    var y = (TimeEntryData)x.Data;
                    y.WorkspaceId = wsId;
                    y.WorkspaceRemoteId = wsRemoteId;
                    y.UserId = userId;
                    y.UserRemoteId = userRemoteId ?? 0;
                    return Tuple.Create(y.Id, new RichTimeEntry(y, x.Info));
                }),
                workspaces: state.Workspaces.Values.Where(x => x.Id == wsId).ToDictionary(x => x.Id)
                        );
            });
            return state;
        }

        static AppState UpdateStateWithNewData(AppState state, IEnumerable<CommonData> receivedData)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            dataStore.Update(ctx =>
            {
                foreach (var iterator in receivedData)
                {
                    // Check first if the newData has localId assigned
                    // (for example, the ones returned by TogglClient.Create)
                    // If no localId, check if an item with the same RemoteId is in the db
                    var newData = iterator;
                    var oldData = newData.Id != Guid.Empty
                                  ? ctx.GetByColumn(newData.GetType(), nameof(ICommonData.Id), newData.Id)
                                  : ctx.GetByColumn(newData.GetType(), nameof(ICommonData.RemoteId), newData.RemoteId);

                    if (oldData != null)
                    {
                        // TODO RX check this criteria to compare.
                        // and evaluate if local relations are needed.
                        if (newData.CompareTo(oldData) >= 0)
                        {
                            newData.Id = oldData.Id;
                            if (newData.DeletedAt != null)
                                DestroyLocalRelationships(state, newData, ctx);
                            else
                                newData = BuildLocalRelationships(state, newData);  // Set local Id values.
                            PutOrDelete(ctx, newData);
                        }
                        else
                        {
                            // No changes, just continue.
                            var logger = ServiceContainer.Resolve<ILogger>();
                            logger.Info("UpdateStateWithNewData", "Posible sync error. Object without changes " + newData);
                            continue;
                        }
                    }
                    else
                    {
                        newData.Id = Guid.NewGuid();  // Assign new Id
                        newData = BuildLocalRelationships(state, newData);  // Set local Id values.
                        PutOrDelete(ctx, newData);
                    }

                    // TODO RX Create a single update method for state.
                    state = state.With(
                                workspaces: state.Update(state.Workspaces, ctx.UpdatedItems),
                                projects: state.Update(state.Projects, ctx.UpdatedItems),
                                workspaceUsers: state.Update(state.WorkspaceUsers, ctx.UpdatedItems),
                                projectUsers: state.Update(state.ProjectUsers, ctx.UpdatedItems),
                                clients: state.Update(state.Clients, ctx.UpdatedItems),
                                tasks: state.Update(state.Tasks, ctx.UpdatedItems),
                                tags: state.Update(state.Tags, ctx.UpdatedItems),
                                timeEntries: state.UpdateTimeEntries(ctx.UpdatedItems)
                            );
                }
            });
            return state;
        }

        static void DestroyLocalRelationships(AppState state, CommonData removedData, ISyncDataStoreContext ctx)
        {
            if (removedData is IClientData)
            {
                state.Projects.Values.Where(x => x.ClientRemoteId == removedData.RemoteId)
                .Select(x => x.With(p =>
                {
                    p.ClientRemoteId = null;
                    p.ClientId = Guid.Empty;
                }))
                .ForEach(x => ctx.Put(x));
            }

            if (removedData is IProjectData)
            {
                state.TimeEntries.Values.Where(x => x.Data.ProjectRemoteId == removedData.RemoteId)
                .Select(x => x.Data.With(t =>
                {
                    t.TaskId = Guid.Empty;
                    t.TaskRemoteId = null;
                    t.ProjectId = Guid.Empty;
                    t.ProjectRemoteId = null;
                })).ForEach(ctx.Put);
            }

            if (removedData is ITagData)
            {
                var removedTag = (ITagData)removedData;
                state.TimeEntries.Values.Where(x => x.Data.Tags.Contains(removedTag.Name))
                .Select(x => x.Data.With(t =>
                {
                    t.Tags = new List<string>(t.Tags.Where(n => n != removedTag.Name));
                })).ForEach(ctx.Put);
            }

            if (removedData is IWorkspaceData)
            {
                // TODO Ask what to do in this cases.
            }

            if (removedData is ITaskData)
            {
                // TODO Ask what to do in this cases.
            }

        }

        static bool HasAnyData()
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
            if (dataStore.Table<TimeEntryData>().Count() > 0 ||
                    dataStore.Table<ProjectData>().Count() > 0)
            {
                return true;
            }
            return false;

        }
        static CommonData BuildLocalRelationships(AppState state, CommonData data)
        {
            // Build local relationships.
            // Object that comes from server needs to be
            // filled with local Ids.
            if (data is TimeEntryData)
            {
                var te = (TimeEntryData)data;
                te.UserId = state.User.Id;
                te.WorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == te.WorkspaceRemoteId).Id;
                if (te.ProjectRemoteId.HasValue &&
                        state.Projects.Any(x => x.Value.RemoteId == te.ProjectRemoteId.Value))
                {
                    te.ProjectId = state.Projects.Single(x => x.Value.RemoteId == te.ProjectRemoteId.Value).Value.Id;
                }

                if (te.TaskRemoteId.HasValue &&
                        state.Tasks.Any(x => x.Value.RemoteId == te.TaskRemoteId.Value))
                {
                    te.TaskId = state.Tasks.Single(x => x.Value.RemoteId == te.TaskRemoteId.Value).Value.Id;
                }
                return te;
            }

            if (data is ProjectData)
            {
                var pr = (ProjectData)data;
                pr.WorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == pr.WorkspaceRemoteId).Id;
                if (pr.ClientRemoteId.HasValue &&
                        state.Clients.Any(x => x.Value.RemoteId == pr.ClientRemoteId.Value))
                {
                    pr.ClientId = state.Clients.Single(x => x.Value.RemoteId == pr.ClientRemoteId.Value).Value.Id;
                }
                return pr;
            }

            if (data is ClientData)
            {
                var cl = (ClientData)data;
                cl.WorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == cl.WorkspaceRemoteId).Id;
                return cl;
            }

            if (data is TaskData)
            {
                var ts = (TaskData)data;
                ts.WorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == ts.WorkspaceRemoteId).Id;
                if (state.Projects.Any(x => x.Value.RemoteId == ts.ProjectRemoteId))
                {
                    ts.ProjectId = state.Projects.Single(x => x.Value.RemoteId == ts.ProjectRemoteId).Value.Id;
                }
                return ts;
            }

            if (data is TagData)
            {
                var t = (TagData)data;
                t.WorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == t.WorkspaceRemoteId).Id;
                return t;
            }

            if (data is UserData)
            {
                var u = (UserData)data;
                u.DefaultWorkspaceId = state.Workspaces.Values.Single(x => x.RemoteId == u.DefaultWorkspaceRemoteId).Id;
            }

            return data;
        }

        static void PutOrDelete(ISyncDataStoreContext ctx, ICommonData data)
        {
            if (data.DeletedAt == null)
            {
                ctx.Put(data);
            }
            else
            {
                ctx.Delete(data);
            }
        }

        // TODO: replace this method with the SQLite equivalent.
        static DateTime GetDatesByDays(ISyncDataStore dataStore, DateTime startDate, int numDays)
        {
            var baseQuery = dataStore.Table<TimeEntryData>().Where(
                                r => r.State != TimeEntryState.New &&
                                r.StartTime < startDate &&
                                r.DeletedAt == null);

            var entries = baseQuery.ToList();
            if (entries.Count > 0)
            {
                var group = entries
                            .OrderByDescending(r => r.StartTime)
                            .GroupBy(t => t.StartTime.Date)
                            .Take(numDays)
                            .LastOrDefault();
                return group.Key;
            }
            return DateTime.MinValue;
        }

        private static void IgnoreTaskErrors(System.Threading.Tasks.Task task, string errorMessage)
        {
            task.ContinueWith(t =>
            {
                var e = t.Exception;
                var log = ServiceContainer.Resolve<ILogger>();
                log.Warning(nameof(Reducers), e, errorMessage);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        #endregion
    }
}

