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
using System.Windows.Media.Animation;
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
        public bool IsRightPanelVisible { get; set; } = true;
        public bool IsCentralPanelVisible { get; set; } = false;
        public bool IsLearningPanelVisible { get; set; } = false;
        public bool IsProcessPanelVisible { get; set; } = false;
        public string TechInfo { get; set; }
        public string IconPath { get; set; }
        public bool ProcessUnderCamera { get; set; } = false;
        public bool OnProcess { get; set; } = false;
        public MessageType CurrentMessageType { get; private set; } = MessageType.Empty;
        public BitmapImage CameraImage { get; set; }
        public AxisStateView XAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView YAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView ZAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
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
            InitAppState();
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
            InitAppState();

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
        private void ChangeViews(bool IsVideOnCenter)
        {
            if (IsVideOnCenter & RightSideVM is CameraVM)
            {
                ChangeViews();
            }
            else if(!IsVideOnCenter & CentralSideVM is CameraVM)
            {
                ChangeViews();
            }            
        }
        private void HideRightPanel(bool hide)
        {
            IsRightPanelVisible = !hide;

        }
        private void HideCentralPanel(bool hide)
        {
            IsCentralPanelVisible = !hide;

        }
        private void HideLearningPanel(bool hide)
        {
            IsLearningPanelVisible = !hide;

        }
        private void HideProcessPanel(bool hide)
        {
            IsProcessPanelVisible = !hide;

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
            ChangeViews();

            //var element = ProcessingObjects.ElementAt(15);
            //element.IsBeingProcessed = true;
            //IsBeingProcessedObject = element;
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
                case Key.A or Key.Z or Key.X or Key.C or Key.V or Key.B:
                    await moveAsync(key);
                    break;
                case Key.Tab when !key.IsRepeat:
                    await _laserMachine.MoveGpInPosAsync(Groups.XY, new double[] { 1, 1 });
                    break;
                case Key.E:
                    _laserMachine.SwitchOnValve(Valves.Light);
                    break;
                case Key.G when !key.IsRepeat:
                    await _laserMachine.GoThereAsync(LMPlace.Loading);
                    break;
                case Key.J:
                    break;
                case Key.K:
                    break;
                case Key.L:
                    break;
                case Key.Home when !key.IsRepeat:
                    {
                        try
                        {
                            await _laserMachine.GoHomeAsync().ConfigureAwait(false);
                            var corner = new double[] { Settings.Default.XLeftPoint, Settings.Default.YLeftPoint };
                            await _laserMachine.MoveGpInPosAsync(Groups.XY, corner).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                        techMessager.EraseMessage();
                    }
                    break;
            }
            key.Handled = true;

            async Task moveAsync(KeyEventArgs key)
            {
                var res = key.Key switch
                {
                    Key.A => (Ax.Y, AxDir.Pos),
                    Key.Z => (Ax.Y, AxDir.Neg),
                    Key.X => (Ax.X, AxDir.Neg),
                    Key.C => (Ax.X, AxDir.Pos),
                    Key.V => (Ax.Z, AxDir.Pos),
                    Key.B => (Ax.Z, AxDir.Neg),
                };

                if (!key.IsRepeat)
                {
                    if (VelocityRegime != Velocity.Step) _laserMachine.GoWhile(res.Item1, res.Item2);
                    if (VelocityRegime == Velocity.Step)
                    {
                        var step = (res.Item2 == AxDir.Pos ? 1 : -1) * 0.005;
                        await _laserMachine.MoveAxRelativeAsync(res.Item1, step, false);

                    }
                }
                key.Handled = true;
                return;
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
            key.Handled = true;
        }

        [ICommand]
        private void ChangeVelocity()
        {
            VelocityRegime = VelocityRegime switch
            {
                Velocity.Slow => Velocity.Fast,
                Velocity.Fast => Velocity.Slow,
                _ => Velocity.Fast
            };
#if PCIInserted
            _laserMachine.SetVelocity(VelocityRegime);
#endif
        }

        [ICommand]
        private void SetStepVelocity()
        {
            VelocityRegime = Velocity.Step;
#if PCIInserted
            _laserMachine.SetVelocity(Velocity.Slow);
#endif
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
                    (Ax.X, - 6.4),//TODO sign depends on relations between driver and encoder. Put it to JSON
                    (Ax.Y, - 6.4),
                    (Ax.Z, 0)
                    });


            
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
                plsOutMde = (int)PlsOutMode.OUT_DIR_DIR_NEG,//.OUT_DIR,
                reset = (int)HomeRst.HOME_RESET_EN,
                acc = Settings.Default.ZAcc,
                dec = Settings.Default.ZDec,
                ppu = Settings.Default.ZPPU,
                homeVelLow = Settings.Default.ZVelLow,
                homeVelHigh = Settings.Default.ZVelService
            };


            var gpXYpar = xpar;


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

                _laserMachine.AddGroup(Groups.XY, new[] { Ax.X, Ax.Y });
                //_laserMachine.SetGroupConfig(0, gpXYpar);
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

            //TODO put it to JSON
            _laserMachine.ConfigureHomingForAxis(Ax.X)
                .SetHomingDirection(AxDir.Neg)
                .SetHomingMode(HmMode.MODE6_Lmt_Ref)
                .SetHomingVelocity(Settings.Default.XVelService)
                .Configure();

            _laserMachine.ConfigureHomingForAxis(Ax.Y)
                .SetHomingDirection(AxDir.Neg)
                .SetHomingMode(HmMode.MODE6_Lmt_Ref)
                .SetHomingVelocity(Settings.Default.YVelService)
                .Configure();

            _laserMachine.ConfigureHomingForAxis(Ax.Z)
                .SetHomingDirection(AxDir.Neg)
                .SetHomingVelocity(/*Settings.Default.ZVelService*/1)
                .SetPositionAfterHoming(1)
                .Configure();

            _laserMachine.ConfigureValves(
                    new()
                    {
                        [Valves.Light] = (Ax.Y, Do.Out4)
                    }                
                );

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
