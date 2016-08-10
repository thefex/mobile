using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.ViewModels;

namespace Toggl.Joey.UI.Activities
{
    [Activity(
         Exported = false,
         WindowSoftInputMode = SoftInput.StateHidden,
         ScreenOrientation = ScreenOrientation.Portrait,
         Theme = "@style/Theme.Toggl.App.EditTimeEntry")]
    public class EditTimeEntryActivity : BaseActivity
    {
        private static readonly string groupfragmentTag = "editgroup_fragment";
        private static readonly string fragmentTag = "edit_fragment";

        public static readonly string IsGrouped = "com.toggl.timer.grouped_edit";
        public static readonly string StartedByFab = "com.toggl.timer.started_by_fab";
        public static readonly string OpenProjects = "com.toggl.timer.open_projects";
        public static readonly string ExtraTimeEntryId = "com.toggl.timer.time_entry_id";
        public static readonly string ExtraGroupedTimeEntriesGuids = "com.toggl.timer.grouped_time_entry_id";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.EditTimeEntryActivity);

            var isGrouped = Intent.Extras.GetBoolean(IsGrouped, false);
            var fragment = FragmentManager.FindFragmentByTag(fragmentTag);
            var groupedFragment = FragmentManager.FindFragmentByTag(groupfragmentTag);

            var guids = Intent.GetStringArrayListExtra(ExtraGroupedTimeEntriesGuids);
            if (guids == null)
            {
                Finish();
            }

            if (isGrouped)
            {
                if (groupedFragment == null)
                {
                    groupedFragment = EditGroupedTimeEntryFragment.NewInstance(guids);
                    FragmentManager.BeginTransaction()
                    .Add(Resource.Id.FrameLayout, groupedFragment, groupfragmentTag)
                    .Commit();
                }
                else
                {
                    FragmentManager.BeginTransaction()
                    .Attach(groupedFragment)
                    .Commit();
                }
            }
            else
            {
                if (fragment == null)
                {
                    fragment = EditTimeEntryFragment.NewInstance(guids[0]);
                    FragmentManager.BeginTransaction()
                    .Add(Resource.Id.FrameLayout, fragment, fragmentTag)
                    .Commit();
                }
                else
                {
                    FragmentManager.BeginTransaction()
                    .Attach(fragment)
                    .Commit();
                }
            }
        }

        public override void OnBackPressed()
        {
            var fragment = (EditTimeEntryFragment)FragmentManager.FindFragmentByTag(fragmentTag);
            if (fragment != null)
            {
                bool dismiss = true;

                if (fragment.ViewModel != null)
                {
                    dismiss = fragment.SaveTimeEntry();
                }

                if (dismiss)
                {
                    base.OnBackPressed();
                }
            }
            else
            {
                base.OnBackPressed();
            }
        }
    }
}
