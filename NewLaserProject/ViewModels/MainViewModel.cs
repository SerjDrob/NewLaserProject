﻿using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Properties;
using NewLaserProject.Views;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NewLaserProject.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel
    {
        private readonly LaserMachine _laserMachine;
        private readonly string _projectDirectory;
        public string VideoScreenMessage { get; set; } = "";
        public int FileScale { get; set; } = 1000;
        public bool MirrorX { get; set; } = true;
        public bool WaferTurn90 { get; set; } = true;
        public BitmapImage CameraImage { get; set; }
        public AxisStateView XAxis { get; set; } = new AxisStateView(0, false, false, true, false);
        public AxisStateView YAxis { get; set; } = new AxisStateView(0, false, false, true, false);
        public AxisStateView ZAxis { get; set; } = new AxisStateView(0, false, false, true, false);
        public LayersProcessingModel LPModel { get; set; }
        public TechWizardViewModel TWModel { get; set; }
        public bool LeftCornerBtnVisibility { get; set; } = false;
        public bool RightCornerBtnVisibility { get; set; } = false;
        public bool WaferContourVisibility { get; set; } = false;
        public bool IsFileSettingsEnable { get; set; } = false;

        private string _pierceSequenceJson = string.Empty;
        public string FileName { get; set; } = "open new file";

        public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();
        public Velocity VelocityRegime { get; private set; } = Velocity.Fast;



        //---------------------------------------------
        private IDxfReader _dxfReader;
        private CoorSystem<LMPlace> _coorSystem;
        private ITeacher _currentTeacher;
        private bool _canTeach = false;
        //---------------------------------------------

        public MainViewModel(LaserMachine laserMachine)
        {
            var workingDirectory = Environment.CurrentDirectory;
            _projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            _laserMachine = laserMachine;
            _laserMachine.OnVideoSourceBmpChanged += _laserMachine_OnVideoSourceBmpChanged;
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;
            Settings.Default.Save();//wtf?
            _coorSystem = GetCoorSystem();
            ImplementMachineSettings();
        }
        public MainViewModel()
        {

        }
        private void _laserMachine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            switch (e.Axis)
            {
                case Ax.X:
                    XAxis = new AxisStateView(e.Position, e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                    break;
                case Ax.Y:
                    YAxis = new AxisStateView(e.Position, e.PLmt, e.PLmt, e.MotionDone, e.MotionStart);
                    break;
                case Ax.Z:
                    ZAxis = new AxisStateView(e.Position, e.PLmt, e.PLmt, e.MotionDone, e.MotionStart);
                    break;
            }
        }

        private void _laserMachine_OnVideoSourceBmpChanged(object? sender, BitmapEventArgs e)
        {
            CameraImage = e.Image;
        }

        [ICommand]
        private void StartProcess()
        {
            //is dxf valid?
            using var wafer = new LaserWafer<Circle>(_dxfReader.GetCircles(), (60, 48));
            if (WaferTurn90) wafer.Turn90();
            if (MirrorX) wafer.MirrorX();
            wafer.Scale(FileScale);
            var process = new LaserProcess<Circle>(wafer, _pierceSequenceJson, _laserMachine, _coorSystem);
            process.Start();
        }
        [ICommand]
        public void Test()
        {
            var system = new CoorSystem<LMPlace>(new System.Drawing.Drawing2D.Matrix(1, 21, 34, 4, 5, 6));
            system.GetMainMatrixElements().SerializeObject($"{_projectDirectory}/AppSettings/CoorSystem.json");
            var m = (float[])ExtensionMethods.DeserilizeObject<float[]>($"{_projectDirectory}/AppSettings/CoorSystem.json");
            //LeftCornerBtnVisibility ^= true;
        }

        [ICommand]
        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "d:\\";
            openFileDialog.Filter = "dxf files (*.dxf)|*.dxf";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if ((bool)openFileDialog.ShowDialog())
            {
                //Get the path of specified file
                FileName = openFileDialog.FileName;
               
            }
            if (File.Exists(FileName))
            { 
                _dxfReader = new DxfReader(FileName);
                LayGeoms = new LayGeomAdapter(_dxfReader).LayerGeometryCollections;
                IsFileSettingsEnable = true;
                LPModel = new(FileName);
                TWModel = new();
                LPModel.ObjectChosenEvent += TWModel.SetObjectsTC;
            }
            else
            {
                IsFileSettingsEnable = false;
            }

        }

        [ICommand]
        private void OpenLayersProcessing()
        {
            if (File.Exists(FileName))
            {
                new LayersView()
                {
                    DataContext = new LayersProcessingModel(FileName)
                }.Show();

            }
            else
            {
                MessageBox.Show("Имя файла неверно или файл не существует", "Внимание", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        [ICommand]
        private void LeftWaferCornerTeach() { }
        [ICommand]
        private void RightWaferCornerTeach() { }
        [ICommand]
        private void TeachCameraOffset()
        {
            var teachPosition = new double[] { 1, 1 };
            double xOffset = 1;
            double yOffset = 1;


            var tcb = CameraOffsetTeacher.GetBuilder();
            tcb.SetOnGoLoadPointAction(() => _laserMachine.GoThereAsync(LMPlace.Loading))
                .SetOnGoUnderCameraAction(() => _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition))
                .SetOnGoToSootAction(() => Task.Run(async () =>
                {
                    await _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { xOffset, yOffset }, true);
                    await _laserMachine.PiercePointAsync();
                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
                    await _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { -xOffset, -yOffset }, true);
                }))
                .SetOnRequestPermissionToStartAction(() => Task.Run(() =>
                {
                    if (MessageBox.Show("Обучить смещение камеры от объектива лазера?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                    if (MessageBox.Show($"Принять новое смещение камеры от объектива лазера {_currentTeacher}?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _currentTeacher.Accept();
                    }
                    else
                    {
                        _currentTeacher.Deny();
                    }
                }))
                .SetOnBiasToughtAction(() => Task.Run(() =>
                {
                    MessageBox.Show("Обучение отменено", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    Settings.Default.XOffset = _currentTeacher.GetParams()[0];
                    Settings.Default.YOffset = _currentTeacher.GetParams()[1];
                    Settings.Default.Save();
                    MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            _canTeach = true;
        }
        [ICommand]
        private void TeachScanatorHorizont()
        {
            var waferWidth = 60;
            var delta = 5;
            var xLeft = delta;
            var xRight = waferWidth-delta;
            var waferHeight = 48;
            float tempX = 0;

            var tcb = LaserHorizontTeacher.GetBuilder();

            tcb.SetGoUnderCameraAction(async () =>
                {
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(waferWidth / 2, waferHeight / 2));
                    VideoScreenMessage = "Выберете место на пластине для прожига горизонтальной линии";
                })
                .SetGoAtFirstPointAction(() => Task.Run(async () =>
                {
                    await _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { Settings.Default.XOffset, Settings.Default.YOffset }, true);
                    var matrix = new System.Drawing.Drawing2D.Matrix();
                    matrix.Rotate((float)Settings.Default.PazAngle);
                    var points = new PointF[] { new PointF(xLeft - waferWidth/2, 0), new PointF(xRight - waferWidth / 2, 0) };
                    matrix.TransformPoints(points);
                    tempX = points[0].X;
                    await _laserMachine.PierceLineAsync(-waferWidth / 2, 0, waferWidth / 2, 0);
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(tempX, waferHeight / 2));
                    VideoScreenMessage = "Установите перекрестие на первую точку линии и нажмите *";
                    tempX= points[1].X;
                }))
                .SetGoAtSecondPointAction(() => Task.Run(async () =>
                {
                    _currentTeacher.SetParams(new double[] { XAxis.Position, YAxis.Position });
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(tempX, waferHeight / 2));
                    VideoScreenMessage = "Установите перекрестие на вторую точку линии и нажмите *";
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
                }))
                .SetOnLaserHorizontToughtAction(() => Task.Run(() =>
                {
                    MessageBox.Show("Обучение отменено", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    var result = _currentTeacher.GetParams();
                    var point1 = new PointF((float)result[0], (float)result[1]);
                    var point2 = new PointF((float)result[2], (float)result[3]);
                    double AC = point2.X - point1.X;
                    double CB = point2.Y - point1.Y;
                    Settings.Default.PazAngle = Math.Atan2(CB, AC);
                    Settings.Default.Save();
                    MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            _canTeach = true;
        }
        [ICommand]
        private void TeachOrthXY(List<PointF> points)
        {
            Guard.IsEqualTo(points.Count, 3, nameof(points));
            using var pointsEnumerator = points.GetEnumerator();
            var tcb = XYOrthTeacher.GetBuilder();
            tcb.SetOnGoNextPointAction(() => Task.Run(async () =>
                {
                    pointsEnumerator.MoveNext();
                    var point = pointsEnumerator.Current;
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(point.X, point.Y), true);
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
                    _currentTeacher.SetParams(XAxis.Position, YAxis.Position);
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
                    _canTeach = false;
                }))
                .SetOnHasResultAction(() => Task.Run(() =>
                {
                    var resultPoints = _currentTeacher.GetParams();
                    _coorSystem = new CoorSystem<LMPlace>(
                        first: (new(points[0].X, points[0].Y), new((float)resultPoints[0], (float)resultPoints[1])),
                        second: (new(points[1].X, points[1].Y), new((float)resultPoints[2], (float)resultPoints[3])),
                        third: (new(points[2].X, points[2].Y), new((float)resultPoints[4], (float)resultPoints[5])));
                    TuneCoorSystem(_coorSystem);
                    _coorSystem.SerializeObject("filePath");
                    MessageBox.Show("Новое значение установленно", "Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            _canTeach = true;
        }
        [ICommand]
        private void TeachNext()
        {
            if (_canTeach)
            {
                _currentTeacher?.Next().Wait();
            }
        }
        [ICommand]
        private void TeachDeny()
        {
            if (_canTeach)
            {
                _currentTeacher?.Deny().Wait();
            }
        }

        #region Driving the machine        

        [ICommand]
        private async Task KeyDown(object args)
        {
            var key = (KeyEventArgs)args;
            switch (key.Key)
            {
                case Key.Tab:
                    break;
                case Key.A:
                    _laserMachine.GoWhile(Ax.Y, AxDir.Pos);
                    break;
                case Key.B:
                    _laserMachine.GoWhile(Ax.Z, AxDir.Neg);
                    break;
                case Key.C:
                    _laserMachine.GoWhile(Ax.X, AxDir.Pos);
                    break;
                case Key.E:
                    break;
                case Key.G:
                    break;
                case Key.J:
                    break;
                case Key.K:
                    break;
                case Key.L:
                    break;
                case Key.V:
                    _laserMachine.GoWhile(Ax.Z, AxDir.Pos);
                    break;
                case Key.X:
                    _laserMachine.GoWhile(Ax.X, AxDir.Neg);
                    break;
                case Key.Z:
                    _laserMachine.GoWhile(Ax.Y, AxDir.Neg);
                    break;
            }
        }

        [ICommand]
        private async Task KeyUp(object args)
        {
            var key = (KeyEventArgs)args;
            switch (key.Key)
            {
                case Key.Tab:
                    break;
                case Key.A:
                    _laserMachine.Stop(Ax.Y);
                    break;
                case Key.B:
                    _laserMachine.Stop(Ax.Z);
                    break;
                case Key.C:
                    _laserMachine.Stop(Ax.X);
                    break;
                case Key.E:
                    break;
                case Key.G:
                    break;
                case Key.J:
                    break;
                case Key.K:
                    break;
                case Key.L:
                    break;
                case Key.V:
                    _laserMachine.Stop(Ax.Z);
                    break;
                case Key.X:
                    _laserMachine.Stop(Ax.X);
                    break;
                case Key.Z:
                    _laserMachine.Stop(Ax.Y);
                    break;
            }
        }

        #endregion

        [ICommand]
        private void MachineSettings()
        {
            var dataContext = new MachineSettingsViewModel(XAxis.Position, YAxis.Position, ZAxis.Position);
            dataContext.CopyFromSettings();
            new MachineSettingsView
            {
                DataContext = dataContext
            }.ShowDialog();
            dataContext.CopyToSettings();
            Settings.Default.Save();
            ImplementMachineSettings();
        }
        private void ImplementMachineSettings()
        {
#if PCIInserted
            _laserMachine.ConfigureAxes(new (Ax, double)[]
                    {
                    (Ax.X, 12.8),
                    (Ax.Y, 12.8),
                    (Ax.Z, 0)                    
                    });

            _laserMachine.ConfigureAxesGroups(new Dictionary<Groups, Ax[]>
                {
                    {Groups.XY, new[] {Ax.X, Ax.Y}}
                });

            //_laserMachine.ConfigureValves(new Dictionary<Valves, (Ax, Do)>
            //    {
            //        {Valves.Blowing, (Ax.Z, Do.Out6)},
            //        {Valves.ChuckVacuum, (Ax.Z, Do.Out4)},
            //        {Valves.Coolant, (Ax.U, Do.Out4)},
            //        {Valves.SpindleContact, (Ax.U, Do.Out5)}
            //    });

            //_laserMachine.SwitchOffValve(Valves.Blowing);
            //_laserMachine.SwitchOffValve(Valves.ChuckVacuum);
            //_laserMachine.SwitchOffValve(Valves.Coolant);
            //_laserMachine.SwitchOffValve(Valves.SpindleContact);

            //_laserMachine.ConfigureSensors(new Dictionary<Sensors, (Ax, Di, Boolean, string)>
            //    {
            //        {Sensors.Air, (Ax.Z, Di.In1, false, "Воздух")},
            //        {Sensors.ChuckVacuum, (Ax.X, Di.In2, false, "Вакуум")},
            //        {Sensors.Coolant, (Ax.U, Di.In2, false, "СОЖ")},
            //        {Sensors.SpindleCoolant, (Ax.Y, Di.In2, false, "Охлаждение шпинделя")}
            //    });
            var xpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 30,
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)PlsOutMode.OUT_DIR,
                reset = (int)HomeRst.HOME_RESET_EN,
                acc = Settings.Default.XAcc,
                dec = Settings.Default.XDec,
                ppu = Settings.Default.XPPU,
                plsInMde = (int)PlsInMode.AB_4X,
                homeVelLow = Settings.Default.XVelLow,
                homeVelHigh = Settings.Default.XVelService
            };
            var ypar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 30,
                plsOutMde = (int)PlsOutMode.OUT_DIR_ALL_NEG,
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                reset = (int)HomeRst.HOME_RESET_EN,
                acc = Settings.Default.YAcc,
                dec = Settings.Default.YDec,
                ppu = Settings.Default.YPPU,
                plsInMde = (int)PlsInMode.AB_4X,
                homeVelLow = Settings.Default.YVelLow,
                homeVelHigh = Settings.Default.YVelService
            };
            var zpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 8,
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)PlsOutMode.OUT_DIR,
                reset = (int)HomeRst.HOME_RESET_EN,
                acc = Settings.Default.ZAcc,
                dec = Settings.Default.ZDec,
                ppu = Settings.Default.ZPPU,
                homeVelLow = Settings.Default.ZVelLow,
                homeVelHigh = Settings.Default.ZVelService
            };

            var XVelRegimes = new Dictionary<Velocity, double>
            {
                {Velocity.Fast, Settings.Default.XVelHigh},
                {Velocity.Slow, Settings.Default.XVelLow},
                {Velocity.Service, Settings.Default.XVelService}
            };

            var YVelRegimes = new Dictionary<Velocity, double>
            {
                {Velocity.Fast, Settings.Default.YVelHigh},
                {Velocity.Slow, Settings.Default.YVelLow},
                {Velocity.Service, Settings.Default.YVelService}
            };

            var ZVelRegimes = new Dictionary<Velocity, double>
            {
                {Velocity.Fast,1/* Settings.Default.ZVelHigh*/},
                {Velocity.Slow, Settings.Default.ZVelLow},
                {Velocity.Service, Settings.Default.ZVelService}
            };


            _laserMachine.ConfigureVelRegimes(new Dictionary<Ax, Dictionary<Velocity, double>>
            {
                {Ax.X, XVelRegimes},
                {Ax.Y, YVelRegimes},
                {Ax.Z, ZVelRegimes},
            });


            try
            {
                _laserMachine.SetConfigs(new (Ax axis, MotionDeviceConfigs configs)[]
                    {
                        (Ax.X, xpar),
                        (Ax.Y, ypar),
                        (Ax.Z, zpar),
                    });
            }
            catch (Exception ex)
            {

                //throw;
            }

            _laserMachine.SetVelocity(VelocityRegime);

            //BCCenterXView = Settings.Default.XDisk;
            //BCCenterYView = Settings.Default.YObjective + Settings.Default.DiskShift;
            //CCCenterXView = Settings.Default.XObjective;
            //CCCenterYView = Settings.Default.YObjective;
            //ZBladeTouchView = Settings.Default.ZTouch;

            //_laserMachine.ConfigureGeometry(new Dictionary<Place, (Ax, double)[]>
            //{
            //        [Place.BladeChuckCenter] = new[]{ (Ax.X, Settings.Default.XDisk), (Ax.Y, Settings.Default.YObjective + Settings.Default.DiskShift)},
            //    {
            //        Place.CameraChuckCenter,
            //        new[] {(Ax.X, Settings.Default.XObjective), (Ax.Y, Settings.Default.YObjective)}
            //    },
            //    {Place.Loading, new[] {(Ax.X, Settings.Default.XLoad), (Ax.Y, Settings.Default.YLoad)}},
            //    {Place.ZBladeTouch, new (Ax, double)[] {(Ax.Z, Settings.Default.ZTouch)}},
            //    {Place.ZFocus, new (Ax, double)[] {(Ax.Z, Settings.Default.ZObjective)}}
            //});

            //_machine.ConfigureGeometry(new Dictionary<Place, double>
            //    {{Place.ZBladeTouch, Settings.Default.ZTouch}}
            //);

            //_machine.ConfigureDoubleFeatures(new Dictionary<MFeatures, double>
            //{
            //    {MFeatures.CameraBladeOffset, Settings.Default.DiskShift},
            //    {MFeatures.ZBladeTouch, Settings.Default.ZTouch},
            //    {MFeatures.CameraFocus, 3}
            //});

            //_machine.SetBridgeOnSensors(Sensors.ChuckVacuum, Settings.Default.VacuumSensorDsbl);
            //_machine.SetBridgeOnSensors(Sensors.Coolant, Settings.Default.CoolantSensorDsbl);
            //_machine.SetBridgeOnSensors(Sensors.Air, Settings.Default.AirSensorDsbl);
            //_machine.SetBridgeOnSensors(Sensors.SpindleCoolant, Settings.Default.SpindleCoolantSensorDsbl);  
#endif
        }
        private CoorSystem<LMPlace> GetCoorSystem()
        {
            var matrixElements = (float[])ExtensionMethods.DeserilizeObject<float[]>($"{_projectDirectory}/AppSettings/CoorSystem.json");
            var sys = new CoorSystem<LMPlace>(new System.Drawing.Drawing2D.Matrix(
                matrixElements[0],
                matrixElements[1],
                matrixElements[2],
                matrixElements[3],
                matrixElements[4],
                matrixElements[5]
                ));
            TuneCoorSystem(sys);
            return sys;
        }
        private void TuneCoorSystem(CoorSystem<LMPlace> coorSystem)
        {
            coorSystem.SetRelatedSystem(LMPlace.Loading, 1, 2);
            coorSystem.SetRelatedSystem(LMPlace.UnderLaser, 1, 2);
            coorSystem.SetRelatedSystem(LMPlace.LeftCorner, 1, 2);
            coorSystem.SetRelatedSystem(LMPlace.RightCorner, 1, 2);
        }
    }

}
