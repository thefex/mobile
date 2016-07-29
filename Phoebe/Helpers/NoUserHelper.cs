using System;
using Toggl.Phoebe.Reactive;

namespace Toggl.Phoebe.Helpers
{
    public static class NoUserHelper
    {
        public static bool IsLoggedIn
            => StoreManager.Singleton.AppState.User.Id != Guid.Empty;
    }
}