using System;
using System.Reactive.Linq;
using System.Threading;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;

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

