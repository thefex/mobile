using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class FeedbackNoApiFragment : Fragment
    {
        private Button noUserRegisterButton;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.FeedbackNoApiFragment, container, false);
            noUserRegisterButton = view.FindViewById<Button>(Resource.Id.FeedbackRegisterButton);
            noUserRegisterButton.Click += OpenRegisterScreen;
            return view;
        }

        private void OpenRegisterScreen(object sender, EventArgs e)
        {
            ((MainDrawerActivity)Activity).OpenPage(DrawerListAdapter.SignupPageId);
        }
    }
}
