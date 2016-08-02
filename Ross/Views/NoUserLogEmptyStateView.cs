using System.Collections.Generic;
using Cirrious.FluentLayouts.Touch;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class NoUserLogEmptyStateView : UIView
    {
        private readonly UIImageView iconHelloArrowUpView;
        private readonly UIImageView alreadyGotAnAccountView;
        private readonly UIImageView iconHelloTogglerView;
        private readonly UIImageView iconHelloArrowDownView;
        private readonly UIImageView heyThereView;
        private readonly UIImageView newToTogglView;

        public NoUserLogEmptyStateView()
        {
            iconHelloArrowUpView = new UIImageView(Image.IconHelloArrowUp);
            alreadyGotAnAccountView = new UIImageView(Image.AlreadyGotAnAccount);
            iconHelloTogglerView = new UIImageView(Image.IconHelloToggler);
            heyThereView = new UIImageView(Image.HeyThere);
            iconHelloArrowDownView = new UIImageView(Image.IconHelloArrowDown);
            newToTogglView = new UIImageView(Image.NewToToggl);

            this.Add(iconHelloArrowUpView);
            this.Add(alreadyGotAnAccountView);
            this.Add(iconHelloTogglerView);
            this.Add(heyThereView);
            this.Add(iconHelloArrowDownView);
            this.Add(newToTogglView);

            this.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            this.AddConstraints(GenerateConstraints());
        }

        private IEnumerable<FluentLayout> GenerateConstraints()
        {
            var togglerNeedsUplift = UIScreen.MainScreen.Bounds.Height < 500;
            var needsToReduceLabelSize = UIScreen.MainScreen.Bounds.Width < 350;

            //Up arrow
            yield return iconHelloArrowUpView.AtLeftOf(this, 15);

            //Login label
            yield return alreadyGotAnAccountView.ToRightOf(iconHelloArrowUpView, 20);
			yield return alreadyGotAnAccountView.AtBottomOf(iconHelloArrowUpView);

            //Toggler dude and Hey there label
			yield return iconHelloTogglerView.WithSameCenterX(this);
            yield return heyThereView.WithSameCenterX(this);

            if (!togglerNeedsUplift)
            {
				yield return iconHelloTogglerView.WithSameCenterY(this);
				yield return heyThereView.Below(iconHelloTogglerView, 10);
            }
            else
            {
                yield return heyThereView.Above(newToTogglView, 30);
                yield return iconHelloTogglerView.Above(heyThereView, 10);
            }

            //Down arrow
            yield return iconHelloArrowDownView.AtBottomOf(this);
            yield return iconHelloArrowDownView.AtRightOf(this, 86);

            //Start tracking label
            yield return newToTogglView.Above(iconHelloArrowDownView);
            yield return newToTogglView.AtRightOf(this, 58);

            //Reduces both labels, if needed
            if (needsToReduceLabelSize)
            {
                alreadyGotAnAccountView.ClipsToBounds = true;
                alreadyGotAnAccountView.ContentMode = UIViewContentMode.ScaleAspectFit;
                yield return alreadyGotAnAccountView.Width().EqualTo(180);

                newToTogglView.ClipsToBounds = true;
                newToTogglView.ContentMode = UIViewContentMode.ScaleAspectFit;
                yield return newToTogglView.Height().EqualTo(40);
            }
        }
    }
}