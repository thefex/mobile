using System;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    public enum Mood
    {
        Neutral,
        Positive,
        Negative,
    }

    [ImplementPropertyChanged]
    public class FeedbackVM : ViewModelBase
    {
        private readonly AppState state;
        private const string Tag = "FeedbackMessage";

        public FeedbackVM(AppState state)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Feedback";
            this.state = state;
        }

        public async Task<bool> Send(Mood currentMod, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var json = new FeedbackJson()
            {
                Subject = "-- Toggl Mobile Feedback",
                IsMobile = true,
            };

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(message);
            sb.AppendLine();
            sb.AppendLine("――――");
            sb.AppendLine();

            AppendMood(currentMod, sb);
            AppendTimeInfo(sb);
            AppendTimeEntryStats(sb);

            var client = ServiceContainer.Resolve<ITogglClient> ();
            var logStore = ServiceContainer.Resolve<LogStore> ();
            try
            {
                json.Message = sb.ToString();
                json.AttachmentData = await logStore.Compress().ConfigureAwait(false);
                if (json.AttachmentData != null)
                {
                    json.AttachmentName = "log.gz";
                }

                await client.CreateFeedback(state.User.ApiToken, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var log = ServiceContainer.Resolve<ILogger> ();
                if (ex.IsNetworkFailure())
                {
                    log.Info(Tag, ex, "Failed to send feedback.");
                }
                else
                {
                    log.Warning(Tag, ex, "Failed to send feedback.");
                }
                return false;
            }

            return true;
        }

        private void AppendMood(Mood currentMood, StringBuilder sb)
        {
            sb.AppendFormat("Mood: {0}", currentMood);
            sb.AppendLine();
        }

        private void AppendTimeInfo(StringBuilder sb)
        {
            var timeManager = ServiceContainer.Resolve<TimeCorrectionManager>();
            sb.AppendFormat("Time correction: {0}", timeManager.Correction);
            sb.AppendLine();
            sb.AppendFormat("Time zone: {0}", Time.TimeZoneId);
            sb.AppendLine();
        }

        private void AppendTimeEntryStats(StringBuilder sb)
        {
            var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            var total = dataStore.Table<TimeEntryData> ().Count();
            var createPending = dataStore.Table<TimeEntryData> ().Count(r => r.SyncState == SyncState.CreatePending);
            var updatePending = dataStore.Table<TimeEntryData> ().Count(r => r.SyncState == SyncState.UpdatePending);
            var synced = dataStore.Table<TimeEntryData> ().Count(r => r.SyncState == SyncState.Synced);
            sb.AppendLine("Time entries:");
            sb.AppendFormat(" - {0} Total", total);
            sb.AppendLine();
            sb.AppendFormat(" - {0} Create Pending", createPending);
            sb.AppendLine();
            sb.AppendFormat(" - {0} Update Pending", updatePending);
            sb.AppendLine();
            sb.AppendFormat(" - {0} Synced", synced);
            sb.AppendLine();
            sb.AppendLine();
        }

        public bool IsNoUserMode
        {
            get
            {
                return String.IsNullOrEmpty(StoreManager.Singleton.AppState.User.ApiToken);
            }
        }
    }
}

