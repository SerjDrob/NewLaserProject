#define Snap

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.LogSinks;
using NewLaserProject.Classes.LogSinks.RepositorySink;
using NewLaserProject.Classes.Process;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.Data.Models;
using NewLaserProject.Data.Models.TechnologyFeatures.Get;
using NewLaserProject.ViewModels.DialogVM;
using Newtonsoft.Json;
using DialogResult = System.Windows.Forms.DialogResult;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Path = System.IO.Path;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using MsgBox = HandyControl.Controls.MessageBox;
using MachineClassLibrary.Miscellaneous;
using System.Windows;

namespace NewLaserProject.ViewModels
{

    public partial class MainViewModel
    {
        private List<IDisposable> _currentProcSubscriptions;
        private DateTime _procStartTime;
        private DateTime _procObjTempTime = new(0);
        private Timer _processTimer;
        private bool _currObjectStarted;
        private bool _isSnapAllowed;
        public ICoorSystem? ExistingAlignment { get; private set; }
            /*= CoorSystem<LMPlace>.GetWorkMatrixSystemBuilder()
            .SetWorkMatrix([1,2,3,4,5,6])
            .Build();*/

        public bool IsBlockZ { get; set; } = true;
        public ObservableCollection<ProcObjTabVM> ProcessingObjects { get; set; }
        public ObservableCollection<ObjsToProcess> ObjectsForProcessing { get; set; } = new();
        public IProcObject IsBeingProcessedObject { get; set; }
        public FileAlignment FileAlignment { get; set; }
        public Technology CurrentTechnology { get; set; }
        public string CurrentLayerFilter { get; set; }
        public LaserEntity CurrentEntityType { get; set; }
        public bool IsWaferMark
        {
            get => _openedFileVM?.IsMarkTextVisible ?? false;
            set
            {
                if (_openedFileVM is not null) _openedFileVM.IsMarkTextVisible = value;
            }
        }
        public MarkPosition MarkPosition { get; set; }
        public bool IsProcessLoaded { get; private set; }
        public bool IsProcessUnderCamera { get; set; }

       
        public string CurrentProcObjectTimer { get; set; }
        public string LastProcObjectTimer { get; set; }
        public string TotalProcessTimer { get; set; }
        public ObservableCollection<ObjectForProcessing> ChosenProcessingObjects { get; set; }

        [ICommand]
        private async Task StartStopProcess(object arg)
        {
            try
            {
                if ((bool)arg)
                {
                    if(!IsLaserInitialized & !IsProcessUnderCamera)  
                    {
                        if (MsgBox.Ask(PWMDeviceOk ? "Не подключена плата управления лазером" : "Не подключен ШИМ" + ", подложка не будет прошита. Всё равно продолжить?") != System.Windows.MessageBoxResult.OK) 
                        {
                            return; 
                        }
                    }
                    await _appStateMachine.FireAsync(AppTrigger.StartProcess);
                }
                else
                {
                    if (_mainProcess is null) throw new NullReferenceException("_mainProcess is null");
                    await _mainProcess.Deny().ConfigureAwait(false);
                    await _appStateMachine.FireAsync(AppTrigger.EndProcess).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Throwed the exception in the method {nameof(StartStopProcess)}.");
                _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the method {nameof(StartStopProcess)}.");
                _processTimer?.Dispose();
                throw;
            }
        }

        private void _processTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            TotalProcessTimer = (e.SignalTime - _procStartTime).ToString(@"hh\:mm\:ss");
            if (_currObjectStarted) _procObjTempTime = _procObjTempTime.AddSeconds(1);
            CurrentProcObjectTimer = _procObjTempTime.ToString("mm:ss");
        }



        [ICommand]
        private void DenyDownloadedProcess()
        {
            _currentProcSubscriptions?.ForEach(s => s.Dispose());
            _mainProcess?.Dispose();
            if (_processTimer is not null)
            {
                _processTimer.Elapsed -= _processTimer_Elapsed;
                _processTimer.Stop();
                _processTimer.Close();
                _processTimer.Dispose();
            }
            OnProcess = false;
            IsProcessing = false;
            //Growl.Clear();
            IsProcessPanelVisible = false;
            HideRightPanel(false);
            ChangeViews(false);
            _cameraVM.SnapShotButtonVisible = false;
            _cameraVM.SnapshotVisible = false;
            IsProcessLoaded = false;
            _openedFileVM.IsCircleButtonVisible = true;
        }


        private IEnumerable<IProcObject> ArrangeProcObjects(List<IProcObject> procObjects)
        {
            var list = procObjects.Select(o => (o.Id.ToString(), o.X, o.Y))
                .OrderBy(l => l.X)
                .ToList();
            var curX = list.First().X;
            var delta = 3000d;
            var curCount = 0;
            var count = list.Count();
            var asc = false;
            var result = new List<(string, double, double)>();
            do
            {
                var rest = list.Skip(curCount);
                var seq =
                    rest.TakeWhile(l => (l.X <= curX + delta) & (l.X >= curX - delta))
                    .ToList();
                var res = asc ? seq.OrderBy(y => y.Y) : seq.OrderByDescending(y => y.Y);
                result.AddRange(res.ToList());
                curCount += seq.Count;
                if (curCount >= count) continue;
                curX = list.ElementAt(curCount).X;
                asc ^= true;
            } while (curCount < count);
            return result.Select(r => procObjects.Single(p => p.Id.ToString() == r.Item1));
        }

        public string CurrentProcBlock { get; set; }
        public int CurrentProcMainLoopCount { get; set; }
        public CurrentPierceBlock CurrentPierceBlock { get; set; }
        public bool IsPrevAlignmentEnable { get; set; }
        private Dictionary<CurrentPierceBlock, CurrentPierceBlock> _pierceBlocks { get; set; } = new();
        public ObservableCollection<CurrentPierceBlock> PierceBlocks { get; set; } = new();

        [ICommand]
        private async Task DownloadProcess()
        {
            IsProcessing = false;
            CutMode = false;
            //TODO determine size by specified layer
            try
            {
                if (!ChosenProcessingObjects?.Any() ?? true) throw new ArgumentException("Объекты для обработки отсутствуют.");
                _currentProcSubscriptions = new();
                var topologySize = _dxfReader.GetSize();

                var wafer = new LaserWafer(topologySize);

                wafer.SetRestrictingArea(0, 0, WaferWidth, WaferHeight);
                wafer.Scale(1F / DefaultFileScale);
                if (WaferTurn90) wafer.Turn90();
                if (MirrorX) wafer.MirrorX();
                wafer.OffsetX((float)WaferOffsetX);
                wafer.OffsetY((float)WaferOffsetY);


                Func<LaserEntity, string, IEnumerable<IProcObject>> getObjects = (LaserEntity entityType, string layer) => entityType switch
                {
                    LaserEntity.Curve => _dxfReader.GetAllCurves(layer).Cast<IProcObject>(),
                    LaserEntity.Circle => _dxfReader.GetCircles(layer).Cast<IProcObject>(),
                    _ => throw new ArgumentOutOfRangeException($"{CurrentEntityType} is not valid entity type for processing")
                };

                var processing = new List<(IEnumerable<IProcObject>, MicroProcess)>();
                foreach (var ofp in ChosenProcessingObjects)
                {
                    var objects = getObjects(ofp.LaserEntity, ofp.Layer);
                    try
                    {
                        objects = ArrangeProcObjects(objects.ToList());
                    }
                    catch (Exception)
                    {
                    }

                    var json = File.ReadAllText(Path.Combine(AppPaths.TechnologyFolder, $"{ofp.Technology?.ProcessingProgram}.json"));
                    var preparator = new EntityPreparator(_dxfReader, AppPaths.TempFolder);

                    //preparator.SetEntityAngle(Settings.Default.PazAngle); in case to take out the paz angle from the commonprocess ctor
                    var mp = new MicroProcess(json, preparator, _laserMachine, async z =>
                    {
                        await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true);
                    });
                    if (_cluster?.Enable ?? false)
                    {
                        processing.Add((objects.SplitOnClusters(new(_fileActualSize.minPoint.X, _fileActualSize.minPoint.Y, _fileActualSize.maxPoint.X, _fileActualSize.maxPoint.Y), _cluster.XParts, _cluster.YParts).ToList(), mp));
                    }
                    else
                    {
                        processing.Add((objects, mp));
                    }
                }

                _mainProcess = new CommonProcess(
                    processing: processing,
                    wafer: wafer,
                    laserMachine: _laserMachine,
                    zeroZPiercing: _settingsManager.Settings.ZeroPiercePoint ?? throw new ArgumentNullException("ZeroPiercePoint is null"),
                    zeroZCamera: _settingsManager.Settings.ZeroFocusPoint ?? throw new ArgumentNullException("ZeroFocusPoint is null"),
                    waferThickness: WaferThickness,
                    dX: _settingsManager.Settings.XOffset ?? throw new ArgumentNullException("XOffset is null"),
                    dY: _settingsManager.Settings.YOffset ?? throw new ArgumentNullException("YOffset is null"),
                    offsetPoints: _settingsManager.Settings.OffsetPoints ?? throw new ArgumentNullException("OffsetPoints is null"),
                    pazAngle: 0,//_settingsManager.Settings.PazAngle ?? throw new ArgumentNullException("PazAngle is null"),//Settings.Default.PazAngle,
                    subject: _subjMediator,
                    baseCoorSystem: (ExistingAlignment != null && IsPrevAlignmentEnable) ? CoorSystem<LMPlace>.GetFromSystem(ExistingAlignment) : _coorSystem,
                    underCamera: IsProcessUnderCamera,
                    aligningPoints: (ExistingAlignment!=null && IsPrevAlignmentEnable) ? FileAlignment.AlignPrev : FileAlignment,
                    waferAngle: _waferAngle,
                    scale: DefaultFileScale,
                    _xCoeffLine,
                    _yCoeffLine);

                _mainProcess.OfType<ProcWaferChanged>()
                    .Subscribe(args =>
                    {
                        ProcessingObjects = new();
                        args.Wafer.Aggregate(1, (ind, p) =>
                        {
                            ProcessingObjects.Add(new ProcObjTabVM { Index = ind, ProcObject = p });
                            return ++ind;
                        });
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<PiercingWithParams>()
                    .Subscribe(args =>
                    {
                        var p = args.ExtParams;
                        CurrentProcBlock = $"SP:{p.MarkSpeed}|L:{p.MarkLoop}|PWMF:{p.PWMFrequency}|PWMD{p.PWMDutyCycle}%|W:{p.HatchWidth}";
                        CurrentPierceBlock = new(p.MarkSpeed, p.MarkLoop, p.PWMFrequency, p.PWMDutyCycle, p.HatchWidth);
                        if (_pierceBlocks.TryGetValue(CurrentPierceBlock, out var cb))
                        {
                            _pierceBlocks[CurrentPierceBlock] = cb with { MarkLoop = cb.MarkLoop + CurrentPierceBlock.MarkLoop };
                        }
                        else
                        {
                            _pierceBlocks[CurrentPierceBlock] = CurrentPierceBlock;      
                        }
                        PierceBlocks = new(_pierceBlocks.Values);
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);


                _mainProcess.OfType<GotAlignment>()
                    .Subscribe(args => 
                    {
                        ExistingAlignment = args.CoorSystem;
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<MainLoopChanged>()
                    .Subscribe(args =>
                    {
                        CurrentProcMainLoopCount = args.Loop;
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);


                _mainProcess.OfType<ProcessingStarted>()
                    .Subscribe(args =>
                    {
                        IsProcessing = true;
                        _processTimer = new Timer(1000);
                        _procStartTime = DateTime.Now;
                        _processTimer.Elapsed += _processTimer_Elapsed;
                        _processTimer.Start();
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcessingStopped>()
                    .Subscribe(args =>
                    {
                        IsProcessing = false;
                        _processTimer?.Stop();
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcObjectChanged>()
                    .Where(poargs => poargs.ProcObject.IsProcessed)
                    .Subscribe(args =>
                    {
                        _currObjectStarted = false;
                        LastProcObjectTimer = CurrentProcObjectTimer;
                        _procObjTempTime = new(0);
                        var o = ProcessingObjects.SingleOrDefault(po => po.ProcObject.Id == args.ProcObject.Id);
                        o.Visibility = System.Windows.Visibility.Collapsed;
                        if (o is not null) ProcessingObjects.Remove(o);//TODO make it thread safe
                        _pierceBlocks.Clear();
                        PierceBlocks.Clear();
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcObjectChanged>()
                    .Where(poargs => !poargs.ProcObject.IsProcessed & poargs.ProcObject.IsBeingProcessed)
                    .Subscribe(poargs =>
                    {
                        _currObjectStarted = true;
                        IsBeingProcessedObject = poargs.ProcObject;//ProcessingObjects.SingleOrDefault(o => o.ProcObject.Id == poargs.ProcObject.Id)?.ProcObject;
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcCompletionPreview>()
                    .Select(args => Observable.FromAsync(async () =>
                    {
                        var status = args.Status;
                        IsProcessing = false;
                        var tempVel = _laserMachine.SetVelocity(MachineClassLibrary.Machine.Velocity.Service);
                        switch (status)
                        {
                            case CompletionStatus.Success:


                                if (_markTextVM.MarkedText != null)
                                {
                                    var text = _markTextVM.MarkedText;
                                    text += _markTextVM.IsDateEnable ? " " + DateTime.Now.ToShortDateString() : "";
                                    text += _markTextVM.IsTimeEnable ? " " + DateTime.Now.ToShortTimeString() : "";
                                    if (IsWaferMark) await MarkWaferAsync(text, MarkPosition, _markTextVM.TextHeight, _markTextVM.EdgeGap, args.CoorSystem); 
                                }


                                _ = _signalColumn.BlinkLightAsync(LightColumn.Light.Blue).ConfigureAwait(false);
                                MsgBox.Success("Процесс завершён");
                                _signalColumn.TurnOff();
                                _signalColumn.TurnOnLight(LightColumn.Light.Green);
                                //_workTimeLogger?.LogProcessEnded();

                                _logger.ForContext<MicroProcess>().Information(RepoSink.End, RepoSink.Proc);

                                break;
                            case CompletionStatus.Cancelled:
                                _ = _signalColumn.BlinkLightAsync(LightColumn.Light.Red).ConfigureAwait(false);
                                MsgBox.Fatal("Процесс отменён");
                                _signalColumn.TurnOff();
                                _signalColumn.TurnOnLight(LightColumn.Light.Green);
                                //_workTimeLogger?.LogProcessCancelled();
                                _logger.Information(RepoSink.Cancelled, RepoSink.Proc);
                                break;
                            default:
                                break;
                        }
                        _laserMachine.ResetErrors();
                        await _laserMachine.GoThereAsync(LMPlace.Loading);
                        var z = (_settingsManager.Settings.ZeroFocusPoint - WaferThickness) ?? ZAxis.Position;
                        await _laserMachine.MoveAxInPosAsync(Ax.Z, z);
                        _laserMachine.SetVelocity(tempVel);
                        await _appStateMachine.FireAsync(AppTrigger.EndProcess);
                    }))
                    .Concat()
                    .Subscribe()
                    .AddSubscriptionTo(_currentProcSubscriptions);


                _mainProcess.OfType<ProcessMessage>()
                    .Select(args => Observable.FromAsync(async () =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            switch (args.MessageType)
                            {
                                case MsgType.Request:
                                    break;
                                case MsgType.Info:
                                    Growl.Info(new GrowlInfo()
                                    {
                                        StaysOpen = true,
                                        Message = args.Message,
                                        ShowDateTime = false,
                                    });
                                    break;
                                case MsgType.Warn:
                                    break;
                                case MsgType.Error:
                                    Growl.Error(new GrowlInfo()
                                    {
                                        StaysOpen = true,
                                        Message = args.Message,
                                        ShowDateTime = false,
                                    });
                                    break;
                                case MsgType.Clear:
                                    Growl.Clear();
                                    break;
                                default:
                                    break;
                            }
                        });
                    }))
                    .Concat()
                    .Subscribe()
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcessException>()
                    .Subscribe(pe =>
                    {
                        _logger.ForContext<MainViewModel>().Warning(pe.Message);
                    });
                _mainProcess.OfType<PermitSnap>()
                    .Subscribe(r => 
                    {
                        _isSnapAllowed = true;
                    });
                _mainProcess.OfType<SnapNotAlowed>()
                    .Subscribe(s => 
                    {
                        _isSnapAllowed = false; 
                    });
                HideProcessPanel(false);
                _mainProcess.CreateProcess();
                IsProcessLoaded = true;
                _openedFileVM.IsCircleButtonVisible = false;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                //_logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");

                _logger.ForContext<MainViewModel>().Warning(ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");

                Growl.Error(new GrowlInfo()
                {
                    Message = ex.Message,
                    ShowDateTime = false
                });
            }
            catch (FileNotFoundException ex)
            {
                //_logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                _logger.ForContext<MainViewModel>().Information(ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                Growl.Error($"Файл технологии \"{CurrentTechnology?.ProgramName}\" не найден.");
            }
            catch (NullReferenceException ex)
            {
                //_logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                _logger.ForContext<MainViewModel>().Information(ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                if (CurrentTechnology is null) Growl.Error($"Файл технологии не выбран.");
            }
            catch (ArgumentException ex)
            {
                //_logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                _logger.ForContext<MainViewModel>().Information(ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                Growl.Error(new GrowlInfo()
                {
                    WaitTime = 2,
                    Message = ex.Message,
                    ShowDateTime = false,
                });
            }
            catch (JsonSerializationException ex)
            {
                //_logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                _logger.ForContext<MainViewModel>().Information(ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                Growl.Error(new GrowlInfo()
                {
                    Message = "В одной из заданных программ присутствует ошибка параметра.",
                    ShowDateTime = false,
                });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Throwed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                throw;
            }
        }


        [ICommand]
        private void SaveWorkFile()
        {
            var objects = new List<(string, LaserEntity, int)>();
            foreach (var item in ChosenProcessingObjects)
            {
                var (layer, entity, id) = item;
                if (layer != string.Empty && id != -1)
                {
                    objects.Add((layer, entity, id));
                }
            }


            var saved = new WorkProcUnit(
                WaferWidth,
                WaferHeight,
                DefaultFileScale,
                WaferTurn90,
                MirrorX,
                _openedFileVM?.WaferOffsetX ?? 0,
                _openedFileVM?.WaferOffsetY ?? 0,
                _openedFileVM?.FileOffsetX ?? 0,
                _openedFileVM?.FileOffsetY ?? 0,
                objects,
                FileName,
                FileAlignment,
                IsWaferMark,
                MarkPosition,
                ((DxfEditor)_dxfReader).ErasedObjects,
                _openedFileVM?.GetIgnoredLayers() ?? new());

            var serialized = JsonConvert.SerializeObject(saved);

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Файлы обработки (*.wpu)|*.wpu";
            saveFileDialog.DefaultExt = "*.wpu";
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(FileName);
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, serialized);
            }
        }


        [ICommand]
        private async Task OpenWPU()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы обработки (*.wpu)|*.wpu";
            openFileDialog.DefaultExt = "*.wpu";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var serialized = File.ReadAllText(openFileDialog.FileName);
                var wpu = JsonConvert.DeserializeObject<WorkProcUnit>(serialized);

                if (wpu is not null)
                {
                    WaferWidth = wpu.WaferWidth;
                    WaferHeight = wpu.WaferHeight;
                    DefaultFileScale = wpu.DefaultFileScale;                    
                    WaferTurn90 = wpu.WaferTurn90;
                    MirrorX = wpu.MirrorX;
                    FileName = wpu.FileName;
                    FileAlignment = wpu.FileAlignment;
                    IsWaferMark = wpu.IsWaferMark;
                    MarkPosition = wpu.MarkPosition;
                    IgnoredLayers = wpu.DisabledLayers;
                    await OpenChosenFile(true,wpu.WaferOffsetX,wpu.WaferOffsetY,wpu.FileOffsetX,wpu.FileOffsetY);

                    wpu.ErasedObjects.ToList()
                        .ForEach(e => _openedFileVM?.GotSelectionMultipleHandler(e.layers, e.selection));


                    var objs = new List<ObjectForProcessing>();
                    foreach (var obj in wpu.Objects)
                    {
                        var technology = await _mediator.Send(new GetTechnologyByIdRequest(obj.Item3));
                        if (technology?.Technology is not null)
                        {
                            var objForProc = new ObjectForProcessing
                            {
                                LaserEntity = obj.Item2,
                                Layer = obj.Item1,
                                Technology = technology.Technology
                            };
                            objs.Add(objForProc);
                            LayersProcessingModel?.SetObjectChecked(obj.Item1, obj.Item2);
                        }
                        if (objs.Any())
                        {
                            ChosenProcessingObjects = new(objs);
                        }
                    }
                }
            }
        }

        private async Task StartProcessAsync()
        {

#if PCIInserted

            var result = await Dialog.Show<CheckParamsDialog>()
               .SetDialogTitle("Запуск процесса")
               .SetDataContext<AskThicknessVM>(vm => vm.Thickness = WaferThickness)
               .GetCommonResultAsync<double>();

            ProcessParams procParams;

            if (result.Success)
            {
                procParams = new ProcessParams(result.CommonResult);
                _mainProcess?.ChangeParams(procParams);
            }
            else
            {
                await _appStateMachine.FireAsync(AppTrigger.EndProcess);
                return;
            }

            try
            {
                var laserSettingsJson = File.ReadAllText(AppPaths.DefaultLaserParams);

                var laserParams = new JsonDeserializer<MarkLaserParams>()
                    .SetKnownType<PenParams>()
                    .SetKnownType<HatchParams>()
                    .Deserialize(laserSettingsJson);

                _laserMachine.SetMarkParams(laserParams);

                OnProcess = true;


                //_logger.LogInformation(new EventId(1, "Process"), $"The process started." +
                //    $"File's name: {FileName}" +
                //    $"Layer's name for processing: {CurrentLayerFilter}" +
                //    $"Entity type for processing: {CurrentEntityType}");


                _logger.ForContext<MainViewModel>().Information("The process started. File's name: {FileName} Layer's name for processing: {CurrentLayerFilter} Entity type for processing: {CurrentEntityType}",
                    FileName, CurrentLayerFilter, CurrentEntityType);

                _signalColumn.TurnOnLight(LightColumn.Light.Yellow);

                var techName =
                    ChosenProcessingObjects.Aggregate(new StringBuilder(),
                    (acc, obj) => acc.AppendLine($"{obj.Layer} -> {obj.LaserEntity} -> {obj.Technology.ProgramName}"),
                    acc => acc.ToString());

                //_workTimeLogger?.LogProcessStarted(FileName, procParams.WaferThickness.ToString(), techName, WaferThickness);

                _logger.ForContext<MicroProcess>().Information(RepoSink.ProcArgs, new ProcStartedArgs
                {
                    FileName = FileName,
                    MaterialThickness = WaferThickness,
                    TechnologyName = techName,
                    MaterialName = procParams.WaferThickness.ToString()
                });

                await _mainProcess.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Throwed the exception in the {nameof(StartProcessAsync)} method.");
                _logger.ForContext<MainViewModel>().Error(ex, $"Throwed the exception in the {nameof(StartProcessAsync)} method.");
                throw;
            }
#endif
        }

        private async Task MarkWaferAsync(string markingText, MarkPosition markPosition, double fontHeight, double edgeGap, ICoorSystem coorSystem)
        {
            var (x, y, angle) = markPosition switch
            {
                MarkPosition.N => (WaferWidth / 2, WaferHeight - edgeGap - fontHeight / 2, 0d),
                MarkPosition.E => (WaferWidth - edgeGap - fontHeight / 2, WaferHeight / 2, Math.PI / 2),
                MarkPosition.S => (WaferWidth / 2, edgeGap + fontHeight / 2, 0d),
                MarkPosition.W => (edgeGap + fontHeight / 2, WaferHeight / 2, Math.PI / 2),
            };
            var position = coorSystem.ToGlobal(x, y);

            var dX = default(double);
            var dY = default(double);

            _settingsManager.Settings.OffsetPoints.GetOffsetByCurCoor(x, y, ref dX, ref dY);
            var posX = position[0] + dX;
            var posY = position[1] + dY;


            var theta = coorSystem.GetMatrixAngle();
            //var markingText = Path.GetFileNameWithoutExtension(FileName) + " " + DateTime.Now;

            await Task.WhenAll(
                _laserMachine.MoveAxInPosAsync(Ax.X, posX, true),
                _laserMachine.MoveAxInPosAsync(Ax.Y, posY, true));


            var markTextJson = File.ReadAllText(AppPaths.MarkTextParams);

            var laserParams = new JsonDeserializer<ExtendedParams>()
                .Deserialize(markTextJson);

            _laserMachine.SetExtMarkParams(new ExtParamsAdapter(laserParams));
            await _laserMachine.MarkTextAsync(markingText, fontHeight, angle + /*-*/ theta);
        }

        [ICommand]
        private void SetMarkPosition()
        {
            if (MarkPosition == MarkPosition.S)
            {
                MarkPosition = MarkPosition.W;
                _openedFileVM?.SetTextPosition(MarkPosition);
                return;
            }
            MarkPosition++;
            _openedFileVM?.SetTextPosition(MarkPosition);
        }

    }
}
