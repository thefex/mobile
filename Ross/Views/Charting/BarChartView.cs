﻿using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views.Charting
{
    public class BarChartView : UIView, IReportChart, IBarChartDataSource
    {
        public EventHandler GoForwardInterval { get; set; }

        public EventHandler GoBackInterval { get; set; }

        public EventHandler AnimationStarted { get; set; }

        public EventHandler AnimationEnded { get; set; }

        private SummaryReportView _reportView;

        public SummaryReportView ReportView
        {
            get {
                return _reportView;
            } set {
                _reportView = value;

                if (_reportView.Activity == null) {
                    return;
                }

                var delayNoData = (_reportView.Projects.Count == 0) ? 0.5 : 0;
                var delayData = (_reportView.Projects.Count == 0) ? 0 : 0.5;

                UIView.Animate (0.3, delayNoData, UIViewAnimationOptions.TransitionNone,
                () => {
                    noProjectTextLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                    noProjectTitleLabel.Alpha = (_reportView.Projects.Count == 0) ? 1 : 0;
                },  null);

                UIView.Animate (0.5, delayData, UIViewAnimationOptions.TransitionNone,
                () => {
                    barChart.Alpha = (_reportView.Projects.Count == 0) ? 0.5f : 1;
                },  null);

                totalTimeLabel.Text = _reportView.TotalGrand;
                moneyLabel.Text = _reportView.TotalBillale;
                ActivityList = _reportView.Activity;
                barChart.ReloadData ();
            }
        }

        readonly UIStringAttributes hoursAttrs = new UIStringAttributes {
            ForegroundColor = UIColor.FromRGB (0xBB, 0xBB, 0xBB),
            BackgroundColor = UIColor.Clear,
            Font = UIFont.FromName ("HelveticaNeue", 9f)
        };

        readonly UIStringAttributes topLabelAttrs = new UIStringAttributes {
            ForegroundColor = UIColor.FromRGB (0x87, 0x87, 0x87),
            BackgroundColor = UIColor.Clear,
            Font = UIFont.FromName ("HelveticaNeue", 12f)
        };

        public BarChartView (RectangleF frame) : base (frame)
        {
            ActivityList = new List<ReportActivity> ();

            titleTimeLabel = new UILabel (new RectangleF (0, 0, 120, 20));
            titleTimeLabel.Text = "ReportsTotalLabel".Tr ();
            titleTimeLabel.Apply (Style.ReportsView.BarCharLabelTitle );
            Add (titleTimeLabel);

            totalTimeLabel = new UILabel (new RectangleF (frame.Width - 120, 0, 120, 20));
            totalTimeLabel.Apply (Style.ReportsView.BarCharLabelValue);
            Add (totalTimeLabel);

            titleMoneyLabel = new UILabel (new RectangleF (0, 20, 120, 20));
            titleMoneyLabel.Apply (Style.ReportsView.BarCharLabelTitle);
            titleMoneyLabel.Text = "ReportsBillableLabel".Tr ();
            Add (titleMoneyLabel);
            moneyLabel = new UILabel (new RectangleF (frame.Width - 120, 20, 120, 20));
            moneyLabel.Apply (Style.ReportsView.BarCharLabelValue );
            Add (moneyLabel);

            barChart = new BarChart (new RectangleF (0, 50, frame.Width, frame.Height - 50)) {
                DataSource = this
            };
            Add (barChart);

            noProjectTitleLabel = new UILabel ( new RectangleF ( 0, 0, frame.Width/2, 20));
            noProjectTitleLabel.Center = new PointF (barChart.Center.X, barChart.Center.Y - 20);
            noProjectTitleLabel.Apply (Style.ReportsView.NoProjectTitle);
            noProjectTitleLabel.Text = "NoDataTitle".Tr ();
            noProjectTitleLabel.Alpha = 0.0f;
            Add (noProjectTitleLabel);

            noProjectTextLabel = new UILabel ( new RectangleF ( 0, 0, frame.Width/2, 35));
            noProjectTextLabel.Center = new PointF (barChart.Center.X, barChart.Center.Y + 5 );
            noProjectTextLabel.Apply (Style.ReportsView.DonutMoneyLabel);
            noProjectTextLabel.Lines = 2;
            noProjectTextLabel.Text = "NoDataText".Tr ();
            noProjectTextLabel.Alpha = 0.0f;
            Add (noProjectTextLabel);
        }

        public List<ReportActivity> ActivityList;
        BarChart barChart;
        UILabel titleTimeLabel;
        UILabel titleMoneyLabel;
        UILabel totalTimeLabel;
        UILabel moneyLabel;
        UILabel noProjectTitleLabel;
        UILabel noProjectTextLabel;
        UIView dragHelperView;

        UIPanGestureRecognizer panGesture;
        UIDynamicAnimator animator;
        UISnapBehavior snap;
        RectangleF snapRect;
        PointF snapPoint;

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            barChart.Dispose ();
        }

        #region IBarChartDataSource implementation

        public string TimeIntervalAtIndex (int index)
        {
            return _reportView.ChartTimeLabels [index];
        }

        public int NumberOfBarsOnChart (BarChart barChart)
        {
            return ActivityList.Count;
        }

        public float ValueForBarAtIndex (BarChart barChart, int index)
        {
            if (_reportView.MaxTotal == 0) {
                return 0;
            }
            return (ActivityList [index].TotalTime == 0) ? 0 : (float) (ActivityList [index].TotalTime / TimeSpan.FromHours (_reportView.MaxTotal ).TotalSeconds);
        }

        public float ValueForSecondaryBarAtIndex (BarChart barChart, int index)
        {
            if (_reportView.MaxTotal == 0) {
                return 0;
            }
            return (ActivityList [index].BillableTime == 0) ? 0 : (float) (ActivityList [index].BillableTime / TimeSpan.FromHours (_reportView.MaxTotal ).TotalSeconds);
        }

        public string TextForBarAtIndex (BarChart barChart, int index)
        {
            return _reportView.ChartRowLabels [index];
        }

        #endregion
    }
}

