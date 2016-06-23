using System;
using CoreGraphics;
using UIKit;

namespace Toggl.Ross.Views
{
    public class CircleView : UIView
    {
        CGColor color;

        public CGColor Color
        {
            get { return color; }
            set
            {
                if (color == value)
                    return;
                color = value;
                this.SetNeedsDisplay();
            }
        }

        public CircleView()
        {
            this.BackgroundColor = Theme.Color.Transparent;
        }

        public void SetFrame(nfloat x, nfloat y, nfloat w, nfloat h)
        {
            this.Frame = new CGRect(x - 1, y - 1, w + 2, h + 2);
            this.SetNeedsDisplay();
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            using(var ctx = UIGraphics.GetCurrentContext())
            {
                var bounds = Bounds;
                bounds.X += 1;
                bounds.Y += 1;
                bounds.Width -= 2;
                bounds.Height -= 2;

                ctx.AddEllipseInRect(bounds);
                ctx.SetFillColor(Color);
                ctx.FillPath();
            }
        }
    }
}

