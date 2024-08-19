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
        public double TableWidth => 77;//130;
        public double TableHeight => 95;//120;
        public double TableOriginX { get; set; } = -55;
        public double TableOriginY { get; set; } = 32;
        public double WaferOriginX { get; set; } = 18;
        public double WaferOriginY { get; set; } = 17;
        public double LaserOriginX => 0;
        public double LaserOriginY => 0;
        public void SetCoordinates(double x, double y) => (TableX, TableY) = (-x, -y);
        public void SetOffsets(double dx, double dy) => (CameraLaserOffsetX, CameraLaserOffsetY) = (-dx, dy);
        public void SetTableOrigin(double orgX, double orgY) => (TableOriginX, TableOriginY) = (-orgX, -orgY);
    }
}
