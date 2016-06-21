using System;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Net
{
    public interface IPushClient
    {
        Task Register(string authToken, string pushToken);

        Task Unregister(string authToken, string pushToken);
    }
}
