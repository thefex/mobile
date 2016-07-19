using Cirrious.FluentLayouts.Touch;
using UIKit;
using CoreGraphics;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public class OBMEmptyView : UIView
    {
        private readonly UIImageView togglerImageView;
        private readonly UILabel titleLabel;
        private readonly UILabel messageLabel;
        private readonly UIImageView arrowImageView;

        public OBMEmptyView()
        {
            Add(togglerImageView = new UIImageView().Apply(Style.OBMEmptyView.TogglerImageView));
            Add(arrowImageView = new UIImageView().Apply(Style.OBMEmptyView.ArrowImageView));
            Add(titleLabel = new UILabel().Apply(Style.OBMEmptyView.TitleLabel));
            Add(messageLabel = new UILabel().Apply(Style.OBMEmptyView.MessageLabel));

            this.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            this.AddConstraints(
                togglerImageView.WithSameCenterX(this),
                titleLabel.WithSameCenterX(this),
                messageLabel.WithSameCenterX(this),

                togglerImageView.Above(titleLabel, 12),
                titleLabel.Above(messageLabel, 10),
                messageLabel.Above(arrowImageView, 21),

                arrowImageView.AtRightOf(this, 65),
                arrowImageView.AtBottomOf(this, 20)
            );

        }

        public string Title
        {
            get { return titleLabel.Text; }
            set
            {
                if (titleLabel.Text == value)
                {
                    return;
                }
                titleLabel.Text = value;
                SetNeedsLayout();
            }
        }

        public string Message
        {
            get { return messageLabel.Text; }
            set
            {
                if (messageLabel.Text == value)
                {
                    return;
                }
                messageLabel.Text = value;
                SetNeedsLayout();
            }
        }


    }
}
