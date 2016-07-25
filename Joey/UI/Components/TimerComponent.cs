using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Activity = Android.Support.V4.App.FragmentActivity;

namespace Toggl.Joey.UI.Components
{
    public class TimerComponent
    {
        protected TextView DurationTextView { get; private set; }
        protected TextView ProjectTextView { get; private set; }
        protected TextView DescriptionTextView { get; private set; }
        protected TextView TimerTitleTextView { get; private set; }

        public View Root { get; private set; }
        public ImageButton AddManualEntry { get; private set; }
        public LogTimeEntriesVM ViewModel { get; private set; }

        private Binding<bool, bool> isRunningBinding;
        private Binding<string, string> durationBinding;
        private Binding<RichTimeEntry, RichTimeEntry> entryBinding;

        private Activity activity;
        private bool hide;
        private bool isRunning;

        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            set
            {
                isRunning = value;
                TimerTitleTextView.Visibility = isRunning ? ViewStates.Gone : ViewStates.Visible;
                ProjectTextView.Visibility = isRunning ? ViewStates.Visible : ViewStates.Gone;
                DescriptionTextView.Visibility = isRunning ? ViewStates.Visible : ViewStates.Gone;
                DurationTextView.Visibility = isRunning ? ViewStates.Visible : ViewStates.Gone;
            }
        }

        public bool Hide
        {
            get { return hide; }
            set
            {
                hide = value;
                Root.Visibility = Hide ? ViewStates.Gone : ViewStates.Visible;
            }
        }

        public TimerComponent(View root, Activity activity)
        {
            this.activity = activity;
            Root = root;
            DurationTextView = Root.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont(Font.RobotoLight);
            TimerTitleTextView = Root.FindViewById<TextView> (Resource.Id.TimerTitleTextView);
            ProjectTextView = Root.FindViewById<TextView> (Resource.Id.ProjectTextView);
            DescriptionTextView = Root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont(Font.RobotoLight);
            IsRunning = false;
        }

        public void SetViewModel(LogTimeEntriesVM viewModel)
        {
            ViewModel = viewModel;

            // TODO: investigate why WhenSourceChanges doesn't work. :(
            isRunningBinding = this.SetBinding(() => ViewModel.IsEntryRunning, () => IsRunning);
            durationBinding = this.SetBinding(() => ViewModel.Duration, () => DurationTextView.Text);
            entryBinding = this.SetBinding(() => ViewModel.ActiveEntry)
                           .WhenSourceChanges(OnActiveEntryChanged);
        }

        private void OnActiveEntryChanged()
        {
            if (ViewModel.ActiveEntry != null &&
                    DescriptionTextView != null &&
                    ProjectTextView != null)
            {
                var entry = ViewModel.ActiveEntry;
                DescriptionTextView.Text = !string.IsNullOrEmpty(entry.Data.Description)
                                           ? entry.Data.Description : activity.ApplicationContext.Resources.GetText(Resource.String.TimerComponentNoDescription);
                ProjectTextView.Text = !string.IsNullOrEmpty(entry.Info.ProjectData.Name)
                                       ? entry.Info.ProjectData.Name : activity.ApplicationContext.Resources.GetText(Resource.String.TimerComponentNoProject);
            }
        }

        public void DetachBindind()
        {
            isRunningBinding.Detach();
            durationBinding.Detach();
            entryBinding.Detach();
        }
    }
}