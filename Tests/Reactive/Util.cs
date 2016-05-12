using System;
using System.Collections.Generic;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;

namespace Toggl.Phoebe.Tests.Reactive
{
    public class PlatformUtils : IPlatformUtils
    {
        public string AppIdentifier { get; set; }

        public string AppVersion { get; set; }

        public bool IsWidgetAvailable { get; set; }

        public ISQLitePlatform SQLiteInfo
        {
            get
            {
                return new SQLitePlatformGeneric();
            }
        }

        public void DispatchOnUIThread(Action action)
        {
            action();
        }
    }

    public class NetWorkPresenceMock : INetworkPresence
    {
        public bool IsNetworkPresent { get { return false; } }

        public void RegisterSyncWhenNetworkPresent()
        {
            throw new NotImplementedException();
        }

        public void UnregisterSyncWhenNetworkPresent()
        {
            throw new NotImplementedException();
        }
    }

    public class ToggleClientMock : ITogglClient
    {
        public static string fakeUserEmail = "test@toggl.com";
        public static string fakeUserPassword = "123";
        public static string fakeGoogleId = "12345";

        public Random rnd = new Random();
        public IList<CommonJson> ReceivedItems = new List<CommonJson> ();


        public Task<T> Create<T> (string authToken, T jsonObject) where T : CommonJson
        {
            ReceivedItems.Add(jsonObject);
            jsonObject.RemoteId = rnd.Next(100);
            return Task.FromResult(jsonObject);
        }
        public Task<T> Get<T> (string authToken, long id) where T : CommonJson
        {
            throw new NotImplementedException();
        }
        public Task<List<T>> List<T> (string authToken) where T : CommonJson
        {
            throw new NotImplementedException();
        }
        public Task<T> Update<T> (string authToken, T jsonObject) where T : CommonJson
        {
            return Task.Run(() =>
            {
                ReceivedItems.Add(jsonObject);
                return jsonObject;
            });
        }
        public Task Delete<T> (string authToken, T jsonObject) where T : CommonJson
        {
            return Task.Run(() =>
            {
                ReceivedItems.Add(jsonObject);
            });
        }
        public Task Delete<T> (string authToken, IEnumerable<T> jsonObjects) where T : CommonJson
        {
            throw new NotImplementedException();
        }
        public Task<UserJson> GetUser(string username, string password)
        {
            Task.Delay(500);
            if (username == fakeUserEmail && password == fakeUserPassword)
            {
                var user = new UserJson()
                {
                    Email = fakeUserEmail,
                    Password = fakeUserPassword,
                    Name = "Test",
                    DefaultWorkspaceRemoteId = 123
                };
                return Task.FromResult(user);
            }
            return null;
        }

        public Task<UserJson> GetUser(string googleAccessToken)
        {
            if (googleAccessToken == fakeGoogleId)
            {
                var user = new UserJson()
                {
                    Email = fakeUserEmail,
                    Password = fakeUserPassword,
                    Name = "Test",
                    DefaultWorkspaceRemoteId = 123
                };
                return Task.FromResult(user);
            }
            return null;
        }

        public Task<List<ClientJson>> ListWorkspaceClients(string authToken, long workspaceId)
        {
            throw new NotImplementedException();
        }
        public Task<List<ProjectJson>> ListWorkspaceProjects(string authToken, long workspaceId)
        {
            throw new NotImplementedException();
        }
        public Task<List<WorkspaceUserJson>> ListWorkspaceUsers(string authToken, long workspaceId)
        {
            throw new NotImplementedException();
        }
        public Task<List<TaskJson>> ListWorkspaceTasks(string authToken, long workspaceId)
        {
            throw new NotImplementedException();
        }
        public Task<List<TaskJson>> ListProjectTasks(string authToken, long projectId)
        {
            throw new NotImplementedException();
        }
        public Task<List<ProjectUserJson>> ListProjectUsers(string authToken, long projectId)
        {
            throw new NotImplementedException();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime start, DateTime end, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime end, int days, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime end, int days)
        {
            throw new NotImplementedException();
        }
        public Task<UserRelatedJson> GetChanges(string authToken, DateTime? since)
        {
            throw new NotImplementedException();
        }
        public Task CreateFeedback(string authToken, FeedbackJson jsonObject)
        {
            throw new NotImplementedException();
        }
        public Task CreateExperimentAction(string authToken, ActionJson jsonObject)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetSince<T>(string authToken, DateTime ? since) where T : CommonJson
        {
            throw new NotImplementedException();
        }
    }

    public class TrackerMock : ITracker
    {
        public string CurrentScreen
        {
            set { } // Do nothing
        }
        public void SendAccountCreateEvent(AccountCredentials credentialsType)
        {
            // Do nothing
        }
        public void SendAccountLoginEvent(AccountCredentials credentialsType)
        {
            // Do nothing
        }
        public void SendAccountLogoutEvent()
        {
            // Do nothing
        }
        public void SendAppInitTime(TimeSpan duration)
        {
            // Do nothing
        }

        public void SendIntroModeEvent(UserMode mode)
        {
            throw new NotImplementedException();
        }

        public void SendRegisterEvent(AccountCredentials credentialsType)
        {
            throw new NotImplementedException();
        }

        public void SendSettingsChangeEvent(SettingName settingName)
        {
            // Do nothing
        }
        public void SendTimerStartEvent(TimerStartSource startSource)
        {
            // Do nothing
        }
        public void SendTimerStopEvent(TimerStopSource stopSource)
        {
            // Do nothing
        }

        public void SendToRegisterScreenEvent(RegisterSource source)
        {
            throw new NotImplementedException();
        }
    }

    public static class Util
    {
        public static readonly Guid UserId = Guid.NewGuid();
        public static readonly Guid WorkspaceId = Guid.NewGuid();

        public static ITimeEntryData CreateTimeEntryData(
            DateTime startTime, long userRemoteId = 1, long workspaceRemoteId = 1)
        {
            return TimeEntryData.Create(x =>
            {
                x.Description = x.Id.ToString();
                x.IsBillable = true;
                x.DurationOnly = true;
                x.StartTime = startTime;
                x.StopTime = startTime.AddMinutes(1);
                x.Tags = new List<string> ();
                x.TaskRemoteId = null;
                x.UserRemoteId = userRemoteId;
                x.WorkspaceRemoteId = workspaceRemoteId;
                x.UserId = UserId;
                x.WorkspaceId = WorkspaceId;
                x.State = TimeEntryState.Finished;
            });
        }

        public static TaskCompletionSource<T> CreateTask<T> (int timeout = 10000)
        {
            var tcs = new TaskCompletionSource<T> ();
            var timer = new System.Timers.Timer(timeout);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                tcs.SetException(new TimeoutException());
            };
            timer.Start();
            return tcs;
        }

        public static AppState GetInitAppState()
        {
            var userData = new UserData { Id = UserId };
            var workspaceData = new WorkspaceData { Id = WorkspaceId };

            var init = AppState.Init();
            return init.With(user: userData, workspaces: init.Update(init.Workspaces, new[] { workspaceData }));
        }
    }

    public class NetworkSwitcher : INetworkPresence
    {
        private bool isConnected;

        public bool IsNetworkPresent
        {
            get
            {
                return isConnected;
            }
        }

        public void SetNetworkConnection(bool isConnected)
        {
            this.isConnected = isConnected;
        }
    }
}
