using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsNoApiFragment : Fragment
    {
        private Button noUserRegisterButton;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.ReportsNoApiFragment, container, false);
            noUserRegisterButton = view.FindViewById<Button>(Resource.Id.ReportsRegisterButton);
            noUserRegisterButton.Click += OpenRegisterScreen;
            return view;
        }

        private void OpenRegisterScreen(object sender, EventArgs e)
        {
            //((MainDrawerActivity)Activity).OpenPage(DrawerListAdapter.RegisterUserPageId);
        }
    }
}