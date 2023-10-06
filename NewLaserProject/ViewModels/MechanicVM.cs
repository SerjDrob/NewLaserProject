using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class MechanicVM
    {
        public double TableX { get; set; }
        public double TableY { get; set; }
        public double CameraLaserOffsetX { get; set; } = -69.443;
        public double CameraLaserOffsetY { get; set; } = 3.797;
        public double TableWidth => 130;
        public double TableHeight => 120;
        public double TableOriginX => 3.3;
        public double TableOriginY => 80.59;
        public double LaserOriginX => 0;
        public double LaserOriginY => 0;

        public void SetCoordinates(double x, double y)
        {
            TableX = x;
            TableY = y;
        }
        public void SetOffsets(double dx, double dy)
        {
            CameraLaserOffsetX = dx;
            CameraLaserOffsetY = dy;
        }
    }
}
