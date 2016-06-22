using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Analytics;
using XPlatUtils;
using System.Reactive.Concurrency;
using System.Threading;

namespace Toggl.Phoebe.ViewModels
{
    [ImplementPropertyChanged]
    public class LoginVM : ViewModelBase, IDisposable
    {
        private const string LogTag = "LoginViewModel";
        private const string ValidateEmailRegexp = "^[^<>\\\\#$@\\s]+@[^<>\\\\#$@\\s]*[^<>\\\\#$\\.\\s@]{1}?\\.{1}?[^<>\\\\#$\\.@\\s]{1}?[^<>\\\\#$@\\s]+$";

        IDisposable subscription;

        public enum LoginMode
        {
            Login,
            Signup
        }

        public LoginVM()
        {
            // Set initia state.
            AuthResult = AuthResult.None;
            IsAuthenticated = false;
            IsAuthenticating = false;
            CurrentLoginMode = LoginMode.Login;

            subscription = StoreManager
                           .Singleton
                           .Observe(x => x.State.RequestInfo)
                           .DistinctUntilChanged(x => x.AuthResult)
#if __MOBILE__
                           .ObserveOn(SynchronizationContext.Current)
#else
                           .ObserveOn(Scheduler.CurrentThread)
#endif
                           .SubscribeSimple(reqInfo =>
            {
                AuthResult = reqInfo.AuthResult;
                IsAuthenticating = reqInfo.Running.Any(x => x is ServerRequest.Authenticate);
            });
        }

        public void Dispose()
        {
            subscription.Dispose();
            subscription = null;
        }

        #region Properties for ViewModel binding

        public bool IsAuthenticating { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public LoginMode CurrentLoginMode { get; private set; }

        public AuthResult AuthResult { get; private set; }

        #endregion


        #region public ViewModel methods

        public void ChangeLoginMode()
        {
            CurrentLoginMode = (CurrentLoginMode == LoginMode.Login) ? LoginMode.Signup : LoginMode.Login;
            var screenStr = (CurrentLoginMode == LoginMode.Login) ? "Login" : "Signup";
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = screenStr;
        }

        public void TryLogin(string email, string password)
        {
            if (CurrentLoginMode == LoginMode.Login)
            {
                RxChain.Send(ServerRequest.Authenticate.Login(email, password));
            }
            else
            {
                RxChain.Send(ServerRequest.Authenticate.Signup(email, password));
            }
        }

        public void TryLoginWithGoogle(string token)
        {
            if (CurrentLoginMode == LoginMode.Login)
            {
                RxChain.Send(ServerRequest.Authenticate.LoginWithGoogle(token));
            }
            else
            {
                RxChain.Send(ServerRequest.Authenticate.SignupWithGoogle(token));
            }

        }

        public bool IsEmailValid(string email)
        {
            return Regex.IsMatch(email ?? "", ValidateEmailRegexp);
        }

        public bool IsPassValid(string pass)
        {
            return (pass ?? "").Length >= 6;
        }

        #endregion
    }
}

