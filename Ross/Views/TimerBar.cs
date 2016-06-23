using System;
using Cirrious.FluentLayouts.Touch;
using CoreAnimation;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class TimerBar : UIToolbar
    {
        public enum State
        {
            TimerInactive,
            TimerRunning,
            ManualMode
        }

        #region views

        private readonly UIButton startButton;
        private readonly UIView startButtonCircle;
        private readonly UISwitch manualSwitch;
        private readonly UIButton manualSwitchHitArea;
        private readonly TimerButtonIcon startButtonIcon;
        private readonly CALayer startButtonHighlight;

        private readonly UILabel timerLabel;
        private readonly UILabel manualLabel;
        private readonly UILabel timerLabelInactive;
        private readonly UILabel manualLabelInactive;

        private readonly UILabel durationLabel;

        private readonly UIView topBorder;

        #endregion

        public event EventHandler StartButtonHit;
        public event EventHandler ManualModeSwitchHit;


        public bool IsManualModeSwitchOn => this.manualSwitch.On;

        public TimerBar()
        {
            this.Apply(Style.Timer.Bar);
            this.Add(this.topBorder = new UIView().Apply(Style.Timer.Border));

            this.Add(this.startButtonCircle = new UIView().Apply(Style.Timer.StartButtonCircle));

            this.startButtonCircle.Layer.AddSublayer(this.startButtonHighlight = new CALayer()
            .Apply(Style.Timer.StartButtonHighlight));
            this.startButtonCircle.Add(this.startButtonIcon = new TimerButtonIcon());

            this.Add(this.startButton = UIButton.FromType(UIButtonType.Custom));

            this.Add(this.timerLabel = new UILabel { Text = "Timer" }
            .Apply(Style.Timer.TimerModeSwitchLabelTimer));
            this.Add(this.manualLabel = new UILabel { Text = "Manual" }
            .Apply(Style.Timer.TimerModeSwitchLabelManual));
            this.Add(this.timerLabelInactive = new UILabel { Text = "Timer" }
            .Apply(Style.Timer.TimerModeSwitchLabelInactive));
            this.Add(this.manualLabelInactive = new UILabel { Text = "Manual" }
            .Apply(Style.Timer.TimerModeSwitchLabelInactive));

            this.Add(this.durationLabel = new UILabel().Apply(Style.Timer.TimerDurationLabel));

            this.Add(this.manualSwitchHitArea = UIButton.FromType(UIButtonType.Custom));
            this.Add(this.manualSwitch = new UISwitch().Apply(Style.Timer.TimerModeSwitch));

            this.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

            this.AddConstraints(
                this.startButton.Width().EqualTo(92),
                this.startButton.AtRightOf(this),
                this.startButton.AtTopOf(this),
                this.startButton.AtBottomOf(this),

                this.manualSwitchHitArea.AtTopOf(this.manualSwitch, -8),
                this.manualSwitchHitArea.AtBottomOf(this.manualSwitch, -8),
                this.manualSwitchHitArea.AtLeftOf(this.timerLabel, -8),
                this.manualSwitchHitArea.AtRightOf(this.manualLabel, -8),

                this.startButtonCircle.WithSameCenterX(this.startButton),
                this.startButtonCircle.WithSameCenterY(this.startButton),
                this.startButtonCircle.Width().EqualTo(40),
                this.startButtonCircle.Height().EqualTo(40),

                this.timerLabel.AtLeftOf(this, 16),
                this.manualSwitch.ToRightOf(this.timerLabel, 0),
                this.manualLabel.ToRightOf(this.manualSwitch, 0),

                this.timerLabel.WithSameCenterY(this),
                this.manualLabel.WithSameCenterY(this),
                this.manualSwitch.WithSameCenterY(this),

                this.manualLabelInactive.WithSameCenterY(this.manualLabel),
                this.manualLabelInactive.WithSameCenterX(this.manualLabel),
                this.timerLabelInactive.WithSameCenterY(this.timerLabel),
                this.timerLabelInactive.WithSameCenterX(this.timerLabel),

                this.durationLabel.WithSameCenterY(this),
                this.durationLabel.Width().EqualTo(124),
                this.durationLabel.AtLeftOf(this),

                this.topBorder.AtTopOf(this),
                this.topBorder.AtLeftOf(this),
                this.topBorder.AtRightOf(this),
                this.topBorder.Height().EqualTo(1)

            );

            this.manualSwitch.ValueChanged += this.onManualSwitchValueChanged;
            this.startButton.TouchUpInside += this.onStartButtonTouchUpInside;
            this.manualSwitchHitArea.TouchUpInside += this.onManualSwitchHitAreaTouchUpInside;

            this.startButton.TouchDown += this.onStartButtonTouchDown;
            this.startButton.TouchCancel += this.onStartButtonTouchCancel;

            this.setState(State.TimerInactive);
        }

        #region event handlers

        private void onStartButtonTouchUpInside(object sender, EventArgs e)
        {
            this.startButtonHighlight.Hidden = true;
            this.StartButtonHit?.Invoke(this, EventArgs.Empty);
        }

        private void onManualSwitchValueChanged(object sender, EventArgs e)
        {
            this.SetState(this.manualSwitch.On ? State.ManualMode : State.TimerInactive);
            this.ManualModeSwitchHit?.Invoke(this, EventArgs.Empty);
        }

        private void onManualSwitchHitAreaTouchUpInside(object sender, EventArgs e)
        {
            this.manualSwitch.SetState(!this.manualSwitch.On, true);
            this.onManualSwitchValueChanged(sender, e);
        }

        private void onStartButtonTouchDown(object sender, EventArgs e)
        {
            this.startButtonHighlight.Hidden = false;
        }

        private void onStartButtonTouchCancel(object sender, EventArgs e)
        {
            this.startButtonHighlight.Hidden = true;
        }

        #endregion

        #region public methods

        public void SetDurationText(string text)
        {
            this.durationLabel.Text = text;
        }

        public void SetState(State state)
        {
            const double animTime = 0.2;

            Animate(animTime, () => this.setState(state, true));
        }

        #endregion

        #region private methods

        private void setState(State state, bool animated = false)
        {
            this.startButtonIcon.SetState(state, animated);

            var isRunning = state == State.TimerRunning;
            var isManual = state == State.ManualMode;

            this.manualSwitchHitArea.Enabled = !isRunning;
            this.manualSwitch.On = isManual;

            var buttonColor = this.startButtonCircle.BackgroundColor;

            switch (state)
            {
                case State.TimerInactive:
                    buttonColor = Color.StartButton;
                    break;
                case State.TimerRunning:
                    buttonColor = Color.StopButton;
                    break;
                case State.ManualMode:
                    buttonColor = Color.AddManualButton;
                    break;
            }

            var manualOpacity = isManual ? 1f : 0f;
            var switchOpacity = isRunning ? 0f : 1f;

            this.setUIState(manualOpacity, switchOpacity, buttonColor);
        }

        private void setUIState(float manualOpacity, float switchOpacity, UIColor buttonColor)
        {
            this.manualLabel.Layer.Opacity = manualOpacity * switchOpacity;
            this.manualLabelInactive.Layer.Opacity = (1 - manualOpacity) * switchOpacity;
            this.timerLabel.Layer.Opacity = (1 - manualOpacity) * switchOpacity;
            this.timerLabelInactive.Layer.Opacity = manualOpacity * switchOpacity;

            this.manualSwitch.Layer.Opacity = switchOpacity;
            this.durationLabel.Layer.Opacity = 1 - switchOpacity;

            this.startButtonCircle.BackgroundColor = buttonColor;
        }

        #endregion
    }
}

