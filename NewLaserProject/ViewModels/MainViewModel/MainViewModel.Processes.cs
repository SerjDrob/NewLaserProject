#define Snap

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HandyControl.Controls;
using HandyControl.Data;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineControlsLibrary.Classes;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.Data.Models;
using NewLaserProject.Properties;
using Newtonsoft.Json;
using Tang.Library.Algorithm.PathSolution.SP;
using Tang.Library.Algorithm.PathSolution.TSP;
using Tang.Library.Algorithm.PathSolution.TSP.Algorithm.Genetic.Chromosome;
using Tang.Library.Algorithm.PathSolution.TSP.MapComponent;
using Path = System.IO.Path;

namespace NewLaserProject.ViewModels
{

    internal partial class MainViewModel
    {
        public ObservableCollection<ProcObjTabVM> ProcessingObjects
        {
            get; set;
        }
        public ObservableCollection<ObjsToProcess> ObjectsForProcessing { get; set; } = new();

        public IProcObject IsBeingProcessedObject
        {
            get; set;
        }
        public FileAlignment FileAlignment
        {
            get; set;
        }
        public Technology CurrentTechnology
        {
            get; set;
        }
        public string CurrentLayerFilter
        {
            get; set;
        }
        public LaserEntity CurrentEntityType
        {
            get; set;
        }
        public bool IsWaferMark
        {
            get => _openedFileVM?.IsMarkTextVisible ?? false;
            set
            {
                if (_openedFileVM is not null) _openedFileVM.IsMarkTextVisible = value;
            }
        }
        public MarkPosition MarkPosition
        {
            get; set;
        }
        public bool IsProcessLoaded
        {
            get;
            private set;
        }


        private List<IDisposable> _currentProcSubscriptions;

        [ICommand]
        private async Task StartStopProcess(object arg)
        {
            try
            {
                if ((bool)arg)
                {
                    await _appStateMachine.FireAsync(AppTrigger.StartProcess);
                }
                else
                {
                    await _mainProcess.Deny().ConfigureAwait(false);
                    await _appStateMachine.FireAsync(AppTrigger.EndProcess).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Throwed the exception in the method {nameof(StartStopProcess)}.");
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

        private DateTime _procStartTime;
        private DateTime _procObjTempTime = new(0);
        private Timer _processTimer;
        private bool _currObjectStarted;
        public string CurrentProcObjectTimer
        {
            get;
            set;
        }
        public string LastProcObjectTimer
        {
            get;
            set;
        }
        public string TotalProcessTimer
        {
            get;
            set;
        }
        private bool _onProcessing = false;

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
            _onProcessing = false;
            //Growl.Clear();
            IsProcessPanelVisible = false;
            HideRightPanel(false);
            ChangeViews(false);
            _cameraVM.SnapShotButtonVisible = false;
            _cameraVM.SnapshotVisible = false;
            IsProcessLoaded = false;
            _openedFileVM.IsCircleButtonVisible = true;
        }
        public ObservableCollection<ObjectForProcessing> ChosenProcessingObjects
        {
            get;
            set;
        }

        private IEnumerable<IProcObject> ArrangeProcObjects(List<IProcObject> procObjects)
        {
            var g = Guid.NewGuid();
            var gg = g.ToString();


            var list = procObjects.Select(o => (o.Id.ToString(), o.X, o.Y))
                .OrderBy(l=>l.X)
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



        [ICommand]
        private async Task DownloadProcess()
        {
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

                    if (objects.Count() > 1)
                    {
                        var lastPoint = new System.Windows.Point(objects.ElementAt(1).X, objects.ElementAt(1).Y);
                        var firstLine = new LineGeometry(new(objects.ElementAt(0).X, objects.ElementAt(0).Y), lastPoint);
                        var geometries = new GeometryCollection();
                        geometries.Add(firstLine);
                        objects.Skip(1).ToList().ForEach(o =>
                        {
                            var curpoint = new System.Windows.Point(o.X, o.Y);
                            var line = new LineGeometry(lastPoint, curpoint);
                            geometries.Add(line);
                            lastPoint = curpoint;
                        });
                        var lgc = new LayerGeometryCollection(geometries, "MyRoute", true, Brushes.Red, Brushes.Yellow);
                        _openedFileVM.AddRoute(Enumerable.Repeat(lgc, 1)); 
                    }

                    var json = File.ReadAllText(Path.Combine(AppPaths.TechnologyFolder, $"{ofp.Technology?.ProcessingProgram}.json"));
                    var preparator = new EntityPreparator(_dxfReader, AppPaths.TempFolder);

                    //preparator.SetEntityAngle(Settings.Default.PazAngle); in case to take out the paz angle from the commonprocess ctor
                    var mp = new MicroProcess(json, preparator, _laserMachine, z =>
                    {
                        return _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true);
                    });
                    processing.Add((objects, mp));
                }



                _mainProcess = new CommonProcess(
                    processing: processing,
                    wafer: wafer,
                    laserMachine: _laserMachine,
                    zeroZPiercing: Settings.Default.ZeroPiercePoint,
                    zeroZCamera: Settings.Default.ZeroFocusPoint,
                    waferThickness: WaferThickness,
                    dX: Settings.Default.XOffset,
                    dY: Settings.Default.YOffset,
                    pazAngle: 0,//Settings.Default.PazAngle,
                    subject: _subjMediator,
                    baseCoorSystem: _coorSystem,
                    underCamera: false,
                    aligningPoints: FileAlignment,
                    waferAngle: _waferAngle,
                    scale: DefaultFileScale);

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


                _mainProcess.OfType<ProcessingStarted>()
                    .Subscribe(args =>
                    {
                        _processTimer = new Timer(1000);
                        _procStartTime = DateTime.Now;
                        _processTimer.Elapsed += _processTimer_Elapsed;
                        _processTimer.Start();
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcessingStopped>()
                    .Subscribe(args =>
                    {
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
                        if(o is not null) ProcessingObjects.Remove(o);
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
                        switch (status)
                        {
                            case CompletionStatus.Success:
                                if (IsWaferMark) await MarkWaferAsync(MarkPosition, 1, 0.1, args.CoorSystem);
                                MessageBox.Success("Процесс завершён");
                                break;
                            case CompletionStatus.Cancelled:
                                MessageBox.Fatal("Процесс отменён");
                                break;
                            default:
                                break;
                        }
                        await _laserMachine.GoThereAsync(LMPlace.Loading);
                        await _appStateMachine.FireAsync(AppTrigger.EndProcess);
                    }))
                    .Concat()
                    .Subscribe()
                    .AddSubscriptionTo(_currentProcSubscriptions);


                _mainProcess.OfType<ProcessMessage>()
                    .Subscribe(args =>
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
                                break;
                            case MsgType.Clear:
                                Growl.Clear();
                                break;
                            default:
                                break;
                        }
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                HideProcessPanel(false);
                _mainProcess.CreateProcess();
                IsProcessLoaded = true;
                _openedFileVM.IsCircleButtonVisible = false;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");

                Growl.Error(new GrowlInfo()
                {
                    Message = ex.Message,
                    ShowDateTime = false
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                Growl.Error($"Файл технологии \"{CurrentTechnology.ProgramName}\" не найден.");
            }
            catch (NullReferenceException ex)
            {
                _logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                if (CurrentTechnology is null) Growl.Error($"Файл технологии не выбран.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                Growl.Error(new GrowlInfo()
                {
                    WaitTime = 2,
                    Message = ex.Message,
                    ShowDateTime = false,
                });
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogInformation(new EventId(2, "Process"), ex, $"Swallowed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                Growl.Error(new GrowlInfo()
                {
                    Message = "В одной из заданных программ присутствует ошибка параметра.",
                    ShowDateTime = false,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Throwed the exception in the {nameof(MainViewModel.DownloadProcess)} method.");
                throw;
            }
        }

        private async Task StartProcessAsync()
        {

#if PCIInserted

            try
            {
                var laserSettingsJson = File.ReadAllText(AppPaths.DefaultLaserParams);

                var laserParams = new JsonDeserializer<MarkLaserParams>()
                    .SetKnownType<PenParams>()
                    .SetKnownType<HatchParams>()
                    .Deserialize(laserSettingsJson);

                _laserMachine.SetMarkParams(laserParams);

                OnProcess = true;
                _logger.LogInformation(new EventId(1, "Process"), $"The process started." +
                    $"File's name: {FileName}" +
                    $"Layer's name for processing: {CurrentLayerFilter}" +
                    $"Entity type for processing: {CurrentEntityType}");
                await _mainProcess.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Throwed the exception in the {nameof(StartProcessAsync)} method.");
                throw;
            }
#endif
        }

        private async Task MarkWaferAsync(MarkPosition markPosition, double fontHeight, double edgeGap, ICoorSystem coorSystem)
        {
            var (x, y, angle) = markPosition switch
            {
                MarkPosition.N => (WaferWidth / 2, WaferHeight - edgeGap - fontHeight / 2, 0d),
                MarkPosition.E => (WaferWidth - edgeGap - fontHeight / 2, WaferHeight / 2, Math.PI / 2),
                MarkPosition.S => (WaferWidth / 2, edgeGap + fontHeight / 2, 0d),
                MarkPosition.W => (edgeGap + fontHeight / 2, WaferHeight / 2, Math.PI / 2),
            };
            var position = coorSystem.ToGlobal(x, y);
            var theta = coorSystem.GetMatrixAngle();
            var markingText = Path.GetFileNameWithoutExtension(FileName) + " " + DateTime.Today.Date;
            await _laserMachine.MoveGpInPosAsync(MachineClassLibrary.Machine.Groups.XY, position, true);

            var markTextJson = File.ReadAllText(AppPaths.MarkTextParams);

            var laserParams = new JsonDeserializer<ExtendedParams>()
                .Deserialize(markTextJson);

            _laserMachine.SetExtMarkParams(new ExtParamsAdapter(laserParams));
            await _laserMachine.MarkTextAsync(markingText, 0.8, angle + theta);
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
