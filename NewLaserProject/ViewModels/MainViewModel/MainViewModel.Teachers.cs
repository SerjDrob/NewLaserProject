﻿using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Classes.Teachers;
using NewLaserProject.Properties;
using NewLaserProject.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        public List<string> TeachingSteps { get; set; }
        public int StepIndex { get; set; }

        [ICommand]
        private async Task StartTeaching(Teacher teacher)
        {
            var assignTrigger = _appStateMachine.SetTriggerParameters<Teacher>(AppTrigger.StartLearning);
            await _appStateMachine.FireAsync(assignTrigger,teacher);
        }

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

                techMessager.RealeaseMessage("Наведите перекрестие на угол и нажмите * чтобы продолжить", MessageType.Exclamation);
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
                techMessager.RealeaseMessage($"Новое значение {_currentTeacher} установленно", MessageType.Info);

                //---Set new coordinate system
                _coorSystem = GetCoorSystem();

                _canTeach = false;
            }))
            .SetOnCornerToughtAction(() => Task.Run(() =>
            {
                techMessager.RealeaseMessage("Обучение отменено", MessageType.Exclamation);
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

            TeachingSteps = new() 
            {
                "Установка подложки",
                "Выбор места прожига", 
                "Совмещение прожига"
            };
            StepIndex = -1;

            StartVideoCapture();
            var teachPosition = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, 10, 10);
            double xOffset = Settings.Default.XOffset;
            double yOffset = Settings.Default.YOffset;
            var zCamera = Settings.Default.ZeroFocusPoint;
            var zLaser = Settings.Default.ZeroPiercePoint;
            var waferThickness = WaferThickness;

            var dc = new AskThicknessVM { Thickness = waferThickness };
            new AskThicknesView { DataContext = dc }.ShowDialog();

            
            waferThickness = dc.Thickness;

            var tcb = CameraOffsetTeacher.GetBuilder();
            tcb.SetOnGoLoadPointAction(() => Task.Run(async () =>
            {
                StepIndex++;
                _laserMachine.SetVelocity(Velocity.Fast);
                await _laserMachine.GoThereAsync(LMPlace.Loading);
                techMessager.RealeaseMessage("Установите подложку и нажмите * чтобы продолжить", MessageType.Info);
            }))
                .SetOnGoUnderCameraAction(async () =>
                {
                    StepIndex++;
                    _laserMachine.SetVelocity(Velocity.Fast);
                    await Task.WhenAll(
                        _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - waferThickness)
                        );
                    techMessager.RealeaseMessage("Выбирете место прожига и нажмите * чтобы продолжить", MessageType.Info);
                })
                .SetOnGoToShotAction(async () =>
                {
                    techMessager.EraseMessage();
                    _laserMachine.SetVelocity(Velocity.Fast);
                    await Task.WhenAll(
                            _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { xOffset, yOffset }, true),
                            _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser - waferThickness)
                            );
                    //await _laserMachine.PiercePointAsync();
                    for (int i = 0; i < 2; i++)
                    {
                        await _laserMachine.PierceLineAsync(-0.5, 0, 0.5, 0);
                        await _laserMachine.PierceLineAsync(0, -0.5, 0, 0.5);
                    }


                    await _laserMachine.PierceLineAsync(-0.4, 0.4, 0.4, 0.4);
                    await _laserMachine.PierceLineAsync(-0.4, -0.4, 0.4, -0.4);
                    await _laserMachine.PierceLineAsync(-0.4, -0.4, -0.4, 0.4);
                    await _laserMachine.PierceLineAsync(0.4, -0.4, 0.4, 0.4);


                    //await _laserMachine.PierceLineAsync(0, 0, 0, 0.4);
                    //await _laserMachine.PierceLineAsync(0, 0, 0.2, 0);

                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                    await Task.WhenAll(
                             _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { -xOffset, -yOffset }, true),
                             _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - waferThickness)
                             );
                    await _currentTeacher.Accept();
                })
                .SetOnSearchScorchAction(() =>
                {
                    StepIndex++;
                    techMessager.RealeaseMessage("Совместите место прожига с перекрестием камеры и нажмите * чтобы продолжить", MessageType.Info);
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
                    techMessager.RealeaseMessage("Обучение отменено", MessageType.Exclamation);
                   // StopVideoCapture();
                    TeachingSteps = new();                    
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    Settings.Default.XOffset = _currentTeacher.GetParams()[0];
                    Settings.Default.YOffset = _currentTeacher.GetParams()[1];
                    Settings.Default.Save();
                    techMessager.RealeaseMessage("Новое значение установленно", MessageType.Exclamation);

                    //---Set new coordinate system
                    _coorSystem = GetCoorSystem();
                    //StopVideoCapture();
                    TeachingSteps = new();
                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            await _currentTeacher.StartTeach();
            _canTeach = true;
        }
        [ICommand]
        private async Task TeachScanatorHorizont()
        {
            var waferWidth = 48;
            var delta = 0.5F;
            var xLeft = delta;
            var xRight = waferWidth - delta;
            var waferHeight = 60;
            var tempX = 0F;
            var tempY = 0F;
            var zCamera = Settings.Default.ZeroFocusPoint - WaferThickness;
            var zLaser = Settings.Default.ZeroPiercePoint - WaferThickness;

            var tcb = LaserHorizontTeacher.GetBuilder();

            tcb.SetGoUnderCameraAction(() => Task.Run(async () =>
             {
                 _laserMachine.SetVelocity(Velocity.Fast);
                 await Task.WhenAll(
                 _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, waferWidth / 2, waferHeight / 2)),
                 _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera));
                 techMessager.RealeaseMessage("Выберете место на пластине для прожига горизонтальной линии", MessageType.Info);
             }))
                .SetGoAtFirstPointAction(async () =>
                {
                    _laserMachine.SetVelocity(Velocity.Fast);
                    await Task.WhenAll(
                        _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { Settings.Default.XOffset, Settings.Default.YOffset }, true),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser));

                    var matrix = new System.Drawing.Drawing2D.Matrix();
                    matrix.Rotate((float)Settings.Default.PazAngle * 180 / MathF.PI);
                    var points = new PointF[] { new PointF(xLeft - waferWidth / 2, 0), new PointF(xRight - waferWidth / 2, 0) };
                    matrix.TransformPoints(points);
                    tempX = points[0].X + waferWidth / 2;
                    tempY = points[0].Y;
                    await _laserMachine.PierceLineAsync(-waferWidth / 2, 0, waferWidth / 2, 0);

                    //await Task.WhenAll( 
                    //    _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)),
                    //    _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - WaferThickness));



                    await Task.WhenAll
                    (
                        _laserMachine.MoveAxInPosAsync(Ax.X, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)[0], true),
                        _laserMachine.MoveAxRelativeAsync(Ax.Y,-tempY - Settings.Default.YOffset,true),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera)
                        );

                    techMessager.RealeaseMessage("Установите перекрестие на первую точку линии и нажмите *", MessageType.Info);
                    tempX = points[1].X + waferWidth / 2;
                    tempY = -tempY + points[1].Y;
                })
                .SetGoAtSecondPointAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { XAxis.Position, YAxis.Position });
                    _laserMachine.SetVelocity(Velocity.Fast);

                    //await Task.WhenAll(
                    //    _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)),
                    //    _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera));

                    await Task.WhenAll
                   (
                       _laserMachine.MoveAxInPosAsync(Ax.X, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)[0], true),
                        _laserMachine.MoveAxRelativeAsync(Ax.Y, -tempY, true),
                       _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera)
                       );


                    techMessager.RealeaseMessage("Установите перекрестие на вторую точку линии и нажмите *", MessageType.Info);
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

                    var first = _coorSystem.FromGlobal(result[0], result[1]);
                    var second = _coorSystem.FromGlobal(result[2], result[3]);

                    double AC = second[0] - first[0];
                    double CB = second[1] - first[1];
                    Settings.Default.PazAngle = Math.Atan2(CB, AC);
                    Settings.Default.Save();
                    MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            await _currentTeacher.StartTeach();
            _canTeach = true;
        }
        [ICommand]
        private async void TeachOrthXY()
        {
            //TODO not all transformations are set on default
            _tempWaferTurn90 = WaferTurn90;
            WaferTurn90 = false;
            var waferThickness = WaferThickness;
            var zFocus = Settings.Default.ZeroFocusPoint;
            var dc = new AskThicknessVM { Thickness = waferThickness };
            new AskThicknesView { DataContext = dc }.ShowDialog();
            waferThickness = dc.Thickness;

            var matrixElements = ExtensionMethods.DeserilizeObject<float[]>(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS,"TeachingDeformation.json"));

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
                    await Task.WhenAll(
                        _laserMachine.MoveGpInPosAsync(Groups.XY, sys.ToGlobal(point.X, point.Y), true)/*,
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zFocus - waferThickness)*/).ConfigureAwait(false);
                    techMessager.RealeaseMessage("Совместите перекрестие визира с ориентиром и нажмите *", MessageType.Exclamation);
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

                    pureSystem.GetMainMatrixElements().SerializeObject(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS,"PureDeformation.json"));
                    teachSystem.GetMainMatrixElements().SerializeObject(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS,"TeachingDeformation.json"));

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
                    techMessager.RealeaseMessage("Установите подложку и нажмите * чтобы продолжить", MessageType.Info);
                }))
                .SetOnGoNAskFirstMarkerAction(() => Task.Run(async () =>
                {
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition);
                    TeachScaleMarkerEnable = true;
                    techMessager.RealeaseMessage("Подведите один из маркеров к ориентиру и нажмите * чтобы продолжить", MessageType.Info);
                }))
                .SetOnAskSecondMarkerAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { YAxis.Position });
                    techMessager.RealeaseMessage("Подведите второй маркер к этому ориентиру и нажмите * чтобы продолжить", MessageType.Info);
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
                    techMessager.RealeaseMessage("Обучение отменено", MessageType.Exclamation);
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(async () =>
                {
                    Settings.Default.CameraScale = _currentTeacher.GetParams()[0];
                    Settings.Default.Save();
                    techMessager.RealeaseMessage("Новое значение установленно", MessageType.Exclamation);
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
                    techMessager.RealeaseMessage($"Переместите координату {coordinate} до конца в отрицательную сторону и нажмите *", MessageType.Info);
                }))
                .SetOnAtPositiveEdgeAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { horizontal ? XAxis.Position : YAxis.Position });
                    techMessager.RealeaseMessage($"Переместите координату {coordinate} до конца в положительную сторону и нажмите *", MessageType.Info);
                }))
                .SetOnDimensionToughtAction(() => Task.Run(async () =>
                {
                    techMessager.RealeaseMessage("Обучение отменено", MessageType.Exclamation);
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
                    techMessager.RealeaseMessage("Новое значение установленно", MessageType.Exclamation);
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
            return _mainProcess?.Next();
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

        private CoorSystem<LMPlace> _testCoorSys; 

        public double TestPointX { get; set; } = 58;
        public double TestPointY { get; set; } = 46;

        [ICommand]
        private async Task TestPoint1()
        {
            //var wafer = new LaserWafer<MachineClassLibrary.Laser.Entities.Point>(new[] { new PPoint(TestPointX, TestPointY, 0, new MachineClassLibrary.Laser.Entities.Point(), "", 0) }, (60, 48));

            //var point = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, wafer[0].X, wafer[0].Y);

            //await _laserMachine.MoveGpInPosAsync(Groups.XY, point, true);
            await _testThreePointsProcess.TestPoint(TestPointX, TestPointY);
        }

        [ICommand]
        private async Task TestPierce()
        {
            await _laserMachine.PierceLineAsync(-5, 5, 5, 5);
            await _laserMachine.PierceLineAsync(-5, -5, 5, -5);
            await _laserMachine.PierceLineAsync(-5, -5, -5, 5);
            await _laserMachine.PierceLineAsync(5, -5, 5, 5);

        }
    }
    public enum Teacher
    {
        Corners,
        CameraOffset,
        ScanatorHorizont,
        OrthXY,
        CameraScale
    }
}