using System;
using Cirrious.FluentLayouts.Touch;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class NoUserEmptyView : UIView
    {
        private readonly Action clickHandler;

        public NoUserEmptyView(Screen screen, Action clickHandler)
        {
            this.clickHandler = clickHandler;

            var firstLabelText = string.Empty;
            var secondLabelText = string.Empty;

            switch (screen)
            {
                case Screen.Reports:
                    firstLabelText = "EmptyStatesBoostProductivity";
                    secondLabelText = "EmptyStatesSignUp";
                    break;
                case Screen.Feedback:
                    firstLabelText = "EmptyStatesNoAccess";
                    secondLabelText = "EmptyStatesSignUpToGetInTouch";
                    break;
            }

            var firstLabel = CreateLabel(firstLabelText);
            var secondLabel = CreateLabel(secondLabelText);
            var signUpButton = CreateButton("EmptyStatesSignUpForFree");
			
            BackgroundColor = UIColor.FromRGB(.6f, .6f, .6f);

            Add(firstLabel);
            Add(secondLabel);
            Add(signUpButton);

			this.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            this.AddConstraints(
                
                //Bottom button
                signUpButton.AtBottomOf(this, 40),
                signUpButton.WithSameCenterX(this),
                signUpButton.Width().EqualTo(200),

                //Sign up label
                secondLabel.Above(signUpButton, 35),
                secondLabel.WithSameCenterX(this),

                //Boost productivity label
                firstLabel.Above(secondLabel, 10),
                firstLabel.WithSameCenterX(this)
            );
        }

        private UILabel CreateLabel(string text)
        {
            var label = new UILabel();
            label.Text = text.Tr();
            label.TextColor = UIColor.White;
            return label;
        }

        private UIButton CreateButton(string text)
        {
            var button = new UIButton();
            button.TouchUpInside += HandleClick;
            button.BackgroundColor = Color.LightishGreen;
            button.SetTitle(text.Tr(), UIControlState.Normal);
            button.TitleEdgeInsets = new UIEdgeInsets(10, 10, 10, 10);
            button.SetTitleColor(UIColor.White, UIControlState.Normal);
            return button;
        }

        private void HandleClick(object sender, EventArgs e) => clickHandler?.Invoke();

        public enum Screen
        {
            Reports,
            Feedback
        }
   }
}