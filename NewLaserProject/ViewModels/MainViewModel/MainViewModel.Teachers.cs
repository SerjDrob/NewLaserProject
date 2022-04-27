using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Classes.Teachers;
using NewLaserProject.Properties;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NewLaserProject.ViewModels
{

    internal partial class MainViewModel
    {
        public double TeacherPointerX { get; set; }
        public double TeacherPointerY { get; set; }
        private bool _tempMirrorX;
        private bool _tempWaferTurn90;
        public bool TeacherPointerVisibility { get; set; } = false;


        [ICommand]
        private async Task WaferCornersTeach(bool leftCorner)
        {

            var lwct = WaferCornerTeacher.GetBuilder();

            lwct.SetOnRequestPermissionToStartAction(() => Task.Run(async () =>
            {
                var pointName = leftCorner ? "левую" : "правую";

                if (MessageBox.Show($"Обучить {pointName} точку пластины?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _currentTeacher.Accept();
                }
                else
                {
                    await _currentTeacher.Deny();
                }
            }))
            .SetOnGoCornerPointAction(async () =>
            {
                var x = leftCorner ? Settings.Default.XLeftPoint : Settings.Default.XRightPoint;
                var y = leftCorner ? Settings.Default.YLeftPoint : Settings.Default.YRightPoint;
                var z = Settings.Default.ZeroFocusPoint; //Settings.Default.ZObjective;

                _laserMachine.SetVelocity(Velocity.Fast);
                await Task.WhenAll(
                    _laserMachine.MoveGpInPosAsync(Groups.XY, new double[] { x, y }, true),
                    _laserMachine.MoveAxInPosAsync(Ax.Z, z)
                    );

                techMessager.RealeaseMessage("Наведите перекрестие на угол и нажмите * чтобы продолжить", Icon.Exclamation);
            })
            .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
            {
                techMessager.EraseMessage();
                var x = XAxis.Position;
                var y = YAxis.Position;
                _currentTeacher.SetParams(new double[] { x, y });

                if (MessageBox.Show($"Принять новые координаты точки {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _currentTeacher.Accept();
                }
                else
                {
                    await _currentTeacher.Deny();
                }
            }))
            .SetOnHasResultAction(() => Task.Run(() =>
            {
                if (leftCorner)
                {
                    Settings.Default.XLeftPoint = _currentTeacher.GetParams()[0];
                    Settings.Default.YLeftPoint = _currentTeacher.GetParams()[1];
                }
                else
                {
                    Settings.Default.XRightPoint = _currentTeacher.GetParams()[0];
                    Settings.Default.YRightPoint = _currentTeacher.GetParams()[1];
                }
                Settings.Default.Save();
                techMessager.RealeaseMessage($"Новое значение {_currentTeacher} установленно", Icon.Info);

                //---Set new coordinate system
                _coorSystem = GetCoorSystem();

                _canTeach = false;
            }))
            .SetOnCornerToughtAction(() => Task.Run(() =>
            {
                techMessager.RealeaseMessage("Обучение отменено", Icon.Exclamation);
                _canTeach = false;
            }));
            _currentTeacher = lwct.Build();
            await _currentTeacher.StartTeach();
            _canTeach = true;
        }

        [ICommand]
        private async Task TeachCameraOffset()
        {
            //if(_canTeach) return;
            var teachPosition = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, 10, 10);
            double xOffset = Settings.Default.XOffset;
            double yOffset = Settings.Default.YOffset;
            var zCamera = Settings.Default.ZeroFocusPoint;
            var zLaser = Settings.Default.ZeroPiercePoint;

            var tcb = CameraOffsetTeacher.GetBuilder();
            tcb.SetOnGoLoadPointAction(() => Task.Run(async () =>
            {
                _laserMachine.SetVelocity(Velocity.Fast);
                await _laserMachine.GoThereAsync(LMPlace.Loading);
                techMessager.RealeaseMessage("Установите подложку и нажмите * чтобы продолжить", Icon.Info);
            }))
                .SetOnGoUnderCameraAction(() => Task.Run(async () =>
                {
                    _laserMachine.SetVelocity(Velocity.Fast);
                    await Task.WhenAll(
                        _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera)
                        );
                    techMessager.RealeaseMessage("Выбирете место прожига и нажмите * чтобы продолжить", Icon.Info);
                }))
                .SetOnGoToShotAction(() => Task.Run(async () =>
                {
                    techMessager.EraseMessage();
                    _laserMachine.SetVelocity(Velocity.Fast);
                    await Task.WhenAll(
                            _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { xOffset, yOffset }, true),
                            _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser)
                            );
                    await _laserMachine.PiercePointAsync();
                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                    await Task.WhenAll(
                             _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { -xOffset, -yOffset }, true),
                             _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera)
                             );
                    await _currentTeacher.Accept();
                }))
                .SetOnSearchScorchAction(() =>
                {
                    techMessager.RealeaseMessage("Совместите место прожига с перекрестием камеры и нажмите * чтобы продолжить", Icon.Info);
                    return Task.CompletedTask;
                })
                .SetOnRequestPermissionToStartAction(() => Task.Run(async () =>
                {
                    if (MessageBox.Show("Обучить смещение камеры от объектива лазера?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await _currentTeacher.Accept();
                    }
                    else
                    {
                        await _currentTeacher.Deny();
                    }
                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                    if (MessageBox.Show($"Принять новое смещение камеры от объектива лазера {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await _currentTeacher.Accept();
                    }
                    else
                    {
                        await _currentTeacher.Deny();
                    }
                }))
                .SetOnBiasToughtAction(() => Task.Run(() =>
                {
                    techMessager.RealeaseMessage("Обучение отменено", Icon.Exclamation);
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    Settings.Default.XOffset = _currentTeacher.GetParams()[0];
                    Settings.Default.YOffset = _currentTeacher.GetParams()[1];
                    Settings.Default.Save();
                    techMessager.RealeaseMessage("Новое значение установленно", Icon.Exclamation);

                    //---Set new coordinate system
                    _coorSystem = GetCoorSystem();

                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            await _currentTeacher.StartTeach();
            _canTeach = true;
        }
        [ICommand]
        private void TeachScanatorHorizont()
        {
            var waferWidth = 48;
            var delta = 5;
            var xLeft = delta;
            var xRight = waferWidth - delta;
            var waferHeight = 60;
            float tempX = 0;
            var zCamera = Settings.Default.ZeroFocusPoint;
            var zLaser = Settings.Default.ZeroPiercePoint;

            var tcb = LaserHorizontTeacher.GetBuilder();

            tcb.SetGoUnderCameraAction(() => Task.Run(async () =>
             {
                 _laserMachine.SetVelocity(Velocity.Fast);
                 await Task.WhenAll(
                 _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(waferWidth / 2, waferHeight / 2)),
                 _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera));
                 techMessager.RealeaseMessage("Выберете место на пластине для прожига горизонтальной линии", Icon.Info);
             }))
                .SetGoAtFirstPointAction(() => Task.Run(async () =>
                {
                    _laserMachine.SetVelocity(Velocity.Fast);
                    await Task.WhenAll(
                        _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { Settings.Default.XOffset, Settings.Default.YOffset }, true),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser));

                    var matrix = new System.Drawing.Drawing2D.Matrix();
                    matrix.Rotate((float)Settings.Default.PazAngle * 180 / MathF.PI);
                    var points = new PointF[] { new PointF(xLeft - waferWidth / 2, 0), new PointF(xRight - waferWidth / 2, 0) };
                    matrix.TransformPoints(points);
                    tempX = points[0].X;
                    await _laserMachine.PierceLineAsync(-waferWidth / 2, 0, waferWidth / 2, 0);

                    await Task.WhenAll( 
                        _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera));

                    techMessager.RealeaseMessage("Установите перекрестие на первую точку линии и нажмите *", Icon.Info);
                    tempX = points[1].X;
                }))
                .SetGoAtSecondPointAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { XAxis.Position, YAxis.Position });
                    _laserMachine.SetVelocity(Velocity.Fast);

                    await Task.WhenAll(
                        _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera));
                    techMessager.RealeaseMessage("Установите перекрестие на вторую точку линии и нажмите *", Icon.Info);
                }))
                .SetOnRequestPermissionToStartAction(() => Task.Run(() =>
                {
                    if (MessageBox.Show("Обучить горизонтальность сканатора?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _currentTeacher.Accept();
                    }
                    else
                    {
                        _currentTeacher.Deny();
                    }
                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(() =>
                {
                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                    if (MessageBox.Show($"Принять новое значение горизонтальности сканатора {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _currentTeacher.Accept();
                    }
                    else
                    {
                        _currentTeacher.Deny();
                    }
                    techMessager.EraseMessage();
                }))
                .SetOnLaserHorizontToughtAction(() => Task.Run(() =>
                {
                    MessageBox.Show("Обучение отменено", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    var result = _currentTeacher.GetParams();

                    var first = _coorSystem.FromSub(LMPlace.FileOnWaferUnderCamera, result[0], result[1]);
                    var second = _coorSystem.FromSub(LMPlace.FileOnWaferUnderCamera, result[2], result[3]);

                    double AC = second[0] - first[0];
                    double CB = second[1] - first[1];
                    Settings.Default.PazAngle = Math.Atan2(CB, AC);
                    Settings.Default.Save();
                    MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            _canTeach = true;
        }
        [ICommand]
        private async void TeachOrthXY()
        {
            //_tempMirrorX = MirrorX;
            _tempWaferTurn90 = WaferTurn90;
            //MirrorX = false;
            WaferTurn90 = false;

            var matrixElements = ExtensionMethods.DeserilizeObject<float[]>($"{_projectDirectory}/AppSettings/TeachingDeformation.json");

            var buider = CoorSystem<LMPlace>.GetWorkMatrixSystemBuilder();
            buider.SetWorkMatrix(new Matrix3x2(
                matrixElements[0],
                matrixElements[1],
                matrixElements[2],
                matrixElements[3],
                matrixElements[4],
                matrixElements[5]
                ));
            var sys = buider.Build();


            using var wafer = new LaserWafer<MachineClassLibrary.Laser.Entities.Point>(_dxfReader.GetPoints(), (60, 48));

            var points = wafer.ToList() ?? throw new NullReferenceException();

            Guard.IsEqualTo(points.Count, 3, nameof(points));
            using var pointsEnumerator = points.GetEnumerator();

            _currentTeacher = XYOrthTeacher.GetBuilder()
                .SetOnGoNextPointAction(() => Task.Run(async () =>
                {
                    pointsEnumerator.MoveNext();
                    var point = pointsEnumerator.Current;
                    _laserMachine.VelocityRegime = Velocity.Fast;
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, sys.ToGlobal(point.X, point.Y), true).ConfigureAwait(false);
                    techMessager.RealeaseMessage("Совместите перекрестие визира с ориентиром и нажмите *", Icon.Exclamation);
                    TeacherPointerX = point.X;
                    TeacherPointerY = point.Y;
                    TeacherPointerVisibility = true;
                }))
                .SetOnWriteDownThePointAction(() => Task.Run(async () =>
                {
                    TeacherPointerVisibility = false;
                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                }))
                .SetOnRequestPermissionToStartAction(() => Task.Run(() =>
                {
                    if (MessageBox.Show("Обучить координатную систему лазера?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _currentTeacher.Accept();
                    }
                    else
                    {
                        _currentTeacher.Deny();
                    }
                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(() =>
                {
                    if (MessageBox.Show($"Принять новое значения координат для матрицы преобразования {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _currentTeacher.Accept();
                    }
                    else
                    {
                        _currentTeacher.Deny();
                    }
                }))
                .SetOnXYOrthToughtAction(() => Task.Run(() =>
                {
                    MessageBox.Show("Обучение отменено", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    MirrorX = _tempMirrorX;
                    WaferTurn90 = _tempWaferTurn90;
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    TeacherPointerVisibility = false;
                    techMessager.EraseMessage();

                    var resultPoints = _currentTeacher.GetParams();

                    var builder = CoorSystem<Place>.GetThreePointSystemBuilder();

                    builder.SetFirstPointPair(new((float)points[0].X, (float)points[0].Y), new((float)resultPoints[0], (float)resultPoints[1]))
                           .SetSecondPointPair(new((float)points[1].X, (float)points[1].Y), new((float)resultPoints[2], (float)resultPoints[3]))
                           .SetThirdPointPair(new((float)points[2].X, (float)points[2].Y), new((float)resultPoints[4], (float)resultPoints[5]));

                    //minus means direction of ordinate axis
                    var pureSystem = builder.FormWorkMatrix(0.001, -0.001, true).Build();
                    var teachSystem = builder.FormWorkMatrix(1, 1, false).Build();

                    pureSystem.GetMainMatrixElements().SerializeObject($"{_projectDirectory}/AppSettings/PureDeformation.json");
                    teachSystem.GetMainMatrixElements().SerializeObject($"{_projectDirectory}/AppSettings/TeachingDeformation.json");

                    MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    MirrorX = _tempMirrorX;
                    WaferTurn90 = _tempWaferTurn90;

                    //---Set new coordinate system
                    _coorSystem = GetCoorSystem();

                    _canTeach = false;
                }))
                .Build();
            await _currentTeacher.StartTeach();
            _canTeach = true;
        }
        [ICommand]
        private async Task TeachCameraScale()
        {
            var teachPosition = new double[] { 1, 1 };

            var tcs = CameraScaleTeacher.GetBuilder()
                .SetOnRequestPermissionToStartAction(() => Task.Run(async () =>
                {
                    if (MessageBox.Show("Обучить масштаб видео?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await _currentTeacher.Accept();
                    }
                    else
                    {
                        await _currentTeacher.Deny();
                    }
                }))
                .SetOnGoLoadPointAction(() => Task.Run(async () =>
                {
                    await _laserMachine.GoThereAsync(LMPlace.Loading);
                    techMessager.RealeaseMessage("Установите подложку и нажмите * чтобы продолжить", Icon.Info);
                }))
                .SetOnGoNAskFirstMarkerAction(() => Task.Run(async () =>
                {
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition);
                    TeachScaleMarkerEnable = true;
                    techMessager.RealeaseMessage("Подведите один из маркеров к ориентиру и нажмите * чтобы продолжить", Icon.Info);
                }))
                .SetOnAskSecondMarkerAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { YAxis.Position });
                    techMessager.RealeaseMessage("Подведите второй маркер к этому ориентиру и нажмите * чтобы продолжить", Icon.Info);
                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
                {
                    TeachScaleMarkerEnable = false;
                    var diff = Math.Abs(YAxis.Position - _currentTeacher.GetParams()[0]);
                    var scale = diff / (ScaleMarkersRatioSecond - ScaleMarkersRatioFirst);
                    if (MessageBox.Show($"Принять новый масштаб видео  {scale}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _currentTeacher.SetParams(new double[] { scale });
                        await _currentTeacher.Accept();
                    }
                    else
                    {
                        await _currentTeacher.Deny();
                    }

                }))
                .SetOnScaleToughtAction(() => Task.Run(async () =>
                {
                    techMessager.RealeaseMessage("Обучение отменено", Icon.Exclamation);
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(async () =>
                {
                    Settings.Default.CameraScale = _currentTeacher.GetParams()[0];
                    Settings.Default.Save();
                    techMessager.RealeaseMessage("Новое значение установленно", Icon.Exclamation);
                    _canTeach = false;
                }));
            _currentTeacher = tcs.Build();
            await _currentTeacher.StartTeach();
            _canTeach = true;

        }


        /// <summary>
        /// Teach horizontal or vertical dimension of machine mechanic
        /// </summary>
        /// <param name="horizontal">true stands for horizontal, false stands for vertical</param>
        /// <returns></returns>
        [ICommand]
        private async Task TeachTheDimension(bool horizontal)
        {
            var coordinate = horizontal ? "X" : "Y";

            _currentTeacher = WorkingDimensionTeacher.GetBuilder()
                .SetOnRequestStartingAction(() => Task.Run(async () =>
                {
                    if (MessageBox.Show($"Обучить габарит координаты {coordinate}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await _currentTeacher.Accept();
                    }
                    else
                    {
                        await _currentTeacher.Deny();
                    }
                }))
                .SetOnAtNegativeEdgeAction(() => Task.Run(async () =>
                {
                    techMessager.RealeaseMessage($"Переместите координату {coordinate} до конца в отрицательную сторону и нажмите *", Icon.Info);
                }))
                .SetOnAtPositiveEdgeAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { horizontal ? XAxis.Position : YAxis.Position });
                    techMessager.RealeaseMessage($"Переместите координату {coordinate} до конца в положительную сторону и нажмите *", Icon.Info);
                }))
                .SetOnDimensionToughtAction(() => Task.Run(async () =>
                {
                    techMessager.RealeaseMessage("Обучение отменено", Icon.Exclamation);
                    _canTeach = false;
                }))
                .SetOnRequestAcceptionAction(() => Task.Run(async () =>
                 {
                     _currentTeacher.SetParams(new double[] { horizontal ? XAxis.Position : YAxis.Position });

                     if (MessageBox.Show($"Принять новый габарит координаты {coordinate} {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                     {
                         await _currentTeacher.Accept();
                     }
                     else
                     {
                         await _currentTeacher.Deny();
                     }

                 }))
                .SetOnGiveResultAction(() => Task.Run(async () =>
                {
                    if (horizontal)
                    {
                        Settings.Default.XNegDimension = _currentTeacher.GetParams()[0];
                        Settings.Default.XPosDimension = _currentTeacher.GetParams()[1];
                    }
                    else
                    {
                        Settings.Default.YNegDimension = _currentTeacher.GetParams()[0];
                        Settings.Default.YPosDimension = _currentTeacher.GetParams()[1];
                    }
                    Settings.Default.Save();
                    techMessager.RealeaseMessage("Новое значение установленно", Icon.Exclamation);
                    _canTeach = false;
                }))
                .Build();
            await _currentTeacher.StartTeach();
            _canTeach = true;
        }


        [ICommand]
        private Task TeachNext()
        {
            if (_canTeach)
            {
                return _currentTeacher?.Next();
            }
            return null;
        }
        [ICommand]
        private Task TeachDeny()
        {
            if (_canTeach)
            {
                return _currentTeacher?.Deny();
            }
            return null;
        }



        public double TestPointX { get; set; } = 58;
        public double TestPointY { get; set; } = 46;

        [ICommand]
        private async Task TestPoint1()
        {
            var wafer = new LaserWafer<MachineClassLibrary.Laser.Entities.Point>(new[] { new PPoint(TestPointX, TestPointY, 0, new MachineClassLibrary.Laser.Entities.Point(), "", 0) }, (60, 48));

            var point = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, wafer[0].X, wafer[0].Y);

            await _laserMachine.MoveGpInPosAsync(Groups.XY, point, true);
        }

        [ICommand]
        private void TestPierce()
        {
            _laserMachine.PierceLineAsync(-10, 0, 10, 0);
        }
    }
}