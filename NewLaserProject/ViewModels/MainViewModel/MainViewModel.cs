using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.Properties;
using PropertyChanged;
using MsgBox = HandyControl.Controls.MessageBox;

namespace NewLaserProject.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel
    {
        private readonly LaserMachine _laserMachine;
        public bool IsLaserInitialized { get; set; } = false;
        public bool IsMotionInitialized { get; set; } = false;
        public bool IsRightPanelVisible { get; set; } = true;
        public bool IsCentralPanelVisible { get; set; } = false;
        public bool IsLearningPanelVisible { get; set; } = false;
        public bool IsProcessPanelVisible { get; set; } = false;
        public string TechInfo
        {
            get; set;
        }
        public string IconPath
        {
            get; set;
        }
        public bool OnProcess { get; set; } = false;
        public MessageType CurrentMessageType { get; private set; } = MessageType.Empty;
        public BitmapImage CameraImage
        {
            get; set;
        }
        public AxisStateView XAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView YAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public AxisStateView ZAxis { get; set; } = new AxisStateView(0, 0, false, false, true, false);
        public double ScaleMarkersRatioFirst { get; private set; } = 0.1;
        public double ScaleMarkersRatioSecond => 1 - ScaleMarkersRatioFirst;

        private string _pierceSequenceJson = string.Empty;
        public Velocity VelocityRegime { get; private set; } = Velocity.Fast;
        public object RightSideVM
        {
            get; set;
        }
        public object CentralSideVM
        {
            get; set;
        }

        public MechanicVM MechTableVM
        {
            get; set;
        }

        private CameraVM _cameraVM;

        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubject<IProcessNotify> _subjMediator;
        public ObservableCollection<string> CameraCapabilities
        {
            get; set;
        }
        public int CameraCapabilitiesIndex
        {
            get; set;
        }
        public bool ShowVideo
        {
            get; set;
        }
        //---------------------------------------------
        private CoorSystem<LMPlace> _coorSystem;
        private ITeacher _currentTeacher;
        private bool _canTeach = false;

        private IProcess? _mainProcess;
        private double _waferAngle;
        private readonly ILogger _logger;

        public MainViewModel(LaserMachine laserMachine, IMediator mediator,
            IServiceProvider serviceProvider, ILoggerProvider loggerProvider,
            ISubject<IProcessNotify> subjMediator)
        {
            _logger = loggerProvider.CreateLogger("MainVM");
            _laserMachine = laserMachine;
            IsMotionInitialized = _laserMachine.IsMotionDeviceInit;
            _mediator = mediator;
            _subjMediator = subjMediator;
            _serviceProvider = serviceProvider;
            var workingDirectory = Environment.CurrentDirectory;
            _laserMachine.CameraPlugged += _laserMachine_CameraPlugged;
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;


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
            _logger.Log(LogLevel.Information, "App started");
            var settmanager = _serviceProvider.GetRequiredService<ISettingsManager<LaserMachineSettings>>();
        }

        private void _laserMachine_CameraPlugged(object? sender, EventArgs e)
        {
            //_laserMachine.StopCamera();
            CameraCapabilities = new(_laserMachine.AvailableVideoCaptureDevices[0].Item2);
            CameraCapabilitiesIndex = Settings.Default.PreferedCameraCapabilities;
            _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
        }

        private object _tempVM;
        private LightColumn _signalColumn;

        public bool IsMechViewChecked
        {
            get;
            set;
        }
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

            var reader = new IMDxfReader("D:/Test.dxf");
            var curve = reader.GetAllCurves().Single();
            var inflateCurve = curve.PObject.InflateCurve(200);
            reader.WriteCurveToFile("D:/TestInflate.dxf", inflateCurve.First(), true);
            var i = 0;
            //_laserMachine.SwitchOnValve(Valves.RedLight);

            //var laser = new JCZLaser(new PWM3());
            //var defLaserParams = ExtensionMethods
            //               .DeserilizeObject<MarkLaserParams>(AppPaths.DefaultLaserParams);

            //var mapper = _serviceProvider.GetService<IMapper>();
            //var mediator = _serviceProvider.GetService<IMediator>();
            //var defaultParams = mapper?.Map<ExtendedParams>(defLaserParams);

            //var dialogResult = await Dialog.Show<MachineControlsLibrary.CommonDialog.CommonDialog>()
            //    .SetDialogTitle("Параметры пера")
            //    .SetDataContext(new EditExtendedParamsVM(defaultParams), vm => { })
            //    .GetCommonResultAsync<ExtendedParams>();

            //laser.SetMarkParams(defLaserParams);
            //laser.SetExtMarkParams(new ExtParamsAdapter(dialogResult.CommonResult));
            //await laser.PierceDxfObjectAsync("D:/TestHatch.dxf");
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
            _openedFileVM = new FileVM(48, 60, _subjMediator);
            _openedFileVM.CanUndoChanged += _openedFileVM_CanUndoChanged;
            _openedFileVM.OnFileClicked += _openedFileVM_OnFileClicked;
            CentralSideVM = _openedFileVM;


            var count = _laserMachine.AvailableVideoCaptureDevices.Count;//TODO what if there is no any devices
            if (count != 0)
            {
                CameraCapabilities = new(_laserMachine.AvailableVideoCaptureDevices[0].Item2);
                CameraCapabilitiesIndex = Settings.Default.PreferedCameraCapabilities;
                _laserMachine.StartCamera(0, CameraCapabilitiesIndex);
            }


            _cameraVM = new CameraVM(_subjMediator);

            RightSideVM = _cameraVM;
#if PCIInserted
            _laserMachine.OnBitmapChanged += _cameraVM.OnVideoSourceBmpChanged;
            _cameraVM.VideoClicked += _cameraVM_VideoClicked;
#endif
        }

        private async void _openedFileVM_OnFileClicked(object? sender, System.Windows.Point e)
        {
            if (XAxis.MotionDone && YAxis.MotionDone && !_onProcessing)
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
            var caps = CameraCapabilities[CameraCapabilitiesIndex].Split(" ");

            if (double.TryParse(caps[0], out var xRatio) && double.TryParse(caps[2], out var yRatio))
            {
                var k = xRatio / yRatio;
                var offset = new[] { e.x * Settings.Default.CameraScale * k, -e.y * Settings.Default.CameraScale };
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
                MechTableVM?.SetCoordinates(XAxis.Position + Settings.Default.XOffset, YAxis.Position + Settings.Default.YOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Swallowed the exception in {nameof(_laserMachine_OnAxisMotionStateChanged)}");
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

            Guard.IsNotNull(axesConfigs, nameof(axesConfigs));

            var xpar = new MotionDeviceConfigs
            {
                maxAcc = 180,
                maxDec = 180,
                maxVel = 30,
                axDirLogic = (int)AxDirLogic.DIR_ACT_HIGH,
                plsOutMde = (int)(axesConfigs.XRightDirection ? PlsOutMode.OUT_DIR : PlsOutMode.OUT_DIR_DIR_NEG),
                reset = axesConfigs.XHomeReset,
                acc = Settings.Default.XAcc,
                dec = Settings.Default.XDec,
                ppu = 4005,// Settings.Default.XPPU*2,
                denominator = 4,
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
                plsOutMde = (int)(axesConfigs.YRightDirection ? PlsOutMode.OUT_DIR : PlsOutMode.OUT_DIR_DIR_NEG),
                reset = axesConfigs.YHomeReset,
                acc = Settings.Default.YAcc,
                dec = Settings.Default.YDec,
                ppu = 3993,//Settings.Default.YPPU*2,
                denominator = 4,
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
                plsOutMde = (int)(axesConfigs.ZRightDirection ? PlsOutMode.OUT_DIR : PlsOutMode.OUT_DIR_DIR_NEG),
                reset = axesConfigs.ZHomeReset,
                acc = Settings.Default.ZAcc,
                dec = Settings.Default.ZDec,
                ppu = Settings.Default.ZPPU,
                homeVelLow = Settings.Default.ZVelLow,
                homeVelHigh = Settings.Default.ZVelService
            };

            try
            {
                _laserMachine.AddAxis(Ax.X, axesConfigs.XLine)
                    .WithConfigs(xpar)
                    .WithVelRegime(Velocity.Fast, Settings.Default.XVelHigh)
                    .WithVelRegime(Velocity.Slow, Settings.Default.XVelLow)
                    .WithVelRegime(Velocity.Service, Settings.Default.XVelService)
                    .Build();

                _laserMachine.AddAxis(Ax.Y, axesConfigs.YLine)
                    .WithConfigs(ypar)
                    .WithVelRegime(Velocity.Fast, Settings.Default.YVelHigh)
                    .WithVelRegime(Velocity.Slow, Settings.Default.YVelLow)
                    .WithVelRegime(Velocity.Service, Settings.Default.YVelService)
                    .Build();

                _laserMachine.AddAxis(Ax.Z, axesConfigs.ZLine)
                    .WithConfigs(zpar)
                    .WithVelRegime(Velocity.Fast, Settings.Default.ZVelHigh)
                    .WithVelRegime(Velocity.Slow, Settings.Default.ZVelLow)
                    .WithVelRegime(Velocity.Service, Settings.Default.ZVelService)
                    .Build();

                _laserMachine.AddAxis(Ax.U, 0d).WithConfigs(xpar).Build();

                _laserMachine.AddGroup(Groups.XY, Ax.X, Ax.Y);

                _laserMachine.ConfigureGeometryFor(LMPlace.Loading)
                    .SetCoordinateForPlace(Ax.X, Settings.Default.XLoad)
                    .SetCoordinateForPlace(Ax.Y, Settings.Default.YLoad)
                    .Build();

                _laserMachine.ConfigureHomingForAxis(Ax.X)
                    .SetHomingDirection((AxDir)axesConfigs.XHomeDirection)
                    .SetHomingMode((HmMode)axesConfigs.XHomeMode)
                    .SetPositionAfterHoming(Settings.Default.XLeftPoint)
                    .SetHomingVelocity(Settings.Default.XVelService)
                    .Configure();

                _laserMachine.ConfigureHomingForAxis(Ax.Y)
                    .SetHomingDirection((AxDir)axesConfigs.YHomeDirection)
                    .SetHomingMode((HmMode)axesConfigs.YHomeMode)
                    .SetPositionAfterHoming(Settings.Default.YLeftPoint)
                    .SetHomingVelocity(Settings.Default.YVelService)
                    .Configure();

                _laserMachine.ConfigureHomingForAxis(Ax.Z)
                    .SetHomingDirection((AxDir)axesConfigs.ZHomeDirection)
                    .SetHomingVelocity(Settings.Default.ZVelService)
                    .SetPositionAfterHoming(Settings.Default.ZeroFocusPoint - WaferThickness)
                    .Configure();

                _laserMachine.ConfigureValves(
                        new()
                        {
                            [Valves.BlueLight] = (Ax.Y, Do.Out4),
                            [Valves.GreenLight] = (Ax.Y, Do.Out5),
                            [Valves.RedLight] = (Ax.U, Do.Out4),
                            [Valves.YellowLight] = (Ax.U, Do.Out5)
                        }
                    );

                _signalColumn = new LightColumn();
                _signalColumn.AddLight(LightColumn.Light.Red, () => _laserMachine.SwitchOnValve(Valves.RedLight), () => _laserMachine.SwitchOffValve(Valves.RedLight));
                _signalColumn.AddLight(LightColumn.Light.Green, () => _laserMachine.SwitchOnValve(Valves.GreenLight), () => _laserMachine.SwitchOffValve(Valves.GreenLight));
                _signalColumn.AddLight(LightColumn.Light.Yellow, () => _laserMachine.SwitchOnValve(Valves.YellowLight), () => _laserMachine.SwitchOffValve(Valves.YellowLight));
                _signalColumn.AddLight(LightColumn.Light.Blue, () => _laserMachine.SwitchOnValve(Valves.BlueLight), () => _laserMachine.SwitchOffValve(Valves.BlueLight));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Swallowed the exception in the {nameof(ImplementMachineSettings)} method.");
                throw;
            }

            try
            {
                _laserMachine.SetVelocity(VelocityRegime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Swallowed the exception in the {nameof(ImplementMachineSettings)} method when setting a velocity regime.");
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
            var dx = Settings.Default.XRightPoint - Settings.Default.XLeftPoint;
            var dy = Settings.Default.YRightPoint - Settings.Default.YLeftPoint;

            //_waferAngle = Math.Atan2(dy, dx);
            _waferAngle = Math.Atan(dy / dx);
#if InvertAngles
            _waferAngle = -_waferAngle;
#endif

            _coorSystem.BuildRelatedSystem()
                      .Rotate(_waferAngle)
                      .Translate(Settings.Default.XLeftPoint, Settings.Default.YLeftPoint)
                      .Build(LMPlace.FileOnWaferUnderCamera);

            _coorSystem.BuildRelatedSystem()
                      .Rotate(_waferAngle)
                      .Translate(Settings.Default.XLeftPoint + Settings.Default.XOffset, Settings.Default.YLeftPoint + Settings.Default.YOffset)
                      .Build(LMPlace.FileOnWaferUnderLaser);


            _coorSystem.SetRelatedSystem(LMPlace.Loading, 50, 20);
            _coorSystem.SetRelatedSystem(LMPlace.UnderLaser, 1, 2);
            _coorSystem.SetRelatedSystem(LMPlace.LeftCorner, Settings.Default.XLeftPoint, Settings.Default.YLeftPoint);
            _coorSystem.SetRelatedSystem(LMPlace.RightCorner, 1, 2);
            MechTableVM?.SetOffsets(Settings.Default.XOffset, Settings.Default.YOffset);
        }
    }
}
