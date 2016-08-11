using System;
using CoreGraphics;
using UIKit;

namespace Toggl.Ross.Theme
{
    public static partial class Style
    {
        public static class ProjectList
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

            public static void ProjectLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 15f);
                v.TextAlignment = UITextAlignment.Left;
                v.TextColor = Color.White;
            }

            public static void NoProjectLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 15f);
                v.TextAlignment = UITextAlignment.Left;
                v.TextColor = Color.LightestGray;
            }

            public static void NewProjectLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 15f);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.Gray;
            }

            public static void ClientLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 15f);
                v.TextAlignment = UITextAlignment.Left;
                v.TextColor = Color.White.ColorWithAlpha(0.8f);
            }

            public static void TasksButtons(UIButton v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Medium", 15f);
                v.SetBackgroundImage(TasksBackgroundDefault.Value, UIControlState.Normal);
                v.SetBackgroundImage(TasksBackgroundHighlighted.Value, UIControlState.Highlighted);
            }

            private static Lazy<UIImage> TasksBackgroundDefault = new Lazy<UIImage> (() => MakeTasksBackground(Color.White));
            private static Lazy<UIImage> TasksBackgroundHighlighted = new Lazy<UIImage> (() => MakeTasksBackground(Color.White.ColorWithAlpha(0.75f)));

            private static UIImage MakeTasksBackground(UIColor circleColor)
            {
                const int imageSize = 60;
                const float circleDiameter = 30;

                UIGraphics.BeginImageContextWithOptions(new CGSize(imageSize, imageSize), false, UIScreen.MainScreen.Scale);
                var ctx = UIGraphics.GetCurrentContext();

                ctx.SetFillColor(circleColor.CGColor);
                ctx.AddArc(imageSize / 2f, imageSize / 2f, circleDiameter / 2f, 0, (float)(2 * Math.PI), true);
                ctx.FillPath();

                var image = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();

                return image;
            }

            public static void TaskLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 17f);
                v.TextColor = Color.Black;
            }

            public static void TaskBackground(UIView v)
            {
                v.BackgroundColor = Color.White;
            }

            public static void TaskSeparator(UIView v)
            {
                v.BackgroundColor = Color.LightestGray;
            }

            public static void WorkspaceHeader(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue-Medium", 14f);
                v.TextAlignment = UITextAlignment.Center;
                v.TextColor = Color.LightGray;
            }

            public static void WorkspaceLabel(UILabel v)
            {
                v.Font = UIFont.FromName("HelveticaNeue", 16f);
                v.TextAlignment = UITextAlignment.Left;
                v.TextColor = Color.Gray;
            }
        }
    }
}
