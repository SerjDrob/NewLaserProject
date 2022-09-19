using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NewLaserProject.Views.Misc
{
    internal class ResizeThumb : Thumb
    {
        public ResizeThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var designerItem = this.DataContext as Control;

            if (designerItem != null)
            {
                var delta = (VerticalAlignment, HorizontalAlignment) switch
                {
                    (VerticalAlignment.Bottom, _) => Math.Min(-e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight),
                    (VerticalAlignment.Top, _) => Math.Min(e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight),
                    (_, HorizontalAlignment.Right) => Math.Min(-e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth),
                    (_, HorizontalAlignment.Left) => Math.Min(e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth)
                };

                designerItem.Width -= delta;
                designerItem.Height -= delta;
            }

            e.Handled = true;
        }
    }
}
