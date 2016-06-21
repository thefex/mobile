using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.FluentLayouts.Touch;
using Foundation;
using UIKit;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;

namespace Toggl.Ross.ViewControllers
{
    public class NewProjectViewController : UIViewController, IOnClientSelectedHandler
    {
        private TextField nameTextField;
        private UIButton clientButton;
        private Guid workspaceId;
        private IOnProjectSelectedHandler handler;
        private NewProjectVM ViewModel {get; set;}
        private Binding<string, string> clientBinding;

        public NewProjectViewController(Guid workspaceId, IOnProjectSelectedHandler handler)
        {
            this.workspaceId = workspaceId;
            this.handler = handler;
            Title = "NewProjectTitle".Tr();
        }

        public override void LoadView()
        {
            var view = new UIView().Apply(Style.Screen);

            view.Add(nameTextField = new TextField()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString(
                    "NewProjectNameHint".Tr(),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = (tf) => tf.ResignFirstResponder(),
            } .Apply(Style.NewProject.NameField));
            nameTextField.EditingChanged += (sender, e) => ValidateProjectName();

            view.Add(clientButton = new UIButton()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
            } .Apply(Style.NewProject.ClientButton).Apply(Style.NewProject.NoClient));
            clientButton.SetTitle("NewProjectClientHint".Tr(), UIControlState.Normal);
            clientButton.TouchUpInside += OnClientButtonTouchUpInside;

            view.AddConstraints(VerticalLinearLayout(view));

            EdgesForExtendedLayout = UIRectEdge.None;
            View = view;

            var addBtn = new UIBarButtonItem(
                "NewProjectAdd".Tr(), UIBarButtonItemStyle.Plain, OnSetBtnPressed)
            .Apply(Style.NavLabelButton).Apply(Style.DisableNavLabelButton);
            addBtn.Enabled = false;
            NavigationItem.RightBarButtonItem = addBtn;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ViewModel = new NewProjectVM(StoreManager.Singleton.AppState, workspaceId);
            clientBinding = this.SetBinding(() => ViewModel.ClientName).WhenSourceChanges(() =>
            {
                var name = string.IsNullOrEmpty(ViewModel.ClientName) ? "NewProjectClientHint".Tr() : ViewModel.ClientName;
                if (string.IsNullOrEmpty(ViewModel.ClientName))
                {
                    clientButton.Apply(Style.NewProject.ClientButton).Apply(Style.NewProject.NoClient);
                }
                else
                {
                    clientButton.Apply(Style.NewProject.WithClient);
                }
                clientButton.SetTitle(name, UIControlState.Normal);
            });
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            nameTextField.BecomeFirstResponder();
        }

        public override void ViewWillDisappear(bool animated)
        {
            // TODO: Release ViewModel only when the
            // ViewController is poped. It is a weird behaviour
            // considering the property name used.
            // But it works ok.
            if (IsMovingFromParentViewController)
            {
                clientBinding.Detach();
                ViewModel.Dispose();
            }
            base.ViewWillDisappear(animated);
        }

        private void OnClientButtonTouchUpInside(object sender, EventArgs e)
        {
            var controller = new ClientSelectionViewController(workspaceId, this);
            NavigationController.PushViewController(controller, true);
        }

        #region IOnClientSelectedHandler implementation
        public void OnClientSelected(IClientData data)
        {
            ViewModel.SetClient(data);

            var btnLabel = "NewProjectNoNameClient".Tr();
            if (!string.IsNullOrEmpty(data.Name))
            {
                btnLabel = data.Name;
            }
            clientButton.Apply(Style.NewProject.WithClient);
            clientButton.SetTitle(btnLabel, UIControlState.Normal);

            NavigationController.PopToViewController(this, true);
        }
        #endregion

        private async void OnSetBtnPressed(object sender, EventArgs e)
        {
            // Protect againts double touch!
            NavigationItem.RightBarButtonItem.Enabled = false;

            var projectName = nameTextField.Text;
            var existsName = ViewModel.ExistProjectWithName(projectName);
            if (existsName)
            {
                var alert = new UIAlertView(
                    "NewProjectNameExistTitle".Tr(),
                    "NewProjectNameExistMessage".Tr(),
                    null,
                    "NewProjectNameExistOk".Tr(),
                    null);
                alert.Clicked += (s, ev) =>
                {
                    if (ev.ButtonIndex == 0)
                    {
                        nameTextField.BecomeFirstResponder();
                    }
                };
                alert.Show();
                NavigationItem.RightBarButtonItem.Enabled = true;
                return;
            }

            var random = new Random();
            var newProjectData = await ViewModel.SaveProjectAsync(projectName, random.Next(ProjectData.HexColors.Length - 1));
            handler.OnProjectSelected(newProjectData.Id, Guid.Empty);

            // Go back to EditTimeEntryViewController
            NavigationController.PopViewControllers(true, 2);
        }

        private IEnumerable<FluentLayout> VerticalLinearLayout(UIView container)
        {
            UIView prev = null;

            var subviews = container.Subviews.Where(v => !v.Hidden).ToList();
            foreach (var v in subviews)
            {
                if (prev == null)
                {
                    yield return v.AtTopOf(container, 10f);
                }
                else
                {
                    yield return v.Below(prev, 5f);
                }
                yield return v.Height().EqualTo(60f).SetPriority(UILayoutPriority.DefaultLow);
                yield return v.Height().GreaterThanOrEqualTo(60f);
                yield return v.AtLeftOf(container);
                yield return v.AtRightOf(container);

                prev = v;
            }
        }

        private void ValidateProjectName()
        {
            var valid = true;
            var name = nameTextField.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                valid = false;
            }

            if (valid)
            {
                NavigationItem.RightBarButtonItem.Apply(Style.NavLabelButton);
            }
            else
            {
                NavigationItem.RightBarButtonItem.Apply(Style.DisableNavLabelButton);
            }

            NavigationItem.RightBarButtonItem.Enabled = valid;
        }
    }
}
