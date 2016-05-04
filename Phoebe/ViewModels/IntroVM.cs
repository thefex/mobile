using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Data;
using System.Reactive.Linq;
using System.Threading;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe.ViewModels
{
    [ImplementPropertyChanged]
    public class IntroVM : ViewModelBase, IDisposable
    {
        IDisposable subscription;

        public IntroVM()
        {
            // Set initial state.
            AuthResult = AuthResult.None;

            subscription = StoreManager
                           .Singleton
                           .Observe(x => x.State.RequestInfo)
                           .DistinctUntilChanged(x => x.AuthResult)
                           .ObserveOn(SynchronizationContext.Current)
                           .SubscribeSimple(reqInfo =>
            {
                AuthResult = reqInfo.AuthResult;
            });
        }

        #region Properties for ViewModel binding

        public AuthResult AuthResult { get; private set; }

        #endregion

        public void SetUpNoUser()
        {
            RxChain.Send(new DataMsg.NoUserState());
        }

        public void Dispose()
        {
        }
    }
}

