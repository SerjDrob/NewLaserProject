﻿#define Snap

using HandyControl.Controls;
using HandyControl.Data;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace NewLaserProject.ViewModels
{

    internal partial class MainViewModel
    {
        public ObservableCollection<IProcObject> ProcessingObjects { get; set; } 
        public ObservableCollection<ObjsToProcess> ObjectsForProcessing { get; set; } = new();
        public IProcObject IsBeingProcessedObject { get; set; }
        public FileAlignment FileAlignment { get; set; }
        public ObservableCollection<object> Alignments { get; set; } = new( new(){ FileAlignment.AlignByThreePoint, FileAlignment.AlignByCorner, FileAlignment.AlignByTwoPoint });
        public Technology CurrentTechnology { get; set; }
        public string CurrentLayerFilter { get; set; }
        public LaserEntity CurrentEntityType { get; set; }
        public bool IsWaferMark { get; set; }
        public MarkPosition MarkPosition { get; set; }
        private List<IDisposable> _currentProcSubscriptions;

        [ICommand]
        private async Task StartStopProcess(object arg)
        {
            try
            {
                if ((bool)arg)
                {
                    //OnProcess = true;
                    await _appStateMachine.FireAsync(AppTrigger.StartProcess);
#if PCIInserted
                    // await _appStateMachine.FireAsync(AppTrigger.EndProcess);
#endif
                }
                else
                {
                    await _mainProcess.Deny().ConfigureAwait(false);
                    await _appStateMachine.FireAsync(AppTrigger.EndProcess).ConfigureAwait(false);

                    //OnProcess = false;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
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


                ProcessingObjects = new(wafer);
                ProcessingObjects.CollectionChanged += ProcessingObjects_CollectionChanged;

                _mainProcess.OfType<ProcWaferChanged>()
                    .Subscribe(args =>
                    {
                        ProcessingObjects = new(args.Wafer);
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcObjectChanged>()
                    .Where(poargs => poargs.ProcObject.IsProcessed)
                    .Subscribe(args =>
                    {
                        var o = ProcessingObjects.SingleOrDefault(po => po.Id == args.ProcObject.Id);
                        ProcessingObjects.Remove(o);
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcObjectChanged>()
                    .Where(poargs => !poargs.ProcObject.IsProcessed & poargs.ProcObject.IsBeingProcessed)
                    .Subscribe(poargs =>
                    {
                        IsBeingProcessedObject = ProcessingObjects.SingleOrDefault(o => o.Id == poargs.ProcObject.Id);
                    //IsBeingProcessedIndex = poargs.ProcObject.index + 1;
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);

                _mainProcess.OfType<ProcCompletionPreview>()
                    .Subscribe(args =>
                    {
                        var status = args.Status;
                        switch (status)
                        {
                            case CompletionStatus.Success:
                                if (IsWaferMark)
                                {
                                    MarkWaferAsync(MarkPosition, 1, 0.1, args.CoorSystem)
                                        .ContinueWith(t => techMessager.RealeaseMessage("Процесс завершён", MessageType.Info), TaskScheduler.Default);
                                }
                                else
                                {
                                    techMessager.RealeaseMessage("Процесс завершён", MessageType.Info);
                                }
                                break;
                            case CompletionStatus.Cancelled:
                                MessageBox.Fatal("Процесс отменён");
                                techMessager.RealeaseMessage("Процесс отменён", MessageType.Exclamation);
                                break;
                            default:
                                break;
                        }
                        _appStateMachine.Fire(AppTrigger.EndProcess);
                    })
                    .AddSubscriptionTo(_currentProcSubscriptions);


                _mainProcess.OfType<ProcessMessage>()
                    .Subscribe(args =>
                    {
                        switch (args.MessageType)
                        {
                            case Classes.MsgType.Request:
                                break;
                            case Classes.MsgType.Info:
                                Growl.Info(new GrowlInfo()
                                {
                                    StaysOpen = true,
                                    Message = args.Message,
                                    ShowDateTime = false
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
                //await _mainProcess.StartAsync(new System.Threading.CancellationToken());
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
            if (procObject is not null)
            {
                procObject.ToProcess = false;
                var index = ProcessingObjects.IndexOf(ProcessingObjects.SingleOrDefault(o => o.Id == procObject.Id));
                ProcessingObjects[index] = procObject;
                _mainProcess?.ExcludeObject(procObject);
            }
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
        
        [ICommand]
        private void ChooseMaterial()
        {
            var material = new MaterialVM { Width = WaferWidth, Height = WaferHeight, Thickness = WaferThickness };
            new MaterialSettingsView
            {
                DataContext = material
            }.ShowDialog();
            WaferWidth = material.Width;
            WaferHeight = material.Height;
            WaferThickness = material.Thickness;
        }
    }

}
