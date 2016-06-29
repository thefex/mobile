using Android.App;
using Android.Content;
using Android.OS;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace Toggl.Joey.UI.Views
{
    public class NoWorkspaceDialogFragment : DialogFragment
    {
        private const string EmailKey = "com.toggl.timer.email";

        public NoWorkspaceDialogFragment()
        {
        }

        public NoWorkspaceDialogFragment(string email)
        {
            var args = new Bundle();
            args.PutString(EmailKey, email);
            Arguments = args;
        }

        private string Email
        {
            get
            {
                if (Arguments == null)
                {
                    return string.Empty;
                }
                return Arguments.GetString(EmailKey);
            }
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            return new AlertDialog.Builder(Activity)
                   .SetTitle(Resource.String.LoginNoWorkspaceDialogTitle)
                   .SetMessage(Resource.String.LoginNoWorkspaceDialogText)
                   .SetPositiveButton(Resource.String.LoginNoWorkspaceDialogOk, OnOkButtonClicked)
                   .SetNegativeButton(Resource.String.LoginNoWorkspaceDialogCancel, OnCancelButtonClicked)
                   .Create();
        }

        private void OnOkButtonClicked(object sender, DialogClickEventArgs args)
        {
            var intent = new Intent(Intent.ActionSend);
            intent.SetType("message/rfc822");
            intent.PutExtra(Intent.ExtraEmail, new[] { Resources.GetString(Resource.String.LoginNoWorkspaceDialogEmail) });
            intent.PutExtra(Intent.ExtraSubject, Resources.GetString(Resource.String.LoginNoWorkspaceDialogSubject));
            intent.PutExtra(Intent.ExtraText, string.Format(Resources.GetString(Resource.String.LoginNoWorkspaceDialogBody), Email));
            StartActivity(Intent.CreateChooser(intent, (string)null));
        }

        private void OnCancelButtonClicked(object sender, DialogClickEventArgs args)
        {
        }
    }
}

