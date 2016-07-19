using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static class OBMExperimentManager
    {
#if __ANDROID__
        public const int ExperimentNumber = 93;
#else
        public const int ExperimentNumber = 116;
#endif

        public const string StartButtonActionKey = "startButton";
        public const string ClickActionValue = "click";

        public static async void Send(string actionKey, string actionValue, IUserData userData)
        {
            var experimentAction = new ExperimentAction
            {
                ExperimentId = ExperimentNumber,
                ActionKey = actionKey,
                ActionValue = actionValue
            };
            await experimentAction.SendAction(userData.ApiToken);
        }

        public static bool IncludedInExperiment(IUserData userData)
        {
            return userData.ExperimentIncluded && userData.ExperimentNumber == ExperimentNumber;
        }

        public static bool InExperimentGroups(int experimentNumber, IUserData userData)
        {
            return userData.ExperimentNumber == experimentNumber;
        }

        class ExperimentAction
        {
            private const string Tag = "ExperimentAction";

            public int ExperimentId { get; set; }
            public string ActionKey { get; set; }
            public string ActionValue { get; set; }

            public async Task<bool> SendAction(string authToken)
            {
                var client = ServiceContainer.Resolve<ITogglClient> ();
                try
                {
                    var json = new ActionJson
                    {
                        ExperimentId = ExperimentId,
                        Key = ActionKey,
                        Value = ActionValue
                    };
                    await client.CreateExperimentAction(authToken, json).ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    var log = ServiceContainer.Resolve<ILogger> ();
                    if (ex.IsNetworkFailure())
                    {
                        log.Info(Tag, ex, "Network failure. Failed to send obm action.");
                    }
                    else
                    {
                        log.Warning(Tag, ex, "Server error. Failed to send obm action.");
                    }
                    return false;
                }
                return true;
            }
        }
    }
}

