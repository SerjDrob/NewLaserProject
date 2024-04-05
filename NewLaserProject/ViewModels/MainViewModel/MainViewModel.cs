using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using HandyControl.Data;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.VideoCapture;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.LogSinks.ConsoleSink;
using NewLaserProject.Classes.LogSinks.RepositorySink;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.Properties;
using PropertyChanged;
using MsgBox = HandyControl.Controls.MessageBox;

namespace NewLaserProject.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    public partial class MainViewModel
    {
        private readonly LaserMachine _laserMachine;
        public bool IsLaserInitialized { get; set; } = false;
        public bool IsMotionInitialized { get; set; } = false;
        public bool IsRightPanelVisible { get; set; } = true;
        public bool IsCentralPanelVisible { get; set; } = false;
        public bool IsLearningPanelVisible { get; set; } = false;
        public bool IsProcessPanelVisible { get; set; } = false;
        public string TechInfo { get; set; }
        public string IconPath { get; set; }
        public bool OnProcess { get; set; } = false;
        public MessageType CurrentMessageType { get; private set; } = MessageType.Empty;
        public BitmapImage CameraImage { get; set; }
        public AxisStateView XAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView YAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView ZAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public double ScaleMarkersRatioFirst { get; private set; } = 0.1;
        public double ScaleMarkersRatioSecond => 1 - ScaleMarkersRatioFirst;

        private string _pierceSequenceJson = string.Empty;
        public Velocity VelocityRegime { get; private set; } = Velocity.Fast;
        public object RightSideVM { get; set; }
        public object CentralSideVM { get; set; }
        public MechanicVM MechTableVM { get; set; }
        public WorkTimeStatisticsVM StatisticsVM { get; set; }
        public bool MotionDeviceOk { get; set; }
        public bool LaserBoardOk { get; set; }
        public bool LaserSourceOk { get; set; }
        public bool VideoCaptureDeviceOk { get; set; }
        public bool PWMDeviceOk { get; set; }

        //private readonly WorkTimeLogger? _workTimeLogger;
        private CameraVM _cameraVM;

        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubject<IProcessNotify> _subjMediator;
        public ObservableCollection<string> CameraCapabilities { get; set; }
        public int CameraCapabilitiesIndex { get; set; }
        public bool ShowVideo { get; set; }
        //---------------------------------------------
        private CoorSystem<LMPlace> _coorSystem;
        private ITeacher _currentTeacher;
        private bool _canTeach = false;

        private IProcess? _mainProcess;
        private double _waferAngle;
        private readonly Serilog.ILogger _logger;
        private readonly ISettingsManager<LaserMachineSettings> _settingsManager;

        public MainViewModel(LaserMachine laserMachine, IMediator mediator,
            IServiceProvider serviceProvider, /*ILoggerProvider loggerProvider*/ Serilog.ILogger logger,
            ISubject<IProcessNotify> subjMediator)
        {
            _logger = logger;// loggerProvider.CreateLogger("MainVM");
            _laserMachine = laserMachine;
            _laserMachine.OfType<DeviceStateChanged>()
                .Subscribe(s =>
                {
                    LaserSourceOk = _laserMachine.LaserSourceOk;
                    LaserBoardOk = _laserMachine.LaserBoardOk;
                    MotionDeviceOk = _laserMachine.MotionDeviceOk;
                    PWMDeviceOk = _laserMachine.PWMDeviceOk;
                    VideoCaptureDeviceOk = _laserMachine.VideoCaptureDeviceOk;
                });
            _laserMachine.OfType<SensorStateChanged>()
                .Subscribe(s =>
                {
                    switch (s.Sensor)
                    {
                        case LaserSensor.Air:
                            IsAirPressureOK = s.state;
                            break;
                        case LaserSensor.LaserSourceFault:
                            IsLaserSourceFault = s.state;
                            break;
                        default:
                            break;
                    }
                });
            IsMotionInitialized = _laserMachine.IsMotionDeviceInit;
            _mediator = mediator;
            _subjMediator = subjMediator;
            _serviceProvider = serviceProvider;
            _settingsManager = _serviceProvider.GetRequiredService<ISettingsManager<LaserMachineSettings>>();
            var workingDirectory = Environment.CurrentDirectory;
            _laserMachine.CameraPlugged += _laserMachine_CameraPlugged;
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;
            StatisticsVM = _serviceProvider.GetRequiredService<WorkTimeStatisticsVM>();
            //_workTimeLogger = _serviceProvider.GetService<WorkTimeLogger>();

            _coorSystem = GetCoorSystem(AppPaths.PureDeformation);
            TuneCoorSystem();
            ImplementMachineSettings();

            _laserMachine.InitMarkDevice(Directory.GetCurrentDirectory())
                .ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        IsLaserInitialized = t.Result;
                    }
                    else if (t.Status == TaskStatus.Faulted)
                    {
                        IsLaserInitialized = false;
                        MsgBox.Error(t.Exception?.InnerException?.Message);
                    }
                });

            var defLaserParams = ExtensionMethods
                .DeserilizeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);

            _laserMachine.SetMarkParams(defLaserParams);

            InitViews();
            InitAppState();
            InitCommands();
            _signalColumn?.TurnOnLight(LightColumn.Light.Red);
            _laserMachine.StartMonitoringState();
            MechTableVM = new();
            _logger.Information(RepoSink.Start, RepoSink.App);
            //_logger.Log(LogLevel.Information, "App started");
            WpfConsole = _serviceProvider.GetRequiredService<WpfConsoleSink>();
        }


        public WpfConsoleSink WpfConsole { get; set; }
        private void _laserMachine_CameraPlugged(object? sender, EventArgs e)
        {
            //_laserMachine.StopCamera();
            CameraCapabilities = new(_laserMachine.AvailableVideoCaptureDevices[0].Item2);
            CameraCapabilitiesIndex = _settingsManager.Settings.PreferredCameraCapabilities ?? throw new ArgumentNullException("PreferredCameraCapabilities is null");
            _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
        }

        private object _tempVM;
        private LightColumn _signalColumn;

        public bool IsMechViewChecked
        {
            get;
            set;
        }
        public bool IsAirPressureOK { get; set; }
        public bool IsLaserSourceFault { get; set; }

        [ICommand]
        private void ChangeMechView()
        {
            if (_tempVM is null)
            {
                if (CentralSideVM is FileVM) (CentralSideVM, _tempVM) = (MechTableVM, CentralSideVM);
                else if (RightSideVM is FileVM) (RightSideVM, _tempVM) = (MechTableVM, RightSideVM);
            }
            else
            {
                if (CentralSideVM is CameraVM) (RightSideVM, _tempVM) = (_tempVM, null);
                else if (RightSideVM is CameraVM) (CentralSideVM, _tempVM) = (_tempVM, null);
            }
            IsMechViewChecked ^= true;
        }
        public void OnInitialized()
        {
            Growl.Warning(new GrowlInfo
            {
                Message = "Необходимо выйти в исходное положение. Клавиша Home",
                StaysOpen = true,
                ShowDateTime = false
            });
        }

        [ICommand]
        private async Task CheckHatch()
        {

            //var reader = new IMDxfReader("D:/Test.dxf");
            //var curve = reader.GetAllCurves().Single();
            //var inflateCurve = curve.PObject.InflateCurve(200);
            //reader.WriteCurveToFile("D:/TestInflate.dxf", inflateCurve.First(), true);
        }


        [ICommand]
        private void DbLoad()//TODO bad method
        {
            LaserDbVM = _serviceProvider.GetService<LaserDbViewModel>();
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
            else if (!IsVideOnCenter & CentralSideVM is CameraVM)
            {
                ChangeViews();
            }
        }
        private void HideRightPanel(bool hide) => IsRightPanelVisible = !hide;
        private void HideCentralPanel(bool hide) => IsCentralPanelVisible = !hide;
        private void HideLearningPanel(bool hide) => IsLearningPanelVisible = !hide;
        private void HideProcessPanel(bool hide) => IsProcessPanelVisible = !hide;
        private void InitViews()
        {
            try
            {
                WaferWidth = _settingsManager.Settings.WaferWidth ?? Settings.Default.WaferWidth;
                WaferHeight = _settingsManager.Settings.WaferHeight ?? Settings.Default.WaferHeight;
                WaferThickness = _settingsManager.Settings.WaferThickness ?? Settings.Default.WaferThickness;

                _openedFileVM = new FileVM(WaferWidth, WaferHeight, _subjMediator);
                _openedFileVM.CanUndoChanged += _openedFileVM_CanUndoChanged;
                _openedFileVM.OnFileClicked += _openedFileVM_OnFileClicked;
                CentralSideVM = _openedFileVM;


                if ((_laserMachine.AvailableVideoCaptureDevices?.Count ?? 0) > 0)
                {
                    CameraCapabilities = new(_laserMachine.AvailableVideoCaptureDevices[0].Item2);
                    CameraCapabilitiesIndex = _settingsManager.Settings.PreferredCameraCapabilities ?? throw new ArgumentNullException("PreferredCameraCapabilities is null");
                    _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
                }
                else
                {
                    _logger.ForContext<MainViewModel>().Information("There is no available video capture devices");
                }


                _cameraVM = new CameraVM(_subjMediator);

                RightSideVM = _cameraVM;
#if PCIInserted
                _laserMachine.OnBitmapChanged += _cameraVM.OnVideoSourceBmpChanged;
                _cameraVM.VideoClicked += _cameraVM_VideoClicked;
#endif
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Throwed the exception in the {nameof(InitViews)} method.");
                _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(InitViews)} method.");
                throw;
            }
        }

        private async void _openedFileVM_OnFileClicked(object? sender, System.Windows.Point e)
        {
            if (XAxis.MotionDone && YAxis.MotionDone && !IsProcessing)
            {
                var result = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, e.X, e.Y);
                _laserMachine.SetVelocity(Velocity.Service);
                try
                {
                    await Task.WhenAll(_laserMachine.MoveAxInPosAsync(Ax.X, result[0]),
                                       _laserMachine.MoveAxInPosAsync(Ax.Y, result[1]))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                }
                _laserMachine.SetVelocity(VelocityRegime);
            }
        }
        private void _openedFileVM_CanUndoChanged(object? sender, bool e) => CanUndoCut = e;

        private async void _cameraVM_VideoClicked(object? sender, (double x, double y) e)
        {
            if (IsProcessing) return;
            var caps = CameraCapabilities[CameraCapabilitiesIndex].Split(" ");

            if (double.TryParse(caps[0], out var xRatio) && double.TryParse(caps[2], out var yRatio))
            {
                var k = xRatio / yRatio;
                var scale = _settingsManager.Settings.CameraScale ?? throw new ArgumentNullException("CameraScale is null");
                var offset = new[] { e.x * scale * k, -e.y * scale };
                try
                {
                    await Task.WhenAll(
                        _laserMachine.MoveAxRelativeAsync(Ax.X, offset[0], true),
                        _laserMachine.MoveAxRelativeAsync(Ax.Y, offset[1], true)
                        ).ConfigureAwait(false);
                    //await _laserMachine.MoveGpRelativeAsync(Groups.XY, offset, true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                }
            }
        }


        [ICommand]
        private void UndoRemoveSelection()
        {
            _openedFileVM?.UndoRemoveSelection();
        }

        private void _laserMachine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            try
            {
                switch (e.Axis)
                {
                    case Ax.X:
                        XAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        LaserViewfinderX = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderLaser, XAxis.Position, YAxis.Position)[0] * DefaultFileScale ?? 0;
                        CameraViewfinderX = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderCamera, XAxis.Position, YAxis.Position)[0] * DefaultFileScale ?? 0;
                        break;
                    case Ax.Y:
                        YAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        LaserViewfinderY = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderLaser, XAxis.Position, YAxis.Position)[1] * DefaultFileScale ?? 0;
                        CameraViewfinderY = _coorSystem?.FromSub(LMPlace.FileOnWaferUnderCamera, XAxis.Position, YAxis.Position)[1] * DefaultFileScale ?? 0;
                        break;
                    case Ax.Z:
                        ZAxis = new AxisStateView(Math.Round(e.Position, 3), Math.Round(e.CmdPosition, 3), e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                        break;
                }
                //MechTableVM?.SetCoordinates(XAxis.Position - 85.876, YAxis.Position - 51.945);
                var xoffset = _settingsManager.Settings.XOffset ?? 0;
                var yoffset = _settingsManager.Settings.YOffset ?? 0;
                MechTableVM?.SetCoordinates(XAxis.Position + xoffset, YAxis.Position + yoffset);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Swallowed the exception in {nameof(_laserMachine_OnAxisMotionStateChanged)}");
                _logger.ForContext<MainViewModel>().Error(ex, $"Swallowed the exception in {nameof(_laserMachine_OnAxisMotionStateChanged)}");
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
        private void LasSettings() => _laserMachine.SetDevConfig();

        [ICommand]
        private void CameraSettings() => _laserMachine.InvokeSettings();

        private void ImplementMachineSettings()
        {
#if PCIInserted

            var axesConfigs = ExtensionMethods
                .DeserilizeObject<LaserMachineAxesConfiguration>(AppPaths.AxesConfigs);

            var machineconfigs = ExtensionMethods
                .DeserilizeObject<LaserMachineConfiguration>(AppPaths.MachineConfigs);

            Guard.IsNotNull(axesConfigs, nameof(axesConfigs));
            Guard.IsNotNull(machineconfigs, nameof(machineconfigs));


            //axesConfigs.SerializeObject(AppPaths.AxesConfigs);


            _laserMachine.SetVideoMirror(axesConfigs.XMirrorCamera, axesConfigs.YMirrorCamera);

            var xpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 30,
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)(axesConfigs.XRightDirection ? PlsOutMode.OUT_DIR : PlsOutMode.OUT_DIR_DIR_NEG),
                reset = axesConfigs.XHomeReset,
                acc = _settingsManager.Settings.XAcc ?? throw new ArgumentNullException("XAcc is null"),
                dec = _settingsManager.Settings.XDec ?? throw new ArgumentNullException("XDec is null"),
                ppu = _settingsManager.Settings.XPPU ?? throw new ArgumentNullException("XPPU is null"),//4005,// Settings.Default.XPPU*2,//TODO fix it !!!!
                denominator = 4,
                plsInMde = (int)PlsInMode.AB_4X,
                homeVelLow = _settingsManager.Settings.XVelLow ?? throw new ArgumentNullException("XVelLow is null"),
                homeVelHigh = _settingsManager.Settings.XVelService ?? throw new ArgumentNullException("XVelService is null")
            };
            var ypar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 30,
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)(axesConfigs.YRightDirection ? PlsOutMode.OUT_DIR : PlsOutMode.OUT_DIR_DIR_NEG),
                reset = axesConfigs.YHomeReset,
                acc = _settingsManager.Settings.YAcc ?? throw new ArgumentNullException("YAcc is null"),
                dec = _settingsManager.Settings.YDec ?? throw new ArgumentNullException("YDec is null"),
                ppu = _settingsManager.Settings.YPPU ?? throw new ArgumentNullException("XAcc is null"),//3993,//Settings.Default.YPPU*2,
                denominator = 4,
                plsInMde = (int)PlsInMode.AB_4X,
                homeVelLow = _settingsManager.Settings.YVelLow ?? throw new ArgumentNullException("YVelLow is null"),
                homeVelHigh = _settingsManager.Settings.YVelService ?? throw new ArgumentNullException("YVelService is null")
            };
            var zpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 8,
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)(axesConfigs.ZRightDirection ? PlsOutMode.OUT_DIR : PlsOutMode.OUT_DIR_DIR_NEG),
                reset = axesConfigs.ZHomeReset,
                acc = _settingsManager.Settings.ZAcc ?? throw new ArgumentNullException("ZAcc is null"),
                dec = _settingsManager.Settings.ZDec ?? throw new ArgumentNullException("ZDec is null"),
                ppu = _settingsManager.Settings.ZPPU ?? throw new ArgumentNullException("ZPPU is null"),
                homeVelLow = _settingsManager.Settings.ZVelLow ?? throw new ArgumentNullException("ZVelLow is null"),
                homeVelHigh = _settingsManager.Settings.ZVelService ?? throw new ArgumentNullException("ZVelService is null")
            };

            try
            {
                _laserMachine.AddAxis(Ax.X, axesConfigs.XLine)
                    .WithConfigs(xpar)
                    .WithVelRegime(Velocity.Fast, _settingsManager.Settings.XVelHigh ?? throw new ArgumentNullException("ZVelService is null"))
                    .WithVelRegime(Velocity.Slow, _settingsManager.Settings.XVelLow ?? throw new ArgumentNullException("ZVelService is null"))
                    .WithVelRegime(Velocity.Service, _settingsManager.Settings.XVelService ?? throw new ArgumentNullException("ZVelService is null"))
                    .Build();

                _laserMachine.AddAxis(Ax.Y, axesConfigs.YLine)
                    .WithConfigs(ypar)
                    .WithVelRegime(Velocity.Fast, _settingsManager.Settings.YVelHigh ?? throw new ArgumentNullException("ZVelService is null"))
                    .WithVelRegime(Velocity.Slow, _settingsManager.Settings.YVelLow ?? throw new ArgumentNullException("ZVelService is null"))
                    .WithVelRegime(Velocity.Service, _settingsManager.Settings.YVelService ?? throw new ArgumentNullException("ZVelService is null"))
                    .Build();

                _laserMachine.AddAxis(Ax.Z, axesConfigs.ZLine)
                    .WithConfigs(zpar)
                    .WithVelRegime(Velocity.Fast, _settingsManager.Settings.ZVelHigh ?? throw new ArgumentNullException("ZVelService is null"))
                    .WithVelRegime(Velocity.Slow, _settingsManager.Settings.ZVelLow ?? throw new ArgumentNullException("ZVelService is null"))
                    .WithVelRegime(Velocity.Service, _settingsManager.Settings.ZVelService ?? throw new ArgumentNullException("ZVelService is null"))
                    .Build();

                _laserMachine.AddAxis(Ax.U, 0d).WithConfigs(xpar).Build();

                _laserMachine.AddGroup(Groups.XY, Ax.X, Ax.Y);

                _laserMachine.ConfigureGeometryFor(LMPlace.Loading)
                    .SetCoordinateForPlace(Ax.X, _settingsManager.Settings.XLoad ?? throw new ArgumentNullException("XLoad is null"))
                    .SetCoordinateForPlace(Ax.Y, _settingsManager.Settings.YLoad ?? throw new ArgumentNullException("YLoad is null"))
                    .Build();

                _laserMachine.ConfigureHomingForAxis(Ax.X)
                    .SetHomingDirection((AxDir)axesConfigs.XHomeDirection)
                    .SetHomingMode((HmMode)axesConfigs.XHomeMode)
                    .SetPositionAfterHoming(_settingsManager.Settings.XLeftPoint ?? throw new ArgumentNullException("XLeftPoint is null"))
                    .SetHomingVelocity(_settingsManager.Settings.XVelService ?? throw new ArgumentNullException("XVelService is null"))
                    .Configure();

                _laserMachine.ConfigureHomingForAxis(Ax.Y)
                    .SetHomingDirection((AxDir)axesConfigs.YHomeDirection)
                    .SetHomingMode((HmMode)axesConfigs.YHomeMode)
                    .SetPositionAfterHoming(_settingsManager.Settings.YLeftPoint ?? throw new ArgumentNullException("YLeftPoint is null"))
                    .SetHomingVelocity(_settingsManager.Settings.YVelService ?? throw new ArgumentNullException("YVelService is null"))
                    .Configure();

                _laserMachine.ConfigureHomingForAxis(Ax.Z)
                    .SetHomingDirection((AxDir)axesConfigs.ZHomeDirection)
                    .SetHomingVelocity(_settingsManager.Settings.ZVelService ?? throw new ArgumentNullException("ZVelService is null"))
                    .SetPositionAfterHoming((_settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null")) - WaferThickness)
                    .Configure();

                //_laserMachine.ConfigureValves(
                //        new()
                //        {
                //            [Valves.BlueLight] = (Ax.Y, Do.Out4),
                //            [Valves.GreenLight] = (Ax.Y, Do.Out5),
                //            [Valves.RedLight] = (Ax.U, Do.Out4),
                //            [Valves.YellowLight] = (Ax.U, Do.Out5),
                //            [Valves.Light] = (Ax.Y, Do.Out4) // TODO fix it!
                //        }
                //    );


                if (machineconfigs.IsPCI1240U || machineconfigs.IsPCI1245E || machineconfigs.IsMOCKBOARD)
                {
                    var switcher = PCI1240ValveSwitcher.GetSwitcherBuilder()
                        .AddValve(Valves.BlueLight, Ax.Y, Do.Out4)
                        .AddValve(Valves.GreenLight, Ax.Y, Do.Out5)
                        .AddValve(Valves.RedLight, Ax.U, Do.Out4)
                        .AddValve(Valves.YellowLight, Ax.U, Do.Out5)
                        .AddValve(Valves.Light, Ax.Y, Do.Out4)
                        .Build();
                    _laserMachine.ConfigureValves(switcher);
                    var sensors = PCI1240SensorsDetector.GetSensorsDetectorBuilder()
                        .AddSensor(LaserSensor.Air,Ax.X,Di.In1)
                        .Build();
                    _laserMachine.ConfigureSensors(sensors);
                }
                else if(machineconfigs.IsPCIE1245)
                {
                    var switcher = PCIE1245ValveSwitcher.GetSwitcherBuilder()
                        .AddValve(Valves.BlueLight, 0)
                        .AddValve(Valves.GreenLight, 1)
                        .AddValve(Valves.RedLight, 2)
                        .AddValve(Valves.YellowLight, 3)
                        .AddValve(Valves.Light, 4)
                        .Build(); 
                    _laserMachine.ConfigureValves(switcher);
                    var sensors = PCIE1245SensorsDetector.GetSensorsDetectorBuilder()
                        .AddSensor(LaserSensor.Air, 0)
                        .AddSensor(LaserSensor.LaserSourceFault, 1)
                        .AddSensor(LaserSensor.LaserCoolantFault, 2)
                        .AddSensor(LaserSensor.LaserOnEmission,3)
                        .Build();
                    _laserMachine.ConfigureSensors(sensors);
                }

                _signalColumn = new LightColumn();
                _signalColumn.AddLight(LightColumn.Light.Red, () => _laserMachine.SwitchOnValve(Valves.RedLight), () => _laserMachine.SwitchOffValve(Valves.RedLight));
                _signalColumn.AddLight(LightColumn.Light.Green, () => _laserMachine.SwitchOnValve(Valves.GreenLight), () => _laserMachine.SwitchOffValve(Valves.GreenLight));
                _signalColumn.AddLight(LightColumn.Light.Yellow, () => _laserMachine.SwitchOnValve(Valves.YellowLight), () => _laserMachine.SwitchOffValve(Valves.YellowLight));
                _signalColumn.AddLight(LightColumn.Light.Blue, () => _laserMachine.SwitchOnValve(Valves.BlueLight), () => _laserMachine.SwitchOffValve(Valves.BlueLight));

            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Throwed the exception in the {nameof(ImplementMachineSettings)} method.");
                _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(ImplementMachineSettings)} method.");
                throw;
            }

            try
            {
                _laserMachine.SetVelocity(VelocityRegime);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Throwed the exception in the {nameof(ImplementMachineSettings)} method when setting a velocity regime.");
                _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(ImplementMachineSettings)} method when setting a velocity regime.");
                throw;
            }
#endif
        }
        private static CoorSystem<LMPlace> GetCoorSystem(string path)
        {
            var matrixElements = ExtensionMethods.DeserilizeObject<float[]>(path) ?? throw new NullReferenceException("CoorSystem in the file is invalid");
            var builder = CoorSystem<LMPlace>.GetWorkMatrixSystemBuilder();
            builder.SetWorkMatrix(matrixElements);
            var sys = builder.Build();
            return sys;
        }
        private void TuneCoorSystem()
        {
            var xleft = _settingsManager.Settings.XLeftPoint ?? throw new ArgumentNullException("XLeftPoint is null");
            var xright = _settingsManager.Settings.XRightPoint ?? throw new ArgumentNullException("XRightPoint is null");
            var yleft = _settingsManager.Settings.YLeftPoint ?? throw new ArgumentNullException("YLeftPoint is null");
            var yright = _settingsManager.Settings.YRightPoint ?? throw new ArgumentNullException("YRightPoint is null");
            var xoffset = _settingsManager.Settings.XOffset ?? throw new ArgumentNullException("XOffset is null");
            var yoffset = _settingsManager.Settings.YOffset ?? throw new ArgumentNullException("YOffset is null");
            var dx = xright - xleft;
            var dy = yright - yleft;

            //_waferAngle = Math.Atan2(dy, dx);
            _waferAngle = Math.Atan(dy / dx);
#if InvertAngles
            _waferAngle = -_waferAngle;
#endif

            _coorSystem.BuildRelatedSystem()
                      .Rotate(_waferAngle)
                      .Translate(xleft, yleft)
                      .Build(LMPlace.FileOnWaferUnderCamera);

            _coorSystem.BuildRelatedSystem()
                      .Rotate(_waferAngle)
                      .Translate(xleft + xoffset, yleft + yoffset)
                      .Build(LMPlace.FileOnWaferUnderLaser);


            _coorSystem.SetRelatedSystem(LMPlace.Loading, 50, 20);
            _coorSystem.SetRelatedSystem(LMPlace.UnderLaser, 1, 2);
            _coorSystem.SetRelatedSystem(LMPlace.LeftCorner, xleft, yleft);
            _coorSystem.SetRelatedSystem(LMPlace.RightCorner, 1, 2);
            MechTableVM?.SetOffsets(xoffset, yoffset);
        }
    }
}
