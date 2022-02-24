using Microsoft.Toolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
//using netDxf;

namespace NewLaserProject.Classes.Geometry
{
    public class CoorSystem<TPlaceEnum> : IDisposable where TPlaceEnum : Enum
    {
        private Dictionary<TPlaceEnum, CoorSystem<TPlaceEnum>> _subSystems;
        private readonly Matrix _mainMatrix;
        public CoorSystem(Matrix mainMatrix)
        {
            _mainMatrix = mainMatrix;
        }
        /// <summary>
        /// Instantiates a new object of CoorSystem by accordance of three pairs of points.
        /// </summary>
        /// <param name="first">First pair of points. Item1 and Item2 are "point from" and "point to" respectively</param>
        /// <param name="second">Second pair of points. Item1 and Item2 are "point from" and "point to" respectively</param>
        /// <param name="third">Third pair of points. Item1 and Item2 are "point from" and "point to" respectively</param>
        public CoorSystem((PointF, PointF) first, (PointF, PointF) second, (PointF, PointF) third)
        {
            var list = new List<(PointF, PointF)> { first, second, third };
            list.Sort((f, s) =>
            {
                if (f.Item1.Y > s.Item1.Y) { return -1; }
                else
                {
                    if (f.Item1.Y < s.Item1.Y) { return 1; }
                    else { return 0; }
                }
            });

            list.Sort((f, s) =>
            {
                if (f.Item1.X < s.Item1.X) { return -1; }
                else
                {
                    if (f.Item1.X > s.Item1.X) { return 1; }
                    else { return 0; }
                }

            });

            var size = new SizeF(MathF.Abs(list[1].Item1.X - list[0].Item1.X), MathF.Abs(list[1].Item1.Y - list[0].Item1.Y));

            _mainMatrix = new Matrix(new RectangleF(list[0].Item1, size), new[] { list[0].Item2, list[1].Item2, list[2].Item2 });
        }
        public void SetRelatedSystem(TPlaceEnum name, Matrix matrix)
        {
            matrix.Multiply(_mainMatrix, MatrixOrder.Append);
            var sub = new CoorSystem<TPlaceEnum>(matrix);
            _subSystems = new();
            if (!_subSystems.TryAdd(name, sub))
            {
                _subSystems[name] = sub;
            }
        }
        public void SetRelatedSystem(TPlaceEnum name, double offsetX, double offsetY)
        {
            var matrix = _mainMatrix.Clone();
            matrix.Translate((float)offsetX, (float)offsetY);
            var sub = new CoorSystem<TPlaceEnum>(matrix);
            _subSystems = new();
            if (!_subSystems.TryAdd(name, sub))
            {
                _subSystems[name] = sub;
            }
        }

        public double[] ToGlobal(double x, double y)
        {
            var points = new PointF[] { new((float)x, (float)y) };
            _mainMatrix.TransformPoints(points);
            return new double[2] { points[0].X, points[0].Y };
        }
        public double[] ToSub(TPlaceEnum to, double x, double y)
        {
            Guard.IsNotNull(_subSystems, nameof(_subSystems));
            return _subSystems.ContainsKey(to) ? _subSystems[to].ToGlobal(x, y) : throw new KeyNotFoundException($"Subsystem {to} is not set");
        }
        public double[] FromGlobal(double x, double y)
        {
            if (_mainMatrix.IsInvertible)
            {
                var points = new PointF[] { new((float)x, (float)y) };
                var matrix = _mainMatrix.Clone();
                matrix.Invert();
                matrix.TransformPoints(points);
                return new double[2] { points[0].X, points[0].Y };
            }
            else
            {
                throw new Exception("System matrix is not invertible");
            }

        }
        public double[] FromSub(TPlaceEnum from, double x, double y)
        {
            Guard.IsNotNull(_subSystems, nameof(_subSystems));
            return _subSystems.ContainsKey(from) ? _subSystems[from].FromGlobal(x, y) : throw new KeyNotFoundException($"Subsystem {from} is not set");
        }
        public float[] GetMainMatrixElements() => _mainMatrix.Elements;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_mainMatrix != null)
                {
                    _mainMatrix.Dispose();
                    //_mainMatrix = null;
                }
            }
        }
    }
}

