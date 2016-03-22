﻿using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class NewProjectFragment : Fragment, IOnClientSelectedHandler
    {
        private static readonly string WorkspaceIdArgument = "workspace_id_param";
        public static readonly int ClientSelectedRequestCode = 1;

        private ActionBar Toolbar;
        public TogglField ProjectBit { get; private set; }
        public TogglField SelectClientBit { get; private set; }
        public ColorPickerRecyclerView ColorPicker { get; private set; }
        public NewProjectViewModel ViewModel { get; private set; }
        private IOnProjectCreatedHandler projectCreatedHandler;

        // Binding to avoid weak references
        // to be collected by the
        // garbage collector. Under investigation ;)
        private Binding<int, int> colorBinding;
        private Binding<string, string> nameBinding;
        private Binding<string, string> clientBinding;

        private Guid WorkspaceId { get; set; }

        public NewProjectFragment (string workspaceId)
        {
            var id = Guid.Empty;
            Guid.TryParse (workspaceId, out id);
            WorkspaceId = id;
        }

        public NewProjectFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static NewProjectFragment NewInstance (string workspaceId)
        {
            return new NewProjectFragment (workspaceId);
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.NewProjectFragment, container, false);
            var activity = (Activity)Activity;

            var toolbar = view.FindViewById<Toolbar> (Resource.Id.NewProjectsFragmentToolbar);
            activity.SetSupportActionBar (toolbar);

            Toolbar = activity.SupportActionBar;
            Toolbar.SetDisplayHomeAsUpEnabled (true);
            Toolbar.SetTitle (Resource.String.NewProjectTitle);

            ProjectBit = view.FindViewById<TogglField> (Resource.Id.NewProjectProjectNameBit)
                         .DestroyAssistView ().DestroyArrow ()
                         .SetName (Resource.String.NewProjectProjectFieldName)
                         .SetHint (Resource.String.NewProjectProjectFieldHint);

            SelectClientBit = view.FindViewById<TogglField> (Resource.Id.SelectClientNameBit)
                              .DestroyAssistView().SetName (Resource.String.NewProjectClientFieldName)
                              .SetHint (Resource.String.NewProjectClientFieldHint)
                              .SimulateButton();

            SelectClientBit.TextField.Click += SelectClientBitClickedHandler;
            SelectClientBit.Click += SelectClientBitClickedHandler;

            ColorPicker = view.FindViewById<ColorPickerRecyclerView> (Resource.Id.NewProjectColorPickerRecyclerView);
            HasOptionsMenu = true;

            ((MainDrawerActivity)Activity).ToolbarMode = MainDrawerActivity.ToolbarModes.SubView;

            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ViewModel = await NewProjectViewModel.Init (WorkspaceId);
            clientBinding = this.SetBinding (() => ViewModel.ClientName, () => SelectClientBit.TextField.Text);
        }

        public NewProjectFragment SetOnProjectCreatedHandler (IOnProjectCreatedHandler handler)
        {
            projectCreatedHandler = handler;
            return this;
        }

        public override void OnDestroyView ()
        {
            ViewModel.Dispose ();
            base.OnDestroyView ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            // show keyboard
            var inputService = (InputMethodManager)Activity.GetSystemService (Context.InputMethodService);
            ProjectBit.TextField.PostDelayed (delegate {
                inputService.ShowSoftInput (ProjectBit.TextField, ShowFlags.Implicit);
            }, 100);
        }

        private async void SaveButtonHandler (object sender, EventArgs e)
        {
            // TODO: Deactivate Save btn.
            // If name is empty.
            var projectName = ProjectBit.TextField.Text;
            if (string.IsNullOrEmpty (projectName)) {
                new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.NewProjectEmptyDialogTitle)
                .SetMessage (Resource.String.NewProjectEmptyDialogMessage)
                .SetPositiveButton (Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                .Show ();
                return;
            }

            // If name exists.
            var existsName = await ViewModel.ExistProjectWithName (projectName);
            if (existsName) {
                new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.NewProjectDuplicateDialogTitle)
                .SetMessage (Resource.String.NewProjectDuplicateDialogMessage)
                .SetPositiveButton (Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                .Show ();
                return;
            }

            // Save project and send result.
            var newProjectData = await ViewModel.SaveProject (projectName, ColorPicker.Adapter.SelectedColor);

            projectCreatedHandler.OnProjectCreated (newProjectData.Id);
        }

        private async void SelectClientBitClickedHandler (object sender, EventArgs e)
        {
            if (await ClientListViewModel.ContainsClients (WorkspaceId)) {
                ClientListDialogFragment.NewInstance (WorkspaceId)
                .SetClientSelectListener (this)
                .Show (FragmentManager, "clients_dialog");
            } else {
                CreateClientDialogFragment.NewInstance (WorkspaceId)
                .SetOnClientSelectedListener (this)
                .Show (FragmentManager, "new_client_dialog");
            }
        }

        #region IOnClientSelectedListener implementation

        public void OnClientSelected (ClientData data)
        {
            ViewModel.SetClient (data);
        }

        #endregion

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate (Resource.Menu.SaveItemMenu, menu);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Resource.Id.saveItem) {
                SaveButtonHandler (this, null);
                Activity.OnBackPressed ();
            }
            return base.OnOptionsItemSelected (item);
        }
    }
}
