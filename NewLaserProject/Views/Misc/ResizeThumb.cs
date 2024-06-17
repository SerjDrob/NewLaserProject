using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

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

    internal class ResizeThumb2 : Thumb
    {
        public ResizeThumb2()
        {
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var designerItem = this.DataContext as Control;

            if (designerItem != null)
            {
                var deltaX = HorizontalAlignment switch
                {
                    HorizontalAlignment.Right => Math.Min(-e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth),
                    HorizontalAlignment.Left => Math.Min(e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth),
                    _=>0
                };

                var deltaY = VerticalAlignment switch
                {
                    VerticalAlignment.Bottom => Math.Min(-e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight),
                    VerticalAlignment.Top => Math.Min(e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight),
                    _ => 0

                };

                designerItem.Width -= deltaX;
                designerItem.Height -= deltaY;
            }

            e.Handled = true;
        }
    }

    internal class RotateThumb : Thumb
    {
        public RotateThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var designerItem = this.DataContext as Control;

            if (designerItem != null)
            {
                var deltaX = HorizontalAlignment switch
                {
                    HorizontalAlignment.Right => Math.Min(-e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth),
                    HorizontalAlignment.Left => Math.Min(e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth),
                    _ => 0
                };

                var deltaY = VerticalAlignment switch
                {
                    VerticalAlignment.Bottom => Math.Min(-e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight),
                    VerticalAlignment.Top => Math.Min(e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight),
                    _ => 0

                };

                var trans = designerItem.RenderTransform;
                if(trans is TransformGroup group)
                {
                    group.Children.Insert(0,new RotateTransform(Math.Sign(deltaY)));
                }
            }

            e.Handled = true;
        }
    }
}
