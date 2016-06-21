using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class PushRestClient : IPushClient
    {
        private readonly Uri v8Url;

        public PushRestClient(Uri url)
        {
            v8Url = new Uri(url, "v8/push_service/");
        }

        private HttpClient MakeHttpClient()
        {
            // Cannot share HttpClient instance between threads as it might (and will) cause InvalidOperationExceptions
            // occasionally.
#if __TESTS__
            var client = new HttpClient()
#else
            var client = new HttpClient(new ModernHttpClient.NativeMessageHandler())
#endif
            {
                Timeout = TimeSpan.FromSeconds(10),
            };
            var headers = client.DefaultRequestHeaders;
            headers.UserAgent.Clear();
            headers.UserAgent.Add(new ProductInfoHeaderValue(Platform.AppIdentifier, Platform.AppVersion));
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }


        private string GetPayload(string regid)
        {
            return new JObject(
                       new JProperty("pushservicetype", "gcm"),
                       new JProperty("regid", regid)
                   ).ToString();
        }

        private void AddAuthToken(string authToken, HttpRequestMessage req)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(
                                               string.Format("{0}:api_token", authToken))));
        }

        /*
        /// <summary>
        /// deviceToken is only used for iOS
        /// </summary>
        public async Task<string> GetPushToken(object deviceToken = null)
        {
            var token = string.Empty;

            // TODO: Probably not the best way to do this, we can move this code to the platform projects later
            // Register APNS Token to GCM
            var options = new Foundation.NSDictionary();
            options.SetValueForKey((Foundation.NSObject)deviceToken, Google.InstanceID.Constants.RegisterAPNSOption);
            options.SetValueForKey(new Foundation.NSNumber(true), Google.InstanceID.Constants.APNSServerTypeSandboxOption);

            // Get our token
            var tcs = new TaskCompletionSource<string>();

            Google.InstanceID.InstanceId.SharedInstance.Token(
                Build.GcmSenderId,
                Google.InstanceID.Constants.ScopeGCM,
                options,
            (t, error) => { tcs.SetResult(t); });
            token = await tcs.Task;

            return token;
        }
        */

        public async Task Register(string authToken, string pushToken)
        {
            using(var httpClient = MakeHttpClient())
            {
                var url = new Uri(v8Url, "subscribe");
                string json = GetPayload(pushToken);

                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = url,
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                AddAuthToken(authToken, httpReq);

                await httpClient.SendAsync(httpReq).ConfigureAwait(false);
            }
        }

        public async Task Unregister(string authToken, string pushToken)
        {
            using(var httpClient = MakeHttpClient())
            {
                var url = new Uri(v8Url, "unsubscribe");
                string json = GetPayload(pushToken);

                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = url,
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                AddAuthToken(authToken, httpReq);

                await httpClient.SendAsync(httpReq).ConfigureAwait(false);
            }
        }
    }
}
