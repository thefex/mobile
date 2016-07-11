using Android.App;
using Android.Content.PM;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Activity = Android.Support.V7.App.AppCompatActivity;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    [Activity(Label = "ProjectListActivity",
              ScreenOrientation = ScreenOrientation.Portrait,
              Theme = "@style/Theme.Toggl.App")]
    public class ProjectListActivity : BaseActivity
    {
        private static readonly string fragmentTag = "projectlist_fragment";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ProjectListActivityLayout);

            // Check if fragment is still in Fragment manager.
            var fragment = FragmentManager.FindFragmentByTag(fragmentTag);

            if (fragment == null)
            {
                var extras = Intent.Extras;
                if (extras == null)
                {
                    Finish();
                }

                var workspaceId = extras.GetString(IntentWorkspaceIdArgument);
                fragment = ProjectListFragment.NewInstance(workspaceId);
                FragmentManager.BeginTransaction()
                .Add(Resource.Id.ProjectListActivityLayout, fragment, fragmentTag)
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
}
