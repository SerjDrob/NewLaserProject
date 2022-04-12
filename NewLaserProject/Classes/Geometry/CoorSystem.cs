using Microsoft.Toolkit.Diagnostics;
using netDxf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace NewLaserProject.Classes.Geometry
{
    public class CoorSystem<TPlaceEnum> : ICoorSystem<TPlaceEnum> where TPlaceEnum : Enum
    {
        private Dictionary<TPlaceEnum, CoorSystem<TPlaceEnum>> _subSystems = new();

        private readonly Matrix3 _workTransformation;

        public CoorSystem()
        {
            _workTransformation = new Matrix3(m11: 1, m12: 0, m13: 0, 
                                              m21: 0, m22: 1, m23: 0, 
                                              m31: 0, m32: 0, m33: 1);
        }

        private CoorSystem(Matrix3 mainMatrix)
        {
            _workTransformation = mainMatrix;
        }
        /// <summary>
        /// Instantiates a new object of CoorSystem by accordance of three pairs of points.
        /// </summary>
        /// <param name="first">First pair of points. Item1 and Item2 are "point from" and "point to" respectively</param>
        /// <param name="second">Second pair of points. Item1 and Item2 are "point from" and "point to" respectively</param>
        /// <param name="third">Third pair of points. Item1 and Item2 are "point from" and "point to" respectively</param>
        //private CoorSystem((PointF, PointF) first, (PointF, PointF) second, (PointF, PointF) third)
        //{
        //    var initialPoints = new Matrix3(m11: first.Item1.X, m12: first.Item1.Y, m13: 1,
        //                                    m21: second.Item1.X, m22: second.Item1.Y, m23: 1,
        //                                    m31: third.Item1.X, m32: third.Item1.Y, m33: 1);


        //    var transformedPoints = new Matrix3(m11: first.Item2.X, m12: first.Item2.Y, m13: 1,
        //                                        m21: second.Item2.X, m22: second.Item2.Y, m23: 1,
        //                                        m31: third.Item2.X, m32: third.Item2.Y, m33: 1);

        //    var invert = initialPoints.Inverse();

        //    _mainTransformation = invert * transformedPoints;

        //    _mainTransformation = _mainTransformation.Transpose();

        //    var scaleX = Math.Sqrt(Math.Pow(_mainTransformation.M11, 2) + Math.Pow(_mainTransformation.M12, 2));
        //    var scaleY = -Math.Sqrt(_mainTransformation.M11 * _mainTransformation.M22 - _mainTransformation.M12 * _mainTransformation.M21) / scaleX;
        //    var shearY = Math.Atan2(_mainTransformation.M11 * _mainTransformation.M21 + _mainTransformation.M12 * _mainTransformation.M22, _mainTransformation.M11 * _mainTransformation.M11 + _mainTransformation.M12 * _mainTransformation.M12);
        //    var rotating = Math.Atan2(_mainTransformation.M12, _mainTransformation.M11);
        //    var translationX = _mainTransformation.M13;
        //    var translationY = _mainTransformation.M23;


        //    _skewMatrix = new Matrix3(m11: 1, m12: 0, m13: 0,
        //                                 m21: shearY, m22: 1, m23: 0,
        //                                 m31: 0, m32: 0, m33: 1);

        //    _scalingMatrix = new Matrix3(m11: scaleX, m12: 0, m13: 0,
        //                                    m21: 0, m22: scaleY, m23: 0,
        //                                    m31: 0, m32: 0, m33: 1);

        //    _rotatingMatrix = new Matrix3(m11: Math.Cos(rotating), m12: -Math.Sin(rotating), m13: 0,
        //                                     m21: Math.Sin(rotating), m22: Math.Cos(rotating), m23: 0,
        //                                     m31: 0, m32: 0, m33: 1);

        //    _translationMatrix = new Matrix3(m11: 1, m12: 0, m13: translationX,
        //                                     m21: 0, m22: 1, m23: translationY,
        //                                     m31: 0, m32: 0, m33: 1);
        //}
        public void SetRelatedSystem(TPlaceEnum name, Matrix3x2 matrix)
        {
            var transformation = ConvertMatrix(matrix) * _workTransformation;
            var sub = new CoorSystem<TPlaceEnum>(transformation);
            if (!_subSystems.TryAdd(name, sub))
            {
                _subSystems[name] = sub;
            }
        }

        public static Matrix3 ConvertMatrix(Matrix3x2 initMatrix)
        {
            return new Matrix3(m11: initMatrix.M11, m12: initMatrix.M12, m13: initMatrix.M31,
                               m21: initMatrix.M21, m22: initMatrix.M22, m23: initMatrix.M32,
                               m31: 0, m32: 0, m33: 1);
        }

        public void SetRelatedSystem(TPlaceEnum name, double offsetX, double offsetY)
        {
            var translate = new Matrix3(m11: 1, m12: 0, m13: offsetX,
                                        m21: 0, m22: 1, m23: offsetY,
                                        m31: 0, m32: 0, m33: 1);

            var matrix = translate * _workTransformation;
            var sub = new CoorSystem<TPlaceEnum>(matrix);
            if (!_subSystems.TryAdd(name, sub))
            {
                _subSystems[name] = sub;
            }
        }

        public double[] ToGlobal(double x, double y)
        {
            var vector = new netDxf.Vector3(x, y, 1);
            var result = _workTransformation * vector;
            var points = new PointF[] { new((float)result.X, (float)result.Y) };
            return new double[2] { points[0].X, points[0].Y };
        }
        public double[] ToSub(TPlaceEnum to, double x, double y)
        {
            Guard.IsNotNull(_subSystems, nameof(_subSystems));
            return _subSystems.ContainsKey(to) ? _subSystems[to].ToGlobal(x, y) : throw new KeyNotFoundException($"Subsystem {to} is not set");
        }
        public double[] FromGlobal(double x, double y)
        {            
            try
            {
                var vector = new netDxf.Vector3(x, y, 1);
                var result = _workTransformation.Inverse() * vector;

                return new double[2] { result.X, result.Y };
            }
            catch (Exception)
            {
                throw new Exception("System matrix is not invertible");
            }

        }
        public double[] FromSub(TPlaceEnum from, double x, double y)
        {
            Guard.IsNotNull(_subSystems, nameof(_subSystems));
            return _subSystems.ContainsKey(from) ? _subSystems[from].FromGlobal(x, y) : throw new KeyNotFoundException($"Subsystem {from} is not set");
        }
        public float[] GetMainMatrixElements()
        {
            return new float[]{ (float) _workTransformation.M11, (float) _workTransformation.M12,
                                (float) _workTransformation.M21, (float) _workTransformation.M22,
                                (float) _workTransformation.M13, (float) _workTransformation.M23 };
        }
        public static ThreePointCoorSystemBuilder<TPlaceEnum> GetThreePointSystemBuilder() => new ThreePointCoorSystemBuilder<TPlaceEnum>();
        public static WorkMatrixCoorSystemBuilder<TPlaceEnum> GetWorkMatrixSystemBuilder() => new WorkMatrixCoorSystemBuilder<TPlaceEnum>();

        public class ThreePointCoorSystemBuilder<TPlace> where TPlace : Enum
        {
            private (PointF originPoint, PointF derivativePoint) _firstPair;
            private (PointF originPoint, PointF derivativePoint) _secondPair;
            private (PointF originPoint, PointF derivativePoint) _thirdPair;
            private Matrix3 _workMatrix = new Matrix3(m11: 1, m12: 0, m13: 0,
                                                      m21: 0, m22: 1, m23: 0,
                                                      m31: 0, m32: 0, m33: 1);

            private bool _isWorkMatrixFormed = false;

            public ThreePointCoorSystemBuilder<TPlace> SetFirstPointPair(PointF originPoint, PointF derivativePoint)
            {
                _firstPair = (originPoint, derivativePoint);
                return this;
            }
            public ThreePointCoorSystemBuilder<TPlace> SetSecondPointPair(PointF originPoint, PointF derivativePoint)
            {
                _secondPair = (originPoint, derivativePoint);
                return this;
            }
            public ThreePointCoorSystemBuilder<TPlace> SetThirdPointPair(PointF originPoint, PointF derivativePoint)
            {
                _thirdPair = (originPoint, derivativePoint);
                return this;
            }
            public ThreePointCoorSystemBuilder<TPlace> FormWorkMatrix(params Transformation[] transformations)
            {
                var first = _firstPair;
                var second = _secondPair;
                var third = _thirdPair;

                var initialPoints = new Matrix3(m11: first.Item1.X, m12: first.Item1.Y, m13: 1,
                                                m21: second.Item1.X, m22: second.Item1.Y, m23: 1,
                                                m31: third.Item1.X, m32: third.Item1.Y, m33: 1);


                var transformedPoints = new Matrix3(m11: first.Item2.X, m12: first.Item2.Y, m13: 1,
                                                    m21: second.Item2.X, m22: second.Item2.Y, m23: 1,
                                                    m31: third.Item2.X, m32: third.Item2.Y, m33: 1);

                var invert = initialPoints.Inverse();

                var _mainTransformation = invert * transformedPoints;

                _mainTransformation = _mainTransformation.Transpose();

                if (transformations.Length == 0)
                {
                    _workMatrix = _mainTransformation;
                }
                else
                {
                    foreach (var trans in transformations)
                    {
                        var transformation = trans switch
                        {
                            Transformation.Rotation => GetRotation(_mainTransformation),
                            Transformation.Scaling => GetScale(_mainTransformation),
                            Transformation.Skew => GetSkew(_mainTransformation),
                            Transformation.Translation => GetTranslation(_mainTransformation)
                        };
                        _workMatrix = transformation * _workMatrix;
                    }
                }
                _isWorkMatrixFormed = true;
                return this;
            }

            private Matrix3 GetScale(Matrix3 mainTransformation)
            {
                var scaleX = Math.Sqrt(Math.Pow(mainTransformation.M11, 2) + Math.Pow(mainTransformation.M12, 2));
                var scaleY = -Math.Sqrt(mainTransformation.M11 * mainTransformation.M22 - mainTransformation.M12 * mainTransformation.M21) / scaleX;

                return new Matrix3(m11: scaleX, m12: 0, m13: 0,
                                   m21: 0, m22: scaleY, m23: 0,
                                   m31: 0, m32: 0, m33: 1);
            }

            private Matrix3 GetSkew(Matrix3 mainTransformation)
            {
                //var shearY = Math.Atan2(mainTransformation.M11 * mainTransformation.M21 + mainTransformation.M12 * mainTransformation.M22, mainTransformation.M11 * mainTransformation.M11 + mainTransformation.M12 * mainTransformation.M12);

                var shearY = (mainTransformation.M11 * mainTransformation.M21 + mainTransformation.M12 * mainTransformation.M22)
                            / (mainTransformation.M11 * mainTransformation.M22 + mainTransformation.M12 * mainTransformation.M21);

                //var shearY = (mainTransformation.M11 * mainTransformation.M21 + mainTransformation.M12 * mainTransformation.M22)
                //           / (mainTransformation.M11 * mainTransformation.M11 + mainTransformation.M12 * mainTransformation.M12);

                shearY = Math.Atan(shearY);

                return new Matrix3(m11: 1, m12: 0, m13: 0,
                                   m21: shearY, m22: 1, m23: 0,
                                   m31: 0, m32: 0, m33: 1);
            }

            private Matrix3 GetRotation(Matrix3 mainTransformation)
            {
                var rotating = Math.Atan2(mainTransformation.M12, mainTransformation.M11);

                return new Matrix3(m11: Math.Cos(rotating), m12: -Math.Sin(rotating), m13: 0,
                                   m21: Math.Sin(rotating), m22: Math.Cos(rotating), m23: 0,
                                   m31: 0, m32: 0, m33: 1);
            }
            private Matrix3 GetTranslation(Matrix3 mainTransformation)
            {
                var translationX = mainTransformation.M13;
                var translationY = mainTransformation.M23;
                return new Matrix3(m11: 1, m12: 0, m13: translationX,
                                   m21: 0, m22: 1, m23: translationY,
                                   m31: 0, m32: 0, m33: 1);
            }

            public CoorSystem<TPlace> Build()
            {
                return new CoorSystem<TPlace>(_workMatrix);
            }
        }
        public class WorkMatrixCoorSystemBuilder<TPlace> where TPlace : Enum
        {
            private Matrix3 _workMatrix;

            public WorkMatrixCoorSystemBuilder<TPlace> SetWorkMatrix(Matrix3x2 workMatrix)
            {
                
                _workMatrix = CoorSystem<TPlace>.ConvertMatrix(workMatrix);
                return this;
            }
            public CoorSystem<TPlace> Build()
            {
                return new CoorSystem<TPlace>(_workMatrix);
            }
        }

        public enum Transformation
        {
            Skew,
            Scaling,
            Rotation,
            Translation
        }


    }
}

