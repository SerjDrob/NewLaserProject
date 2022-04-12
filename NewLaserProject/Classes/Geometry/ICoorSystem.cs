using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;

namespace NewLaserProject.Classes.Geometry
{
    public interface ICoorSystem<TPlaceEnum> where TPlaceEnum : Enum
    {
        double[] FromGlobal(double x, double y);
        double[] FromSub(TPlaceEnum from, double x, double y);
        float[] GetMainMatrixElements();
        void SetRelatedSystem(TPlaceEnum name, double offsetX, double offsetY);
        void SetRelatedSystem(TPlaceEnum name, Matrix3x2 matrix);
        double[] ToGlobal(double x, double y);
        double[] ToSub(TPlaceEnum to, double x, double y);
    }
}