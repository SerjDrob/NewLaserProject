#define Snap

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using HandyControl.Controls;
using HandyControl.Data;
using Humanizer;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process;
using NewLaserProject.Data.Models;
using NewLaserProject.Properties;
using NewLaserProject.Views;

namespace NewLaserProject.ViewModels
{

    internal partial class MainViewModel
    {
        public ObservableCollection<ProcObjTabView> ProcessingObjects
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
            get; set;
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
                    _processTimer = new Timer(1000);
                    _procStartTime = DateTime.Now;
                    _processTimer.Elapsed += _processTimer_Elapsed;
                    _processTimer.Start();
                    await _appStateMachine.FireAsync(AppTrigger.StartProcess);
                }
                else
                {
                    if (_processTimer is not null)
                    {
                        _processTimer.Elapsed -= _processTimer_Elapsed;
                        _processTimer.Stop();
                        _processTimer.Close();
                        _processTimer.Dispose();
                    }
                    await _mainProcess.Deny().ConfigureAwait(false);
                    await _appStateMachine.FireAsync(AppTrigger.EndProcess).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _processTimer?.Dispose();
                throw;
            }
            finally
            {
                //var current = DateTime.Now;
                //var dt = new DateTime(0);
                //var timer = new Timer(1000);
                //timer.Elapsed += (s, e) =>
                //{
                //    dt = dt.AddSeconds(1);
                //    CurrentProcObjectTimer = dt.ToString("mm:ss");// (e.SignalTime - current).ToString();
                //};
                //timer.Start();
            }
        }

        private void _processTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            TotalProcessTimer = (e.SignalTime - _procStartTime).ToString(@"hh\:mm\:ss");
            if(_currObjectStarted) _procObjTempTime = _procObjTempTime.AddSeconds(1);
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


        [ICommand]
        private void DenyDownloadedProcess()
        {
            _currentProcSubscriptions?.ForEach(s => s.Dispose());
            _mainProcess?.Dispose();
            OnProcess = false;
            //Growl.Clear();
            IsProcessPanelVisible = false;
            HideRightPanel(false);
            ChangeViews(false);
            _cameraVM.SnapShotButtonVisible = false;
            _cameraVM.SnapshotVisible = false;
            IsProcessLoaded = false;
            _openedFileVM.IsCircleButtonVisible = true;
        }
        [ICommand]
        private void AddObjectToProcess() => ObjectsForProcessing.Add(new ObjsToProcess(LayersStructure));

        [ICommand]
        private void RemoveObjectFromProcess(ObjsToProcess @object) => ObjectsForProcessing.Remove(@object);

        [ICommand]
        private void DownloadProcess()
        {
            //TODO determine size by specified layer
            try
            {
                _currentProcSubscriptions = new();
                var topologySize = _dxfReader.GetSize();
                var procObjects = (CurrentEntityType switch
                {
                    LaserEntity.Curve => _dxfReader.GetAllCurves(CurrentLayerFilter).Cast<IProcObject>(),
                    LaserEntity.Circle => _dxfReader.GetCircles(CurrentLayerFilter).Cast<IProcObject>(),
                    LaserEntity.Point or LaserEntity.Line or LaserEntity.None => throw new ArgumentOutOfRangeException($"{CurrentEntityType} is not valid entity type for processing")
                }).Concat(
                            ObjectsForProcessing
                            .Where(o => o.LaserEntity == LaserEntity.Circle | o.LaserEntity == LaserEntity.Curve)
                            .SelectMany(o => o.LaserEntity switch
                            {
                                LaserEntity.Curve => _dxfReader.GetAllCurves(o.Layer).Cast<IProcObject>(),
                                LaserEntity.Circle => _dxfReader.GetCircles(o.Layer).Cast<IProcObject>()
                            })).ToList();

                if (!procObjects.Any()) throw new ArgumentException("Объекты для обработки отсутствуют.");

                var wafer = new LaserWafer(procObjects, topologySize);
                var serviceWafer = new LaserWafer(topologySize);

                wafer.SetRestrictingArea(0, 0, WaferWidth, WaferHeight);
                wafer.Scale(1F / DefaultFileScale);
                serviceWafer.Scale(1 / DefaultFileScale);

                if (WaferTurn90)
                {
                    wafer.Turn90();
                    serviceWafer.Turn90();
                }
                if (MirrorX)
                {
                    wafer.MirrorX();
                    serviceWafer.MirrorX();
                }
                wafer.OffsetX((float)WaferOffsetX);
                wafer.OffsetY((float)WaferOffsetY);
                serviceWafer.OffsetX((float)WaferOffsetX);
                serviceWafer.OffsetY((float)WaferOffsetY);


                _pierceSequenceJson = File.ReadAllText(ProjectPath.GetFilePathInFolder("TechnologyFiles", $"{CurrentTechnology.ProcessingProgram}.json"));
                var entityPreparator = new EntityPreparator(_dxfReader, ProjectPath.GetFolderPath("TempFiles"));
                var materialEntRule = CurrentTechnology.Material.MaterialEntRule;
                if (materialEntRule is not null)
                {
                    entityPreparator.SetEntityContourOffset(materialEntRule.Offset);
                    entityPreparator.SetEntityContourWidth(materialEntRule.Width);
                }



                _mainProcess = new GeneralLaserProcess(
                    wafer: wafer,
                    serviceWafer: serviceWafer,
                    jsonPierce: _pierceSequenceJson,
                    laserMachine: _laserMachine,
                    zeroZPiercing: Settings.Default.ZeroPiercePoint,
                    zeroZCamera: Settings.Default.ZeroFocusPoint,
                    waferThickness: WaferThickness,
                    dX: Settings.Default.XOffset,
                    dY: Settings.Default.YOffset,
                    pazAngle: Settings.Default.PazAngle,
                    entityPreparator: entityPreparator,
                    subject: _subjMediator,
                    baseCoorSystem: _coorSystem,
                    underCamera: false,//true,
                    aligningPoints: FileAlignment,
                    waferAngle: _waferAngle,
                    scale: DefaultFileScale);


                //ProcessingObjects = new(wafer);
                //ProcessingObjects.CollectionChanged += ProcessingObjects_CollectionChanged;

                _mainProcess.OfType<ProcWaferChanged>()
                    .Subscribe(args =>
                    {
                        ProcessingObjects = new();
                        args.Wafer.Aggregate(1, (ind, p) =>
                        {
                            ProcessingObjects.Add(new ProcObjTabView { Index = ind, ProcObject = p });
                            return ++ind;
                        });
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcObjectChanged>()
                    .Where(poargs => poargs.ProcObject.IsProcessed)
                    .Subscribe(args =>
                    {
                        _currObjectStarted = false;
                        _procObjTempTime = new(0);
                        var o = ProcessingObjects.SingleOrDefault(po => po.ProcObject.Id == args.ProcObject.Id);
                        ProcessingObjects.Remove(o);
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcObjectChanged>()
                    .Where(poargs => !poargs.ProcObject.IsProcessed & poargs.ProcObject.IsBeingProcessed)
                    .Subscribe(poargs =>
                    {
                        _currObjectStarted = true;
                        IsBeingProcessedObject = ProcessingObjects.SingleOrDefault(o => o.ProcObject.Id == poargs.ProcObject.Id)?.ProcObject;
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
                                await _laserMachine.GoThereAsync(LMPlace.Loading);
                                break;
                            default:
                                break;
                        }
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
                Growl.Error(new GrowlInfo()
                {
                    StaysOpen = false,
                    Message = ex.Message,
                    ShowDateTime = false
                });
            }
            catch (FileNotFoundException)
            {
                Growl.Error($"Файл технологии \"{CurrentTechnology.ProgramName}\" не найден.");
            }
            catch(NullReferenceException)
            {
                if (CurrentTechnology is null) Growl.Error($"Файл технологии не выбран.");
            }
            catch(ArgumentException ex)
            {
                Growl.Error(ex.Message);
            }
        }

        private async Task StartProcessAsync()
        {

#if PCIInserted

            try
            {
                var laserSettingsJson = File.ReadAllText(ProjectPath.GetFilePathInFolder("AppSettings", "DefaultLaserParams.json"));

                var laserParams = new JsonDeserializer<MarkLaserParams>()
                    .SetKnownType<PenParams>()
                    .SetKnownType<HatchParams>()
                    .Deserialize(laserSettingsJson);

                _laserMachine.SetMarkParams(laserParams);

                OnProcess = true;
                Trace.TraceInformation($"The process started");
                Trace.WriteLine($"File's name: {FileName}");
                Trace.WriteLine($"Layer's name for processing: {CurrentLayerFilter}");
                Trace.WriteLine($"Entity type for processing: {CurrentEntityType}");
                Trace.Flush();
                await _mainProcess.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
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
            await _laserMachine.MarkTextAsync(markingText, 0.8, angle + theta);
        }

        private void ProcessingObjects_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //var oldItem = e?.OldItems?[0] as IProcObject;
            //var newItem = e?.NewItems?[0] as IProcObject;
            //if (oldItem is not null && newItem is not null)
            //{
            //    if (newItem.IsBeingProcessed & !oldItem.IsBeingProcessed)
            //    {
            //        IsBeingProcessedObject = newItem;
            //    }
            //}
        }

        [ICommand]
        private void ToProcChecked(IProcObject procObject)
        {
            if (procObject is not null)
            {
                procObject.ToProcess = true;
                _mainProcess?.IncludeObject(procObject);
            }
        }

        [ICommand]
        private void ToProcUnchecked(IProcObject procObject)
        {
            //if (procObject is not null)
            //{
            //    procObject.ToProcess = false;
            //    var index = ProcessingObjects.IndexOf(ProcessingObjects.SingleOrDefault(o => o.Id == procObject.Id));
            //    ProcessingObjects[index] = procObject;
            //    _mainProcess?.ExcludeObject(procObject);
            //}
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
