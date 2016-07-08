using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;

namespace Toggl.Joey.UI.Adapters
{
    public class SettingsAdapter : BaseAdapter
    {
        private const int HeaderViewType = 0;
        private const int CheckboxViewType = 1;
        private readonly List<IListItem> listItems;

        Binding<bool, bool> runningNotificationBinding, idleBinding, chooseProjectBinding, useDefaultBinding, groupedBinding;
        SettingsVM viewModel { get; set; }
        CheckboxListItem groupedEntries { get; set; }
        CheckboxListItem runningNotification { get; set; }
        CheckboxListItem idleNotification { get; set; }
        CheckboxListItem chooseProjectForNew { get; set; }
        CheckboxListItem useDefault { get; set; }

        public SettingsAdapter()
        {
            viewModel = new SettingsVM(StoreManager.Singleton.AppState);
            runningNotification = new CheckboxListItem(
                Resource.String.SettingsGeneralRunningNotificationTitle,
                Resource.String.SettingsGeneralRunningNotificationDesc,
                nameof(SettingsState.RunningNotification),
                viewModel.SetRunningNotification);

            idleNotification = new CheckboxListItem(
                Resource.String.SettingsGeneralNotifTitle,
                Resource.String.SettingsGeneralNotifDesc,
                nameof(SettingsState.IdleNotification),
                viewModel.SetIdleNotification);

            chooseProjectForNew = new CheckboxListItem(
                Resource.String.SettingsGeneralAskProjectTitle,
                Resource.String.SettingsGeneralAskProjectDesc,
                nameof(SettingsState.ChooseProjectForNew),
                viewModel.SetChooseProjectForNew);

            useDefault = new CheckboxListItem(
                Resource.String.SettingsGeneralMobileTagTitle,
                Resource.String.SettingsGeneralMobileTagDesc,
                nameof(SettingsState.UseDefaultTag),
                viewModel.SetUseDefaultTag);

            groupedEntries = new CheckboxListItem(
                Resource.String.SettingsGeneralGroupedEntriesTitle,
                Resource.String.SettingsGeneralGroupedEntriesDesc,
                nameof(SettingsState.GroupedEntries),
                viewModel.SetGroupedTimeEntries);

            runningNotificationBinding = this.SetBinding(() => viewModel.RunningNotification, () => runningNotification.IsChecked);
            idleBinding = this.SetBinding(() => viewModel.IdleNotification, () => idleNotification.IsChecked);
            chooseProjectBinding = this.SetBinding(() => viewModel.ChooseProjectForNew, () => chooseProjectForNew.IsChecked);
            useDefaultBinding = this.SetBinding(() => viewModel.UseDefaultTag, () => useDefault.IsChecked);
            groupedBinding = this.SetBinding(() => viewModel.GroupedTimeEntries, () => groupedEntries.IsChecked);

            listItems = new List<IListItem>
            {
                new HeaderListItem(Resource.String.SettingsGeneralHeader),
                runningNotification,
                idleNotification,
                chooseProjectForNew,
                useDefault,
                groupedEntries
            };

            viewModel.PropertyChanged += (e, ar) => NotifyDataSetChanged();
        }

        public override int ViewTypeCount
        {
            get { return 2; }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override bool IsEnabled(int position)
        {
            return GetItemViewType(position) != HeaderViewType;
        }

        public override int GetItemViewType(int position)
        {
            return listItems [position].ViewType;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView;
            var listItem = listItems [position];
            var viewType = listItem.ViewType;

            if (viewType == HeaderViewType)
            {
                if (view == null)
                {
                    view = LayoutInflater.FromContext(parent.Context).Inflate(
                               Resource.Layout.SettingsListHeaderItem, parent, false);
                    view.Tag = new HeaderListItemHolder(view);
                }
                var holder = (HeaderListItemHolder)view.Tag;
                holder.Rebind((HeaderListItem)listItem);
            }
            else if (viewType == CheckboxViewType)
            {
                if (view == null)
                {
                    view = LayoutInflater.FromContext(parent.Context).Inflate(
                               Resource.Layout.SettingsListCheckboxItem, parent, false);
                    view.Tag = new CheckboxListItemHolder(view);
                }
                var holder = (CheckboxListItemHolder)view.Tag;
                holder.Rebind((CheckboxListItem)listItem);
            }
            else
            {
                throw new InvalidOperationException(String.Format("Unknown view type {0}", viewType));
            }

            return view;
        }

        public override int Count
        {
            get { return listItems.Count; }
        }

        public void OnItemClicked(int position)
        {
            var item = listItems [position];

            var checkItem = item as CheckboxListItem;
            if (checkItem != null)
            {
                checkItem.Toggle();
            }
        }

        private interface IListItem
        {
            int ViewType { get; }
        }

        private class HeaderListItem : IListItem
        {
            private readonly int headerResId;

            public HeaderListItem(int headerResId)
            {
                this.headerResId = headerResId;
            }

            public int ViewType
            {
                get { return HeaderViewType; }
            }

            public int HeaderResId
            {
                get { return headerResId; }
            }
        }

        private class CheckboxListItem : IListItem
        {
            private readonly int titleResId;
            private readonly int descriptionResId;
            private readonly string settingName;
            private readonly Action<bool> valueSetter;

            public CheckboxListItem(int titleResId, int descriptionResId, string settingName, Action<bool> valueSetter)
            {
                this.titleResId = titleResId;
                this.descriptionResId = descriptionResId;
                this.settingName = settingName;
                this.valueSetter = valueSetter;
            }

            public int ViewType
            {
                get { return CheckboxViewType; }
            }

            public int TitleResId
            {
                get { return titleResId; }
            }

            public int DescriptionResId
            {
                get { return descriptionResId; }
            }

            public string SettingName
            {
                get { return settingName; }
            }

            public bool IsChecked { get; set; }

            public void Toggle()
            {
                valueSetter(!IsChecked);
            }
        }

        private class HeaderListItemHolder : Java.Lang.Object
        {
            public TextView HeaderTextView { get; private set; }

            public HeaderListItemHolder(View root)
            {
                HeaderTextView = root.FindViewById<TextView> (Resource.Id.HeaderTextView).SetFont(Font.RobotoMedium);
            }

            public void Rebind(HeaderListItem dataSource)
            {
                HeaderTextView.SetText(dataSource.HeaderResId);
            }
        }


        private class CheckboxListItemHolder : Java.Lang.Object
        {
            public TextView TitleTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public CheckBox CheckBox { get; private set; }

            public CheckboxListItemHolder(View root)
            {
                TitleTextView = root.FindViewById<TextView> (Resource.Id.TitleTextView).SetFont(Font.Roboto);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont(Font.RobotoLight);
                CheckBox = root.FindViewById<CheckBox> (Resource.Id.CheckBox);
            }

            public void Rebind(CheckboxListItem dataSource)
            {
                TitleTextView.SetText(dataSource.TitleResId);
                DescriptionTextView.SetText(dataSource.DescriptionResId);
                CheckBox.Checked = dataSource.IsChecked;
            }
        }
    }
}
