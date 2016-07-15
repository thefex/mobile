using System;
using System.Linq;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey.UI.Adapters
{
    public class ColorPickerAdapter : RecyclerView.Adapter
    {
        private readonly int columnsCount;
        private readonly int rowsCount;

        // SelectedColorChanged is needed for binding!
        public event EventHandler SelectedColorChanged;
        public int SelectedColor { get; private set; }

        public ColorPickerAdapter(int columnsCount, int rowsCount)
        {
            this.columnsCount = columnsCount;
            this.rowsCount = rowsCount;
            SelectedColor = 0;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var v = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ColorPickerItem, parent, false);
            return new ColorPickerViewHolder(v, OnClick);
        }

        public override int GetItemViewType(int position)
        {
            return 1;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var h = (ColorPickerViewHolder)holder;
            h.Button.SetBackgroundColor(Color.ParseColor(ProjectData.HexColors.ElementAt(position)));
            h.Tick.Visibility = position == SelectedColor ? ViewStates.Visible : ViewStates.Invisible;
        }

        public override int ItemCount
        {
            get
            {
                return ProjectData.HexColors.Take(columnsCount * rowsCount).Count();
            }
        }

        private void OnClick(int position)
        {
            SelectedColor = position;
            NotifyDataSetChanged();
            if (SelectedColorChanged != null)
            {
                SelectedColorChanged(this, EventArgs.Empty);
            }
        }

        internal class ColorPickerViewHolder : RecyclerView.ViewHolder
        {
            public View Button { get; private set; }
            public ImageView Tick { get; private set; }

            public ColorPickerViewHolder(View v, Action<int> listener) : base(v)
            {
                v.Click += (sender, e) => listener(AdapterPosition);
                Tick = v.FindViewById<ImageView>(Resource.Id.ColorPickerViewTick);
                Tick.BringToFront();
                Button = v.FindViewById<View>(Resource.Id.ColorPickerViewButton);
            }
        }
    }
}

