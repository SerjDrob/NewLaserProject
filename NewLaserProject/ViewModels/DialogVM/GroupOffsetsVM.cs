using System;
using System.Collections.Generic;
using System.Linq;
using MachineClassLibrary.GeometryUtility;
using MachineControlsLibrary.CommonDialog;
using PropertyChanged;

namespace NewLaserProject.ViewModels.DialogVM
{
    [AddINotifyPropertyChangedInterface]
    partial class GroupOffsetsVM : CommonDialogResultable<(IEnumerable<(double, double)>,double)>
    {
        public double Width { get; init; }
        public double Height { get; init; }
        public double Thickness { get; set; }
        public double EdgeOffset { get; set; } = 1d;
        public int CountX { get; set; } = 10;
        public int CountY { get; set; } = 10;
        public double DeltaX { get => (Width - 2 * EdgeOffset) / (CountX - 1); }
        public double DeltaY { get => (Height - 2 * EdgeOffset) / (CountY - 1); }
        public double PointsCount { get => CountY * CountX; }


        public GroupOffsetsVM(double width, double height, double thickness) => (Width, Height, Thickness) = (width, height, thickness);
        public override void SetResult()
        {
            var xs = Enumerable.Range(0, CountX).Select(x => EdgeOffset + x * DeltaX);
            var ys = Enumerable.Range(0, CountY).Select(y => EdgeOffset + y * DeltaY);
            var points = xs.SelectMany(x => ys.Select(y =>
            {
                return (x, y);
            })).ToList();
            SetResult((points,Thickness));
        }
    }
}
