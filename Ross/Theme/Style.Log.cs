using System;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class Log
        {
            public static void HeaderBackgroundView(UIView v)
            {
                v.BackgroundColor = Color.Background;
            }

            public static void HeaderDateLabel(UILabel v)
            {
                v.TextColor = Color.OffSteel;
                v.TextAlignment = UITextAlignment.Left;
                v.Font = Font.Main(12);
            }

            public static void HeaderDurationLabel(UILabel v)
            {
                v.TextColor = Color.OffSteel;
                v.TextAlignment = UITextAlignment.Right;
                v.Font = Font.Main(12);
            }

            public static void ReloadTableViewFooterLabel(UILabel v)
            {
                v.TextColor = Color.Gray;
                v.TextAlignment = UITextAlignment.Center;
                v.Font = Font.MainLight(12);
            }

            public static void ReloadTableViewFooterButton(UIButton v)
            {
                v.Font = Font.Main(13);
                v.SetTitleColor(Color.DarkGray, UIControlState.Normal);
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

            public static void TaggedEntry(UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Image.IconTag;
            }

            public static void BillableEntry(UIImageView v)
            {
                v.ContentMode = UIViewContentMode.Center;
                v.Image = Image.IconBillable;
            }

            public static void PlainEntry(UIImageView v)
            {
                v.Image = null;
            }

            public static void SectionBorder(UIView v)
            {
                v.BackgroundColor = Color.Border;
            }

            public static void EntryList(UITableView v)
            {
                v.SeparatorInset = UIEdgeInsets.Zero;
                v.SeparatorColor = Color.Border;
                v.BackgroundColor = Color.Background;
            }
        }

    }
}
