using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Classes.Process;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.Properties;
using NewLaserProject.UserControls;
using NewLaserProject.Views;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NewLaserProject.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel
    {
        const string APP_SETTINGS_FOLDER = "AppSettings";


        private InfoMessager techMessager;
        private readonly LaserMachine _laserMachine;
        public bool IsLaserInitialized { get; set; } = false;
        public bool IsMotionInitialized { get; set; } = false;
        public string VideoScreenMessage { get; set; } = "";
        public string TechInfo { get; set; }
        public string IconPath { get; set; }
        public bool ProcessUnderCamera { get; set; } = false;
        public bool OnProcess { get; set; } = false;
        public MessageType CurrentMessageType { get; private set; } = MessageType.Empty;
        public BitmapImage CameraImage { get; set; }
        public AxisStateView XAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView YAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView ZAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        //public LayersProcessingModel LPModel { get; set; }
        //public TechWizardViewModel TWModel { get; set; }
        public bool LeftCornerBtnVisibility { get; set; } = false;
        public bool RightCornerBtnVisibility { get; set; } = false;
        public bool TeachScaleMarkerEnable { get; private set; } = false;
        public double ScaleMarkersRatioFirst { get; private set; } = 0.1;
        public double ScaleMarkersRatioSecond { get => 1 - ScaleMarkersRatioFirst; }

        private string _pierceSequenceJson = string.Empty;
        public Velocity VelocityRegime { get; private set; } = Velocity.Fast;
        public AppSettingsVM AppSngsVM { get; set; }
        public object RightSideVM { get; set; }
        public object CentralSideVM { get; set; }
        private CameraVM _cameraVM;

        private readonly DbContext _db;
        private readonly IMediator _mediator;

        public ObservableCollection<string> CameraCapabilities { get; set; }
        public int CameraCapabilitiesIndex { get; set; }
        public bool ShowVideo { get; set; }
        //---------------------------------------------
        private CoorSystem<LMPlace> _coorSystem;
        private ITeacher _currentTeacher;
        private bool _canTeach = false;

        private IProcess _mainProcess;

        //---------------------------------------------
        public MainViewModel(DbContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
            _coorSystem = GetCoorSystem();
            InitViews();
        }
        public MainViewModel(LaserMachine laserMachine, DbContext db, IMediator mediator)
        {
            _laserMachine = laserMachine;
            IsMotionInitialized = _laserMachine.IsMotionDeviceInit;
            _db = db;
            _mediator = mediator;

            techMessager = new();
            techMessager.PublishMessage += TechMessager_PublishMessage;
            var workingDirectory = Environment.CurrentDirectory;
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;
            _coorSystem = GetCoorSystem();
            ImplementMachineSettings();
            var count = _laserMachine.GetVideoCaptureDevicesCount();
            CameraCapabilities = new(_laserMachine.AvaliableVideoCaptureDevices[0].Item2);
            CameraCapabilitiesIndex = Settings.Default.PreferedCameraCapabilities;
            _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
            _laserMachine.InitMarkDevice(Directory.GetCurrentDirectory())
                .ContinueWith(t => 
                {
                    if (t.Status==TaskStatus.RanToCompletion)
                    {
                        IsLaserInitialized = t.Result;
                    }
                    else if (t.Status==TaskStatus.Faulted)
                    {
                        IsLaserInitialized = false;
                        MessageBox.Show(t.Exception?.InnerException?.Message,"Ошибка инициализации", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }); 
            TuneMachineFileView();
            techMessager.RealeaseMessage("Необходимо выйти в исходное положение. Клавиша Home", MessageType.Danger);
            InitViews();
            //AppSngsVM = new(_db);
        }
        [ICommand]
        private void DbLoad()
        {
            if(LaserDbVM is null) LaserDbVM = new(_db);
        }
        [ICommand]
        private void AppSettingsOpen()
        {
            var defLaserParams = ExtensionMethods
                .DeserilizeObject<MarkLaserParams>(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "DefaultLaserParams.json"));

            if (AppSngsVM is null) AppSngsVM = new(_db, defLaserParams)
            {
                IsMirrored = Settings.Default.WaferMirrorX,
                IsRotated = Settings.Default.WaferAngle90
            };
        }
        [ICommand]
        private void AppSettingsClose()
        {
            if (AppSngsVM is not null)
            {
                AppSngsVM.SaveDbChanges();
                var defProcFilter = new DefaultProcessFilterDTO
                {
                    LayerFilterId = AppSngsVM.DefaultTechSelector.DefLayerFilter.Id,
                    MaterialId = AppSngsVM.DefaultMaterial.Id,
                    EntityType=(uint)AppSngsVM.DefaultEntityType,
                    DefaultWidth=AppSngsVM.DefaultWidth,
                    DefaultHeight=AppSngsVM.DefaultHeight
                };

                var defLaserParams = AppSngsVM.MarkSettingsViewModel.GetLaserParams();

                defProcFilter.SerializeObject(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "DefaultProcessFilter.json"));
                defLaserParams.SerializeObject(Path.Combine(ProjectPath.GetFolderPath("AppSettings"), "DefaultLaserParams.json"));

                Settings.Default.WaferMirrorX = AppSngsVM.IsMirrored;
                Settings.Default.WaferAngle90 = AppSngsVM.IsRotated;
                Settings.Default.Save();
            }
        }

        [ICommand]
        private void ChangeViews()
        {
            (RightSideVM, CentralSideVM) = (CentralSideVM, RightSideVM);
        }

        private void InitViews()
        {
            _openedFileVM = new FileVM(48, 60);
            CentralSideVM = _openedFileVM;
            _cameraVM = new CameraVM();
            RightSideVM = _cameraVM;
#if PCIInserted
            _laserMachine.OnBitmapChanged += _cameraVM.OnVideoSourceBmpChanged;
            _cameraVM.VideoClicked += _cameraVM_VideoClicked;
#endif
        }

        private void _cameraVM_VideoClicked(object? sender, (double x, double y) e)
        {
            var k = 1280d / 1024;//TODO It must depend on current camera capabilities
            var imgX = e.x * Settings.Default.CameraScale * k;
            var imgY = e.y * Settings.Default.CameraScale;
            _laserMachine.MoveGpRelativeAsync(Groups.XY, new double[] { -imgX, imgY },true);//TODO fix it. it smells
        }

        [ICommand]
        private void CameraCapabilitiesChanged()
        {
            _laserMachine.StopCamera();
            _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
        }

        private void TechMessager_PublishMessage(string message, string iconPath, MessageType icon)
        {
            TechInfo = message;
            IconPath = iconPath;
            CurrentMessageType = icon;
        }


        private void _laserMachine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            try
            {
                switch (e.Axis)
                {
                    case Ax.X:
                        XAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        LaserViewfinderX = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderLaser, XAxis.Position, YAxis.Position)[0] * FileScale ?? 0;
                        CameraViewfinderX = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderCamera, XAxis.Position, YAxis.Position)[0] * FileScale ?? 0;
                        break;
                    case Ax.Y:
                        YAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        LaserViewfinderY = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderLaser, XAxis.Position, YAxis.Position)[1] * FileScale ?? 0;
                        CameraViewfinderY = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderCamera, XAxis.Position, YAxis.Position)[1] * FileScale ?? 0;
                        break;
                    case Ax.Z:
                        ZAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        break;
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }

        private void _laserMachine_OnVideoSourceBmpChanged(object? sender, VideoCaptureEventArgs e)
        {
            CameraImage = e.Image;
        }


        private void StartVideoCapture()
        {
            _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
            ShowVideo = true;
        }
        private void StopVideoCapture()
        {
            _laserMachine.StopCamera();
            ShowVideo = false;
        }

        [ICommand]
        private async Task Test()
        {
            IsLaserInitialized ^= true;
            //ChangeViews();
            //var topologySize = _dxfReader.GetSize();
            //var path = Path.Combine(ProjectPath.GetFolderPath("TempFiles"));
            //var objs = _dxfReader.GetAllDxfCurves2(path, "PAZ");
            //var wafer = new LaserWafer<DxfCurve>(objs, topologySize);
            //var waferPoints = new LaserWafer<MachineClassLibrary.Laser.Entities.Point>(_dxfReader.GetPoints(), topologySize);
            //wafer.Scale(1F / FileScale);
            //waferPoints.Scale(1F / FileScale);
            //if (WaferTurn90) wafer.Turn90();
            //if (WaferTurn90) waferPoints.Turn90();
            //if (MirrorX) wafer.MirrorX();
            //if (MirrorX) waferPoints.MirrorX();

            //_pierceSequenceJson = File.ReadAllText(Path.Combine(ProjectPath.GetFolderPath("TechnologyFiles"),"CircleListing.json"));
            //var coorSystem = _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);

            //var points = waferPoints.Cast<PPoint>();

            //_mainProcess = new ThreePointProcess(wafer, points, _pierceSequenceJson, _laserMachine,
            //            coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
            //            Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle);
            //_mainProcess.CreateProcess();
            //string str = _mainProcess.ToString();
        }



        [ICommand]
        private void OpenLayersProcessing()
        {
            if (File.Exists(FileName))
            {
                _dxfReader = new IMDxfReader(FileName);
                new LayersView()
                {
                    DataContext = new LayersProcessingModel(_dxfReader)
                }.Show();

            }
            else
            {
                MessageBox.Show("Имя файла неверно или файл не существует", "Внимание", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #region Driving the machine        

        [ICommand]
        private async Task KeyDown(object args)
        {
            var key = (KeyEventArgs)args;
            if (key.OriginalSource is TextBoxBase) return;
                     
            switch (key.Key)
            {
                case Key.Tab:
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, new double[] { 1, 1 });
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
                    await _laserMachine.GoThereAsync(LMPlace.Loading);
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
                case Key.Home:
                    {
                        await _laserMachine.GoHomeAsync().ConfigureAwait(false);
                        var corner = new double[] {Settings.Default.XLeftPoint, Settings.Default.YLeftPoint };
                        await _laserMachine.MoveGpInPosAsync(Groups.XY,corner).ConfigureAwait(false);
                        techMessager.EraseMessage();
                    }
                    break;
            }
        }

        [ICommand]
        private async Task KeyUp(object args)
        {
            var key = (KeyEventArgs)args;
            if (key.OriginalSource is TextBoxBase) return;

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

        [ICommand]
        private void ChangeVelocity()
        {
            VelocityRegime = VelocityRegime switch
            {
                Velocity.Slow => Velocity.Fast,
                Velocity.Fast => Velocity.Slow
            };
            _laserMachine.SetVelocity(VelocityRegime);
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
                    (Ax.X, 6.4),
                    (Ax.Y, 6.4),
                    (Ax.Z, 0)
                    });

            //_laserMachine.ConfigureAxesGroups(new Dictionary<Groups, Ax[]>
            //    {
            //        {Groups.XY, new[] {Ax.X, Ax.Y}}
            //    });


            _laserMachine.AddGroup(Groups.XY, new[] { Ax.X, Ax.Y });

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
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)PlsOutMode.OUT_DIR,
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


            _laserMachine.ConfigureGeometry(new Dictionary<LMPlace, (Ax, double)[]>
            {
                [LMPlace.Loading] = new[] { (Ax.X, Settings.Default.XLoad), (Ax.Y, Settings.Default.YLoad) } //,
            });


            _laserMachine.ConfigureHomingForAxis(Ax.X)
                .SetHomingMode(HmMode.MODE6_Lmt_Ref)
                .SetHomingVelocity(Settings.Default.XVelService)
                .Configure();

            _laserMachine.ConfigureHomingForAxis(Ax.Y)
                .SetHomingMode(HmMode.MODE6_Lmt_Ref)
                .SetHomingVelocity(Settings.Default.YVelService)
                .Configure();

            _laserMachine.ConfigureHomingForAxis(Ax.Z)
                .SetHomingVelocity(/*Settings.Default.ZVelService*/1)
                .SetPositionAfterHoming(1)
                .Configure();

#endif
        }
        private CoorSystem<LMPlace> GetCoorSystem()
        {
            var matrixElements = ExtensionMethods.DeserilizeObject<float[]>(ProjectPath.GetFilePathInFolder(ProjectFolders.APP_SETTINGS,"PureDeformation.json") 
                ?? throw new NullReferenceException("CoorSystem in the file is invalid"));

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
            TuneCoorSystem(sys);
            return sys;
        }
        private void TuneCoorSystem(CoorSystem<LMPlace> coorSystem)
        {
            var dx = Settings.Default.XRightPoint - Settings.Default.XLeftPoint;
            var dy = Settings.Default.YRightPoint - Settings.Default.YLeftPoint;

            var angle = Math.Atan2(dy, dx);

            coorSystem.BuildRelatedSystem()
                      .Rotate(angle)
                      .Translate(Settings.Default.XLeftPoint, Settings.Default.YLeftPoint)
                      .Build(LMPlace.FileOnWaferUnderCamera);

            coorSystem.BuildRelatedSystem()
                      .Rotate(angle)
                      .Translate(Settings.Default.XLeftPoint + Settings.Default.XOffset, Settings.Default.YLeftPoint + Settings.Default.YOffset)
                      .Build(LMPlace.FileOnWaferUnderLaser);


            coorSystem.SetRelatedSystem(LMPlace.Loading, 50, 20);
            coorSystem.SetRelatedSystem(LMPlace.UnderLaser, 1, 2);
            coorSystem.SetRelatedSystem(LMPlace.LeftCorner, Settings.Default.XLeftPoint, Settings.Default.YLeftPoint);
            coorSystem.SetRelatedSystem(LMPlace.RightCorner, 1, 2);
        }
    }

}
