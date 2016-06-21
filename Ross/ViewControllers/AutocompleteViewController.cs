using System;
using Cirrious.FluentLayouts.Touch;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class AutocompleteViewController : ObservableTableViewController<ITimeEntryData>
    {
        EditTimeEntryVM viewModel;
        Action<ITimeEntryData> itemSelectedHandler;

        public AutocompleteViewController(EditTimeEntryVM viewModel, Action<ITimeEntryData> itemSelectedHandler)
        {
            this.viewModel = viewModel;
            this.itemSelectedHandler = itemSelectedHandler;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TableView.TranslatesAutoresizingMaskIntoConstraints = false;
            TableView.BackgroundColor = UIColor.Clear;
            TableView.TableFooterView = new UIView();
            TableView.RowHeight = 60f;
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;

            CreateCellDelegate = CreateTagCell;
            BindCellDelegate = BindCell;
            DataSource = viewModel.SuggestionsCollection;
        }

        private UITableViewCell CreateTagCell(NSString cellIdentifier)
        {
            return new SuggestionCell(cellIdentifier);
        }

        private void BindCell(UITableViewCell cell, ITimeEntryData timeEntryData, NSIndexPath path)
        {
            ((SuggestionCell)cell).Bind(timeEntryData);
        }

        protected override void OnRowSelected(object item, NSIndexPath indexPath)
        {
            base.OnRowSelected(item, indexPath);
            itemSelectedHandler.Invoke((ITimeEntryData)item);
            TableView.DeselectRow(indexPath, true);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        private class SuggestionCell : UITableViewCell
        {
            private const float HorizPadding = 15.0f;
            private readonly UIView textContentView;
            private readonly UILabel projectLabel;
            private readonly UILabel clientLabel;
            private readonly UILabel descriptionLabel;

            public SuggestionCell(NSString cellIdentifier) : base(UITableViewCellStyle.Default, cellIdentifier)
            {
                textContentView = new UIView();

                projectLabel = new UILabel().Apply(Style.Recent.CellProjectLabel);
                clientLabel = new UILabel().Apply(Style.Recent.CellClientLabel);
                descriptionLabel = new UILabel().Apply(Style.Recent.CellDescriptionLabel);

                textContentView.AddSubviews(projectLabel, clientLabel, descriptionLabel);

                ContentView.AddSubview(textContentView);

                ContentView.AddConstraints(new FluentLayout[]
                {
                    textContentView.AtTopOf(ContentView),
                    textContentView.AtBottomOf(ContentView),
                    textContentView.AtLeftOf(ContentView),
                    textContentView.AtRightOf(ContentView)
                } .ToLayoutConstraints());

                textContentView.AddConstraints(new FluentLayout[]
                {
                    projectLabel.AtTopOf(textContentView, 8),
                    projectLabel.AtLeftOf(textContentView, HorizPadding),
                    clientLabel.WithSameCenterY(projectLabel).Plus(1),
                    clientLabel.ToRightOf(projectLabel, 6),
                    clientLabel.AtRightOf(textContentView, HorizPadding),
                    descriptionLabel.Below(projectLabel, 4),
                    descriptionLabel.AtLeftOf(textContentView, HorizPadding + 1),
                    descriptionLabel.AtRightOf(textContentView, HorizPadding)
                } .ToLayoutConstraints());

                textContentView.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
                ContentView.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            }


            public void Bind(ITimeEntryData data)
            {
                var projectName = "LogCellNoProject".Tr();
                var projectColor = Color.Gray;
                var clientName = string.Empty;

                if (data.ProjectId != Guid.Empty)
                {
                    var projectData = StoreManager.Singleton.AppState.Projects[data.ProjectId];
                    projectName = projectData.Name;
                    var hex = projectData.Id != Guid.Empty
                              ? ProjectData.HexColors[projectData.Color % ProjectData.HexColors.Length]
                              : ProjectData.HexColors[ProjectData.DefaultColor];
                    projectColor = UIColor.Clear.FromHex(hex);

                    if (projectData.ClientId != Guid.Empty)
                    {
                        var clientData = StoreManager.Singleton.AppState.Clients[projectData.ClientId];
                        clientName = clientData.Name;
                    }
                }

                projectLabel.TextColor = projectColor;

                if (projectLabel.Text != projectName)
                {
                    projectLabel.Text = projectName;
                    projectLabel.InvalidateIntrinsicContentSize();
                    SetNeedsLayout();
                }

                if (clientLabel.Text != clientName)
                {
                    clientLabel.Text = clientName;
                    clientLabel.InvalidateIntrinsicContentSize();
                    SetNeedsLayout();
                }

                var description = data.Description;
                var descriptionHidden = string.IsNullOrWhiteSpace(description);

                if (descriptionHidden)
                {
                    description = "LogCellNoDescription".Tr();
                    descriptionHidden = false;
                }

                if (descriptionLabel.Text != description)
                {
                    descriptionLabel.Text = description;
                    descriptionLabel.InvalidateIntrinsicContentSize();
                    SetNeedsLayout();
                }

                LayoutIfNeeded();
            }
        }
    }
}


