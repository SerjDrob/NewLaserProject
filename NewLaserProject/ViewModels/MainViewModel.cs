using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Properties;
using NewLaserProject.Views;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NewLaserProject.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel
    {
        private readonly LaserMachine _laserMachine;
        private DxfReader _dxfReader;
        public BitmapImage CameraImage { get; set; }
        public AxisStateView XAxis { get; set; }
        public AxisStateView YAxis { get; set; }
        public AxisStateView ZAxis { get; set; }
        public LayersProcessingModel LPModel { get; set; }
        public TechWizardViewModel TWModel { get; set; }
        public bool LeftCornerBtnVisibility { get; set; } = false;
        public bool RightCornerBtnVisibility { get; set; } = false;
        public bool WaferContourVisibility { get; set; } = false;
        public bool IsFileSettingsEnable { get; set; } = false;

        private string _pierceSequenceJson = string.Empty;
        public string FileName { get; set; } = "open new file";
        private ITeacher _currentTeacher;
        private bool _canTeach = false;
        public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();
        public Velocity VelocityRegime { get; private set; } = Velocity.Fast;

        public MainViewModel(LaserMachine laserMachine)
        {
            _laserMachine = laserMachine;
            _laserMachine.OnVideoSourceBmpChanged += _laserMachine_OnVideoSourceBmpChanged;
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;
            Settings.Default.Save();
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
            using var wafer = new LaserWafer<Circle>(_dxfReader.GetCircles(),(60,48));

        }
        [ICommand]
        public void Test()
        {
            LeftCornerBtnVisibility ^= true;
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
                _dxfReader = new DxfReader(FileName);
                LayGeoms = new LayGeomAdapter(_dxfReader.Document).CalcGeometry();
            }
            if (File.Exists(FileName))
            {
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


            var tcb = TeachCameraBias.GetBuilder();
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
                    if(MessageBox.Show("Обучить смещение камеры от объектива лазера?", "Обучение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                    MessageBox.Show("Новое значение установленно","Обучение", MessageBoxButton.OK, MessageBoxImage.Information);
                    _canTeach = false;
                }));
            _currentTeacher = tcb.Build();
            _canTeach = true;
        }
        [ICommand]
        private void TeachScanatorHorizont() { }
        [ICommand]
        private void TeachOrthXY() { }
        [ICommand]
        private void TeachNext()
        {
            if (_canTeach)
            {
                _currentTeacher?.Next().Wait();
            }
        }
        [ICommand]
        private void MachineSettings() 
        {
            var dataContext = new MachineSettingsViewModel(/*XAxis.Position,YAxis.Position,ZAxis.Position*/1, 2, 3);
            dataContext.CopyFromSettings();
            new MachineSettingsView 
            {
                DataContext = dataContext
            }.ShowDialog();
            dataContext.CopyToSettings();
            Settings.Default.Save();
          //  ImplementMachineSettings();
        }
        private void ImplementMachineSettings()
        {
            var xpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 50,
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
                maxVel = 50,
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
                maxVel = 50,
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
                {Velocity.Fast, Settings.Default.ZVelHigh},
                {Velocity.Slow, Settings.Default.ZVelLow},
                {Velocity.Service, Settings.Default.ZVelService}
            };
                       

            _laserMachine.ConfigureVelRegimes(new Dictionary<Ax, Dictionary<Velocity, double>>
            {
                {Ax.X, XVelRegimes},
                {Ax.Y, YVelRegimes},
                {Ax.Z, ZVelRegimes},
            });


            _laserMachine.SetConfigs(new (Ax axis, MotionDeviceConfigs configs)[]
            {
                (Ax.X, xpar),
                (Ax.Y, ypar),
                (Ax.Z, zpar),
            });

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
        }
    }

}
