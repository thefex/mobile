using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Recent
        {
            public static void HeaderBackgroundView(UIView v)
            {
                v.BackgroundColor = Color.LightestGray;
            }

            public static void HeaderLabel(UILabel v)
            {
                v.TextColor = Color.Gray;
                v.TextAlignment = UITextAlignment.Left;
                v.Font = UIFont.FromName("HelveticaNeue-Medium", 14f);
            }

            public static void CellContentView(UIView v)
            {
                v.Apply(TimeEntryCell.ContentView);
            }

            public static void CellProjectLabel(UILabel v)
            {
                v.Apply(TimeEntryCell.ProjectLabel);
            }

            public static void CellClientLabel(UILabel v)
            {
                v.Apply(TimeEntryCell.ClientLabel);
            }

            public static void CellTaskLabel(UILabel v)
            {
                v.Apply(TimeEntryCell.TaskLabel);
            }

            public static void CellDescriptionLabel(UILabel v)
            {
                v.Apply(TimeEntryCell.DescriptionLabel);
            }

            public static void CellDurationLabel(UILabel v)
            {
                v.Apply(TimeEntryCell.DurationLabel);
            }

        }
    }
}
