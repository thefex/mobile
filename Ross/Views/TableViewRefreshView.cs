using CoreGraphics;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class TableViewRefreshView : UIRefreshControl
    {
        const float scaleFactor = .8f;

        public TableViewRefreshView()
        {
            this.Apply(Style.TableViewHeader);
            Transform = CGAffineTransform.MakeScale(scaleFactor, scaleFactor);

        }

        public void AdaptToTableView(UITableView tableView)
        {
            tableView.Add(this);
        }

    }
}
