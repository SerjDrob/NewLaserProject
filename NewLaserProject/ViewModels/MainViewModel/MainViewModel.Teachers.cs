﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HandyControl.Data;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Miscellaneous;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Teachers;
using NewLaserProject.ViewModels.DialogVM;
using Dialog = HandyControl.Controls.Dialog;
using Growl = HandyControl.Controls.Growl;
using MsgBox = HandyControl.Controls.MessageBox;

namespace NewLaserProject.ViewModels
{

    public partial class MainViewModel
    {
        private bool _tempWaferTurn90;
        private Stateless.StateMachine<AppState, AppTrigger>.TriggerWithParameters<Teacher>? _startTeachTrigger;
        private CoorSystem<LMPlace> _testCoorSys;
        private double _angle;
        public double TeacherPointerX { get; set; }
        public double TeacherPointerY { get; set; }
        
        public bool TeacherPointerVisibility { get; set; } = false;
        public List<string> TeachingSteps { get; set; }
        public int StepIndex { get; set; }
        
        [ICommand]
        private async Task StartTeaching(Teacher teacher)
        {
            try
            {

                _startTeachTrigger ??= _appStateMachine.SetTriggerParameters<Teacher>(AppTrigger.StartLearning);
                await _appStateMachine.FireAsync(_startTeachTrigger, teacher);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Exception when start teaching.");
                throw;
            }

        }

        private async Task<ITeacher> TeachCameraOffsetAsync()
        {

            TeachingSteps = new()
            {
                "Установка подложки",
                "Выбор места прожига",
                "Совмещение прожига"
            };
            StepIndex = -1;

            StartVideoCapture();
            var teachPosition = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, 10, 10);
            var xOffset = _settingsManager.Settings.XOffset ?? throw new ArgumentNullException("XOffset is null");
            var yOffset = _settingsManager.Settings.YOffset ?? throw new ArgumentNullException("YOffset is null");
            var zCamera = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");
            var zLaser = _settingsManager.Settings.ZeroPiercePoint ?? throw new ArgumentNullException("ZeroPiercePoint is null");
            var waferThickness = WaferThickness;
            _settingsManager.Settings.OffsetPoints ??= new List<OffsetPoint>();
            var tcb = CameraOffsetTeacher.GetBuilder();

            tcb.SetOnGoLoadPointAction(() => Task.Run(async () =>
            {
                StepIndex++;
                _laserMachine.SetVelocity(Velocity.Service);
                Growl.Info(new GrowlInfo
                {
                    Message = "Установите подложку и нажмите * чтобы продолжить",
                    ShowDateTime = false,
                    StaysOpen = true
                });
            }))
                .SetOnGoUnderCameraAction(async () =>
                {
                    StepIndex++;
                    _laserMachine.SetVelocity(Velocity.Service);
                    
                    Growl.Info(new GrowlInfo
                    {
                        Message = "Выбирете место прожига и нажмите * чтобы продолжить",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                })
                .SetOnGoToShotAction(async () =>
                {
                    Growl.Clear();
                    _laserMachine.SetVelocity(Velocity.Service);
                    var xAct = _laserMachine.GetAxActual(Ax.X);
                    var yAct = _laserMachine.GetAxActual(Ax.Y);
                    try
                    {
                        _settingsManager.Settings.OffsetPoints.GetOffsetByCurCoor(xAct, yAct, ref xOffset, ref yOffset);
                    }
                    catch (InvalidOperationException)
                    {
                        xOffset = _settingsManager.Settings.XOffset ?? throw new NullReferenceException("XOffset is null in the settings");
                        yOffset = _settingsManager.Settings.YOffset ?? throw new NullReferenceException("YOffset is null in the settings");
                    }
                    catch(IndexOutOfRangeException)
                    {
                        xOffset = _settingsManager.Settings.XOffset ?? throw new NullReferenceException("XOffset is null in the settings");
                        yOffset = _settingsManager.Settings.YOffset ?? throw new NullReferenceException("YOffset is null in the settings");
                    }

                    await Task.WhenAll(
                            _laserMachine.MoveAxRelativeAsync(Ax.X, xOffset, true),
                            _laserMachine.MoveAxRelativeAsync(Ax.Y, yOffset, true),
                            _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser - waferThickness)
                            );

                    var defLaserParams = MiscExtensions
                           .DeserializeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);
                    var pen = defLaserParams.PenParams with
                    {
                        Freq = 30000,
                        MarkSpeed = 200,
                        ModDutyCycle = 10,
                        MarkLoop = 1,
                        QPulseWidth = 2
                    };
                    var hatch = defLaserParams.HatchParams;


                    _laserMachine.SetMarkParams(new(pen, hatch));

                    for (int i = 0; i < 2; i++)
                    {
                        await _laserMachine.PierceLineAsync(-0.5, 0, 0.5, 0);
                        await _laserMachine.PierceLineAsync(0, -0.5, 0, 0.5);
                    }


                    await _laserMachine.PierceLineAsync(-0.4, 0.4, 0.4, 0.4);
                    await _laserMachine.PierceLineAsync(-0.4, -0.4, 0.4, -0.4);
                    await _laserMachine.PierceLineAsync(-0.4, -0.4, -0.4, 0.4);
                    await _laserMachine.PierceLineAsync(0.4, -0.4, 0.4, 0.4);

                    _currentTeacher.SetParams(_laserMachine.GetAxActual(Ax.X), _laserMachine.GetAxActual(Ax.Y));
                    await Task.WhenAll(
                             _laserMachine.MoveAxRelativeAsync(Ax.X, -xOffset, true),
                             _laserMachine.MoveAxRelativeAsync(Ax.Y, -yOffset, true),
                             _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera - waferThickness)
                             );
                    await _currentTeacher.AcceptAsync();
                })
                .SetOnSearchScorchAction(() =>
                {
                    StepIndex++;
                    Growl.Info(new GrowlInfo
                    {
                        Message = "Совместите место прожига с перекрестием камеры и нажмите * чтобы продолжить",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                    return Task.CompletedTask;
                })
                .SetOnRequestPermissionToStartAction(() => Task.Run(async () =>
                {
                    if (MsgBox.Ask("Обучить смещение камеры от объектива лазера?", "Обучение") == MessageBoxResult.OK)
                    {
                        await _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        await _currentTeacher.DenyAsync();
                    }
                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(_laserMachine.GetAxActual(Ax.X), _laserMachine.GetAxActual(Ax.Y));

                    if (MsgBox.Ask($"Принять новое смещение камеры от объектива лазера {_currentTeacher}?", "Обучение") == MessageBoxResult.OK)
                    {
                        await _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        await _currentTeacher.DenyAsync();
                    }
                }))
                .SetOnBiasToughtAction(() => Task.Run(() =>
                {
                    Growl.Warning(new GrowlInfo
                    {
                        Message = "Обучение отменено",
                        ShowDateTime = false,
                        StaysOpen = false
                    });
                    TeachingSteps = new();
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    _settingsManager.Settings.XOffset = _currentTeacher.GetParams()[0];
                    _settingsManager.Settings.YOffset = _currentTeacher.GetParams()[1];
                    _settingsManager.Settings.OffsetPoints.Add(new(
                        _laserMachine.GetAxActual(Ax.X),
                        _laserMachine.GetAxActual(Ax.Y),
                        _currentTeacher.GetParams()[0],
                        _currentTeacher.GetParams()[1]
                        ));
                    _settingsManager.Save();
                    Growl.Info(new GrowlInfo
                    {
                        Message = "Новое значение установлено",
                        ShowDateTime = false,
                        StaysOpen = false
                    });
                    //---Set new coordinate system
                    _coorSystem = GetCoorSystem(AppPaths.PureDeformation);
                    TuneCoorSystem();
                    //StopVideoCapture();
                    TeachingSteps = new();
                    _canTeach = false;
                }));
            return await Task.FromResult(tcb.Build());
        }
        private async Task<ITeacher> TeachScanatorHorizontAsync()
        {
            var waferWidth = 48;
            var delta = 0.5F;
            var xLeft = delta;
            var xRight = waferWidth - delta;
            var waferHeight = 60;
            var tempX = 0F;
            var tempY = 0F;
            var zeroFocusPoint = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");
            var zeroPiercePoint = _settingsManager.Settings.ZeroPiercePoint ?? throw new ArgumentNullException("ZeroPiercePoint is null");
            var zCamera = zeroFocusPoint - WaferThickness;
            var zLaser = zeroPiercePoint - WaferThickness;

            var tcb = LaserHorizontTeacher.GetBuilder();

            tcb.SetGoUnderCameraAction(() => Task.Run(async () =>
             {
                 _laserMachine.SetVelocity(Velocity.Service);
                 var coors = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, waferWidth / 2, waferHeight / 2);
                 await Task.WhenAll(
                 _laserMachine.MoveAxInPosAsync(Ax.X, coors[0]),
                 _laserMachine.MoveAxInPosAsync(Ax.Y, coors[1]),
                 _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera)).ConfigureAwait(false);

                 Growl.Info(new GrowlInfo
                 {
                     Message = "Выберете место на пластине для прожига горизонтальной линии и нажмите *",
                     ShowDateTime = false,
                     StaysOpen = true
                 });

             }))
                .SetGoAtFirstPointAction(async () =>
                {
                    _laserMachine.SetVelocity(Velocity.Service);
                    var xOffset = _settingsManager.Settings.XOffset ?? throw new ArgumentNullException("XOffset is null");
                    var yOffset = _settingsManager.Settings.YOffset ?? throw new ArgumentNullException("YOffset is null");

                    await Task.WhenAll(
                        _laserMachine.MoveAxRelativeAsync(Ax.X, xOffset, true),
                        _laserMachine.MoveAxRelativeAsync(Ax.Y, yOffset, true),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zLaser)).ConfigureAwait(false);

                    var matrix = new System.Drawing.Drawing2D.Matrix();
                    matrix.Rotate((float)(_settingsManager.Settings.PazAngle ?? throw new ArgumentNullException("PAZAngle is null")) * 180 / MathF.PI);
                    var points = new PointF[] { new PointF(xLeft - waferWidth / 2, 0), new PointF(xRight - waferWidth / 2, 0) };
                    matrix.TransformPoints(points);
                    tempX = points[0].X + waferWidth / 2;
                    tempY = points[0].Y;

                    var defLaserParams = MiscExtensions
                          .DeserializeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);
                    var pen = defLaserParams.PenParams;
                    var hatch = defLaserParams.HatchParams;


                    _laserMachine.SetMarkParams(new(pen, hatch));


                    var result = await _laserMachine.PierceLineAsync(-waferWidth / 2, 0, waferWidth / 2, 0);

                    await Task.WhenAll
                    (
                        _laserMachine.MoveAxInPosAsync(Ax.X, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)[0], true),
                        _laserMachine.MoveAxRelativeAsync(Ax.Y, -tempY - (_settingsManager.Settings.YOffset ?? throw new ArgumentNullException("YOffset is null")), true),
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera)
                        ).ConfigureAwait(false);

                    Growl.Info(new GrowlInfo
                    {
                        Message = "Установите перекрестие на первую точку линии и нажмите *",
                        ShowDateTime = false,
                        StaysOpen = true
                    });

                    tempX = points[1].X + waferWidth / 2;
                    tempY = -tempY + points[1].Y;
                })
                .SetGoAtSecondPointAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams([XAxis.Position, YAxis.Position]);
                    _laserMachine.SetVelocity(Velocity.Service);

                    //await Task.WhenAll(
                    //    _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)),
                    //    _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera));

                    await Task.WhenAll
                   (
                       _laserMachine.MoveAxInPosAsync(Ax.X, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, tempX, waferHeight / 2)[0], true),
                        _laserMachine.MoveAxRelativeAsync(Ax.Y, -tempY, true),
                       _laserMachine.MoveAxInPosAsync(Ax.Z, zCamera)
                       );

                    Growl.Info(new GrowlInfo
                    {
                        Message = "Установите перекрестие на вторую точку линии и нажмите *",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                }))
                .SetOnRequestPermissionToStartAction(() => Task.Run(() =>
                {
                    if (MsgBox.Ask("Обучить горизонтальность сканатора?", "Обучение") == MessageBoxResult.OK)
                    {
                        _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        _currentTeacher.DenyAsync();
                    }
                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(() =>
                {
                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                    var result = _currentTeacher.GetParams();
                    var first = _coorSystem.FromGlobal(result[0], result[1]);
                    var second = _coorSystem.FromGlobal(result[2], result[3]);
                    double AC = second[0] - first[0];
                    double CB = second[1] - first[1];
                    _angle = Math.Atan2(CB, AC);
                    var alpha = _angle * 180 / Math.PI;
                    _currentTeacher.SetResult(alpha);

                    if (MsgBox.Ask($"Принять новое значение горизонтальности сканатора {_currentTeacher}?", "Обучение") == MessageBoxResult.OK)
                    {

                        _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        _currentTeacher.DenyAsync();
                    }
                    // Growl.Clear();
                }))
                .SetOnLaserHorizontToughtAction(() => Task.Run(() =>
                {
                    //MsgBox.Show("Обучение отменено", "Обучение");
                    Growl.Warning("Обучение отменено");
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {

                    //_laserMachine.SetSystemAngle(alpha);
                    _settingsManager.Settings.PazAngle = _angle;
                    _settingsManager.Save();
                    MsgBox.Info("Новое значение установлено", "Обучение");
                    _canTeach = false;
                }));
            return tcb.Build();
        }
        private async Task<ITeacher> TeachOrthXYAsync()
        {
            //TODO not all transformations are set on default
            _tempWaferTurn90 = WaferTurn90;
            WaferTurn90 = false;
            var waferThickness = WaferThickness;
            var zFocus = _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null");

            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Толщина подложки")
                .SetDataContext<AskThicknessVM>(vm => vm.Thickness = 0.5d)
                .GetCommonResultAsync<double>();

            if (result.Success)
            {
                waferThickness = result.CommonResult;
            }

            var matrixElements = MiscExtensions.DeserializeObject<float[]>(AppPaths.TeachingDeformation);

            var workMatrixBuilder = CoorSystem<LMPlace>.GetWorkMatrixSystemBuilder();
            workMatrixBuilder.SetWorkMatrix(new Matrix3x2(
                matrixElements[0],
                matrixElements[1],
                matrixElements[2],
                matrixElements[3],
                matrixElements[4],
                matrixElements[5]
                ));
            var sys = workMatrixBuilder.Build();


            using var wafer = new LaserWafer(_dxfReader.GetPoints(), (60, 48));//TODO situation when file isn't  correct or even absent

            var points = wafer.ToList() ?? throw new NullReferenceException();

            Guard.IsEqualTo(points.Count, 3, nameof(points));
            using var pointsEnumerator = points.GetEnumerator();

            var xyOrthBuilder = XYOrthTeacher.GetBuilder()
                .SetOnGoNextPointAction(() => Task.Run(async () =>
                {
                    pointsEnumerator.MoveNext();
                    var point = pointsEnumerator.Current;
                    _laserMachine.VelocityRegime = Velocity.Fast;
                    await Task.WhenAll(
                        _laserMachine.MoveGpInPosAsync(Groups.XY, sys.ToGlobal(point.X, point.Y), true)/*,
                        _laserMachine.MoveAxInPosAsync(Ax.Z, zFocus - waferThickness)*/).ConfigureAwait(false);

                    Growl.Info(new GrowlInfo
                    {
                        Message = "Совместите перекрестие визира с ориентиром и нажмите *",
                        StaysOpen = true,
                        ShowDateTime = false
                    });

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
                    if (MsgBox.Ask("Обучить координатную систему лазера?", "Обучение") == MessageBoxResult.OK)
                    {
                        _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        _currentTeacher.DenyAsync();
                    }

                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(() =>
                {
                    if (MsgBox.Ask($"Принять новое значения координат для матрицы преобразования {_currentTeacher}?", "Обучение") == MessageBoxResult.OK)
                    {
                        _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        _currentTeacher.DenyAsync();
                    }
                }))
                .SetOnXYOrthToughtAction(() => Task.Run(() =>
                {
                    MsgBox.Error("Обучение отменено", "Обучение");
                    WaferTurn90 = _tempWaferTurn90;
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    TeacherPointerVisibility = false;
                    Growl.Clear();
                    var resultPoints = _currentTeacher.GetParams();

                    var builder = CoorSystem<Place>.GetThreePointSystemBuilder();

                    builder.SetFirstPointPair(new((float)points[0].X, (float)points[0].Y), new((float)resultPoints[0], (float)resultPoints[1]))
                           .SetSecondPointPair(new((float)points[1].X, (float)points[1].Y), new((float)resultPoints[2], (float)resultPoints[3]))
                           .SetThirdPointPair(new((float)points[2].X, (float)points[2].Y), new((float)resultPoints[4], (float)resultPoints[5]));

                    //minus means direction of ordinate axis
                    var pureSystem = builder.FormWorkMatrix(0.001, -0.001).BuildPure();
                    var teachSystem = builder.FormWorkMatrix(1, 1).Build();

                    pureSystem.GetMainMatrixElements().SerializeObject(AppPaths.PureDeformation);
                    teachSystem.GetMainMatrixElements().SerializeObject(AppPaths.TeachingDeformation);

                    MsgBox.Info("Новое значение установлено", "Обучение");
                    WaferTurn90 = _tempWaferTurn90;

                    //---Set new coordinate system
                    _coorSystem = GetCoorSystem(AppPaths.PureDeformation);
                    TuneCoorSystem();
                    _canTeach = false;
                }));
            return xyOrthBuilder.Build();
        }
        private async Task<ITeacher> TeachCameraScaleAsync()
        {
            var teachPosition = new double[] { 1, 1 };
            Velocity tempVelocity = Velocity.Slow;
            var tcs = CameraScaleTeacher.GetBuilder()
                .SetOnRequestPermissionToStartAction(() => Task.Run(async () => //TODO looks like pornogrphy
                {
                    if (MsgBox.Ask("Обучить масштаб видео?", "Обучение") == MessageBoxResult.OK)
                    {
                        await _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        await _currentTeacher.DenyAsync();
                    }
                }))
                .SetOnGoLoadPointAction(() => Task.Run(async () =>
                {
                    tempVelocity = _laserMachine.SetVelocity(Velocity.Service);
                    await _laserMachine.GoThereAsync(LMPlace.Loading);
                    Growl.Info(new GrowlInfo
                    {
                        Message = "Установите подложку и нажмите * чтобы продолжить",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                }))
                .SetOnGoNAskFirstMarkerAction(() => Task.Run(async () =>
                {
                    await Task.WhenAll(
                        _laserMachine.MoveAxInPosAsync(Ax.X, teachPosition[0]),
                        _laserMachine.MoveAxInPosAsync(Ax.Y, teachPosition[1]));
                    _laserMachine.SetVelocity(Velocity.Fast);
                    _cameraVM.TeachScaleMarkerEnable = true;
                    Growl.Info(new GrowlInfo
                    {
                        Message = "Подведите один из маркеров к ориентиру и нажмите * чтобы продолжить",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                }))
                .SetOnAskSecondMarkerAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams([YAxis.Position]);
                    Growl.Info(new GrowlInfo
                    {
                        Message = "Подведите второй маркер к этому ориентиру и нажмите * чтобы продолжить",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                }))
                .SetOnRequestPermissionToAcceptAction(() => Task.Run(async () =>
                {
                    _cameraVM.TeachScaleMarkerEnable = false;
                    var diff = Math.Abs(YAxis.Position - _currentTeacher.GetParams()[0]);
                    var scale = diff / (ScaleMarkersRatioSecond - ScaleMarkersRatioFirst);
                    if (MsgBox.Ask($"Принять новый масштаб видео  {scale}?", "Обучение") == MessageBoxResult.OK)
                    {
                        _currentTeacher.SetParams(new double[] { scale });
                        await _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        await _currentTeacher.DenyAsync();
                    }

                }))
                .SetOnScaleToughtAction(() => Task.Run(async () =>
                {
                    Growl.Info("Обучение отменено");
                    _laserMachine.SetVelocity(tempVelocity);
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(async () =>
                {
                    _settingsManager.Settings.CameraScale = _currentTeacher.GetParams()[0];
                    _settingsManager.Save();
                    Growl.Info("Новое значение установлено");
                    _laserMachine.SetVelocity(tempVelocity);
                    _canTeach = false;
                }));
            return tcs.Build();
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
                    if (MsgBox.Ask($"Обучить габарит координаты {coordinate}?", "Обучение") == MessageBoxResult.OK)
                    {
                        await _currentTeacher.AcceptAsync();
                    }
                    else
                    {
                        await _currentTeacher.DenyAsync();
                    }
                }))
                .SetOnAtNegativeEdgeAction(() => Task.Run(async () =>
                {
                    Growl.Info(new GrowlInfo
                    {
                        Message = $"Переместите координату {coordinate} до конца в отрицательную сторону и нажмите *",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                }))
                .SetOnAtPositiveEdgeAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { horizontal ? XAxis.Position : YAxis.Position });
                    Growl.Info(new GrowlInfo
                    {
                        Message = $"Переместите координату {coordinate} до конца в положительную сторону и нажмите *",
                        ShowDateTime = false,
                        StaysOpen = true
                    });
                }))
                .SetOnDimensionToughtAction(() => Task.Run(async () =>
                {
                    Growl.Info("Обучение отменено");
                    _canTeach = false;
                }))
                .SetOnRequestAcceptionAction(() => Task.Run(async () =>
                 {
                     _currentTeacher.SetParams(new double[] { horizontal ? XAxis.Position : YAxis.Position });

                     if (MsgBox.Ask($"Принять новый габарит координаты {coordinate} {_currentTeacher}?", "Обучение") == MessageBoxResult.OK)
                     {
                         await _currentTeacher.AcceptAsync();
                     }
                     else
                     {
                         await _currentTeacher.DenyAsync();
                     }

                 }))
                .SetOnGiveResultAction(() => Task.Run(async () =>
                {
                    if (horizontal)
                    {
                        _settingsManager.Settings.XNegDimension = _currentTeacher.GetParams()[0];
                        _settingsManager.Settings.XPosDimension = _currentTeacher.GetParams()[1];
                    }
                    else
                    {
                        _settingsManager.Settings.YNegDimension = _currentTeacher.GetParams()[0];
                        _settingsManager.Settings.YPosDimension = _currentTeacher.GetParams()[1];
                    }
                    _settingsManager.Save();
                    Growl.Info("Новое значение установлено");
                    _canTeach = false;
                }))
                .Build();
            await _currentTeacher.StartTeachAsync();
            _canTeach = true;
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
        CameraGroupOffset,
        ScanatorHorizont,
        OrthXY,
        CameraScale, 
        ScanheadCalibration
    }
}
