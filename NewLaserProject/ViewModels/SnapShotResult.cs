using NewLaserProject.Classes.Process.ProcessFeatures;
using System.Drawing;
using Point = System.Windows.Point;

namespace NewLaserProject.ViewModels
{
    public record SnapShotResult(Point Point) : IProcessNotify
    {
        public static implicit operator PointF(SnapShotResult result) => new PointF((float)result.Point.X, (float)result.Point.Y);
        public static implicit operator Point(SnapShotResult result) => result.Point;
    }
}
