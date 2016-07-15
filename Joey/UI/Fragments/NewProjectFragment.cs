using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.InputMethods;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class NewProjectFragment : Fragment, IOnClientSelectedHandler
    {
        public static readonly int ClientSelectedRequestCode = 1;
        private static readonly string WorkspaceIdArgument = "workspace_id_param";
        private static int ColumnsCount = 5;
        private static int RowsCount = 5;

        private ActionBar Toolbar;
        public TogglField ProjectBit { get; private set; }
        public TogglField SelectClientBit { get; private set; }
        private RecyclerView colorPicker;
        public NewProjectVM ViewModel { get; private set; }

        // Binding to avoid weak references
        // to be collected by the
        // garbage collector. Under investigation ;)
        private Binding<int, int> colorBinding;
        private Binding<string, string> nameBinding;
        private Binding<string, string> clientBinding;

        private Guid WorkspaceId
        {
            get
            {
                Guid id;
                Guid.TryParse(Arguments.GetString(WorkspaceIdArgument), out id);
                return id;
            }
        }

        public NewProjectFragment()
        {
        }

        public NewProjectFragment(IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base(jref, xfer)
        {
        }

        public static NewProjectFragment NewInstance(string workspaceId)
        {
            var fragment = new NewProjectFragment();

            var args = new Bundle();
            args.PutString(WorkspaceIdArgument, workspaceId);
            fragment.Arguments = args;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.NewProjectFragment, container, false);
            var activity = (Activity)Activity;

            var toolbar = view.FindViewById<Toolbar> (Resource.Id.NewProjectsFragmentToolbar);
            activity.SetSupportActionBar(toolbar);

            Toolbar = activity.SupportActionBar;
            Toolbar.SetDisplayHomeAsUpEnabled(true);
            Toolbar.SetTitle(Resource.String.NewProjectTitle);

            ProjectBit = view.FindViewById<TogglField> (Resource.Id.NewProjectProjectNameBit)
                         .DestroyAssistView().DestroyArrow()
                         .SetName(Resource.String.NewProjectProjectFieldName)
                         .SetHint(Resource.String.NewProjectProjectFieldHint);

            SelectClientBit = view.FindViewById<TogglField> (Resource.Id.SelectClientNameBit)
                              .DestroyAssistView().SetName(Resource.String.NewProjectClientFieldName)
                              .SetHint(Resource.String.NewProjectClientFieldHint)
                              .SimulateButton();

            SelectClientBit.TextField.Click += SelectClientBitClickedHandler;
            SelectClientBit.Click += SelectClientBitClickedHandler;

            // Setup colour picker
            colorPicker = view.FindViewById<RecyclerView> (Resource.Id.NewProjectColorPickerRecyclerView);
            colorPicker.SetLayoutManager(new GridLayoutManager(Context, ColumnsCount));
            var adapter = new ColorPickerAdapter(ColumnsCount, RowsCount);
            colorPicker.SetAdapter(adapter);

            HasOptionsMenu = true;
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            ViewModel = new NewProjectVM(Phoebe.Reactive.StoreManager.Singleton.AppState, WorkspaceId);
            clientBinding = this.SetBinding(() => ViewModel.ClientName, () => SelectClientBit.TextField.Text);
        }

        public override void OnDestroyView()
        {
            ViewModel.Dispose();
            base.OnDestroyView();
        }

        public override void OnStart()
        {
            base.OnStart();

            // show keyboard
            var inputService = (InputMethodManager)Activity.GetSystemService(Context.InputMethodService);
            ProjectBit.TextField.PostDelayed(delegate
            {
                inputService.ShowSoftInput(ProjectBit.TextField, ShowFlags.Implicit);
            }, 100);
        }

        private async void SaveButtonHandler(object sender, EventArgs e)
        {
            // TODO: Deactivate Save btn.
            // If name is empty.
            var projectName = ProjectBit.TextField.Text;
            if (string.IsNullOrEmpty(projectName))
            {
                new AlertDialog.Builder(Activity)
                .SetTitle(Resource.String.NewProjectEmptyDialogTitle)
                .SetMessage(Resource.String.NewProjectEmptyDialogMessage)
                .SetPositiveButton(Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                .Show();
                return;
            }

            // If name exists.
            var existsName = ViewModel.ExistProjectWithName(projectName);
            if (existsName)
            {
                new AlertDialog.Builder(Activity)
                .SetTitle(Resource.String.NewProjectDuplicateDialogTitle)
                .SetMessage(Resource.String.NewProjectDuplicateDialogMessage)
                .SetPositiveButton(Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                .Show();
                return;
            }

            // Save project and send result.
            var newProjectData = await ViewModel.SaveProjectAsync(projectName, ((ColorPickerAdapter)colorPicker.GetAdapter()).SelectedColor);
            var resultIntent = new Intent();
            resultIntent.PutExtra(BaseActivity.IntentProjectIdArgument, newProjectData.Id.ToString());
            Activity.SetResult(Result.Ok, resultIntent);
            Activity.Finish();
        }

        private void SelectClientBitClickedHandler(object sender, EventArgs e)
        {
            if (ViewModel.ContainsClients(WorkspaceId))
            {
                ClientListDialogFragment.NewInstance(WorkspaceId)
                .SetClientSelectListener(this)
                .Show(FragmentManager, "clients_dialog");
            }
            else
            {
                CreateClientDialogFragment.NewInstance(WorkspaceId)
                .SetOnClientSelectedListener(this)
                .Show(FragmentManager, "new_client_dialog");
            }
        }

        public void OnClientSelected(IClientData data)
        {
            ViewModel.SetClient(data);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.SaveItemMenu, menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                var resultIntent = new Intent();
                Activity.SetResult(Result.Canceled, resultIntent);
                Activity.Finish();
            }
            else
            {
                SaveButtonHandler(this, null);
            }
            return base.OnOptionsItemSelected(item);
        }


    }
}
