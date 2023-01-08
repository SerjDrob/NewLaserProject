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
    internal class ObjsToProcess
    {
        public IDictionary<string, IEnumerable<(string objType, int count)>> Structure { get; init; }
        public ObjsToProcess(IDictionary<string, IEnumerable<(string objType, int count)>> layersStructure)
        {
            Structure = layersStructure;
            LaserEntity = LaserEntity.None;
        }

        public string Layer { get; set; }
        public LaserEntity LaserEntity { get; set; }
    }

    internal partial class MainViewModel
    {
        public ObservableCollection<IProcObject> ProcessingObjects { get; set; } //= new();
        public ObservableCollection<ObjsToProcess> ObjectsForProcessing { get; set; } = new();
        public IProcObject IsBeingProcessedObject { get; set; }
        public FileAlignment FileAlignment { get; set; }
        public Technology CurrentTechnology { get; set; }
        public string CurrentLayerFilter { get; set; }
        public LaserEntity CurrentEntityType { get; set; }
        public bool IsWaferMark { get; set; }

        [ICommand]
        private async Task StartStopProcess(object arg)
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
                CancelProcess();
                await _appStateMachine.FireAsync(AppTrigger.EndProcess);

                //OnProcess = false;
            }
        }

        [ICommand]
        private void AddObjectToProcess()
        {
            ObjectsForProcessing.Add(new ObjsToProcess(LayersStructure));
        }

        [ICommand]
        private void RemoveObjectFromProcess(ObjsToProcess @object)
        {
            ObjectsForProcessing.Remove(@object);
        }

        [ICommand]
        private void DownloadProcess()
        {
            //TODO determine size by specified layer
            var topologySize = _dxfReader.GetSize();

            var procObjects = (CurrentEntityType switch
            {
                LaserEntity.Curve => _dxfReader.GetAllCurves(CurrentLayerFilter).Cast<IProcObject>(),
                LaserEntity.Circle => _dxfReader.GetCircles(CurrentLayerFilter).Cast<IProcObject>()
            }).Concat(
                        ObjectsForProcessing
                        .Where(o => o.LaserEntity == LaserEntity.Circle | o.LaserEntity == LaserEntity.Curve)
                        .SelectMany(o => o.LaserEntity switch
                        {
                            LaserEntity.Curve => _dxfReader.GetAllCurves(o.Layer).Cast<IProcObject>(),
                            LaserEntity.Circle => _dxfReader.GetCircles(o.Layer).Cast<IProcObject>()
                        })).ToList();

            var wafer = new LaserWafer(procObjects, topologySize);


            wafer.SetRestrictingArea(0, 0, WaferWidth, WaferHeight);
            wafer.Scale(1F / FileScale);
            if (WaferTurn90) wafer.Turn90();
            if (MirrorX) wafer.MirrorX();
            wafer.OffsetX((float)WaferOffsetX);
            wafer.OffsetY((float)WaferOffsetY);

            _pierceSequenceJson = File.ReadAllText(ProjectPath.GetFilePathInFolder("TechnologyFiles", $"{CurrentTechnology.ProcessingProgram}.json"));
            var entityPreparator = new EntityPreparator(_dxfReader, ProjectPath.GetFolderPath("TempFiles"));

            switch (FileAlignment)
            {
                case FileAlignment.AlignByCorner:
                    {
                        var dx = Settings.Default.XRightPoint - Settings.Default.XLeftPoint;
                        var dy = Settings.Default.YRightPoint - Settings.Default.YLeftPoint;

                        var angle = Math.Atan2(dy, dx);
                        
                        entityPreparator.SetEntityAngle(angle)
                            .AddEntityAngle(-Settings.Default.PazAngle);//TODO do this in the three point proces too!!!!

                        var coorSystem = _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderLaser);
                        _mainProcess = new LaserProcess(wafer, _pierceSequenceJson, _laserMachine,
                                        coorSystem, Settings.Default.ZeroPiercePoint, WaferThickness, entityPreparator);
                    }
                    break;

                case FileAlignment.AlignByThreePoint:
                    {
                        var pts = _dxfReader.GetPoints();
                        var waferPoints = new LaserWafer(pts, topologySize);

                        waferPoints.Scale(1F / FileScale);
                        if (WaferTurn90) waferPoints.Turn90();
                        if (MirrorX) waferPoints.MirrorX();
                        waferPoints.OffsetX((float)WaferOffsetX);
                        waferPoints.OffsetY((float)WaferOffsetY);



                        var coorSystem = (CoorSystem<LMPlace>)_coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);


#if notSnap
                        waferPoints.SetRestrictingArea(0, 0, WaferWidth, WaferHeight);
                        if (waferPoints.Count() < 3)
                        {
                            techMessager.RealeaseMessage("Невозможно запустить процесс. В области пластины должно быть три референтных точки.", MessageType.Exclamation);
                            return;
                        }

                        var points = waferPoints.Cast<PPoint>();



                        _mainProcess = new ThreePointProcess(wafer, points, _pierceSequenceJson, _laserMachine,
                                        coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                                        Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle, entityPreparator, _subjMediator);
#endif



#if Snap
                        //------SnapTest--------------

                        var serviceWafer = new LaserWafer(topologySize);
                        serviceWafer.Scale(1F / FileScale);
                        if (WaferTurn90) serviceWafer.Turn90();
                        if (MirrorX) serviceWafer.MirrorX();
                        serviceWafer.OffsetX((float)WaferOffsetX);
                        serviceWafer.OffsetY((float)WaferOffsetY);

                        _mainProcess = new ThreePointProcesSnap(/*wafer*/ procObjects, serviceWafer, _pierceSequenceJson, _laserMachine,//TODO das experiment
                                                               coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                                                               Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle, entityPreparator, _subjMediator);
                        //----------------------------  
#endif
                    }
                    break;

                default:
                    break;
            }

            ProcessingObjects = new(wafer);
            ProcessingObjects.CollectionChanged += ProcessingObjects_CollectionChanged;

            //_mainProcess.CurrentWaferChanged += _mainProcess_CurrentWaferChanged;
            //_mainProcess.ProcessingObjectChanged += _mainProcess_ProcessingObjectChanged;
            //_mainProcess.ProcessingCompleted += _mainProcess_ProcessingCompleted;


            _mainProcess.OfType<ProcWaferChanged>()
                .Subscribe(args =>
                {
                    ProcessingObjects = new(args.Wafer);
                });

            _mainProcess.OfType<ProcObjectChanged>()
                .Where(poargs => poargs.ProcObject.IsProcessed)
                .Subscribe(args =>
                {
                    var o = ProcessingObjects.SingleOrDefault(po => po.Id == args.ProcObject.Id);
                    ProcessingObjects.Remove(o);
                });

            _mainProcess.OfType<ProcObjectChanged>()
                .Where(poargs => !poargs.ProcObject.IsProcessed & poargs.ProcObject.IsBeingProcessed)
                .Subscribe(poargs =>
                {
                    IsBeingProcessedObject = ProcessingObjects.SingleOrDefault(o => o.Id == poargs.ProcObject.Id);
                    //IsBeingProcessedIndex = poargs.ProcObject.index + 1;
                });

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
                });


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
                });



            HideProcessPanel(false);

        }

        private async Task StartProcess()
        {

#if PCIInserted

            try
            {
                var laserSettingsjson = File.ReadAllText(ProjectPath.GetFilePathInFolder("AppSettings", "DefaultLaserParams.json"));

                var laserParams = new JsonDeserializer<MarkLaserParams>()
                    .SetKnownType<PenParams>()
                    .SetKnownType<HatchParams>()
                    .Deserialize(laserSettingsjson);

                _laserMachine.SetMarkParams(laserParams);

                OnProcess = true;
                Trace.TraceInformation($"The process started");
                Trace.WriteLine($"File's name: {FileName}");
                Trace.WriteLine($"Layer's name for processing: {CurrentLayerFilter}");
                Trace.WriteLine($"Entity type for processing: {CurrentEntityType}");
                Trace.Flush();
                await _mainProcess.StartAsync();

            }
            catch (Exception ex)
            {

                throw;
            }
#endif

        }

        //private void _mainProcess_ProcessingCompleted(object? sender, ProcessCompletedEventArgs args)
        //{
        //    var status = args.Status;
        //    switch (status)
        //    {
        //        case CompletionStatus.Success:
        //            if (IsWaferMark)
        //            {
        //                MarkWaferAsync(MarkPosition, 1, 0.1, args.CoorSystem)
        //                    .ContinueWith(t => techMessager.RealeaseMessage("Процесс завершён", MessageType.Info),TaskScheduler.Default);
        //            }
        //            else
        //            {
        //                techMessager.RealeaseMessage("Процесс завершён", MessageType.Info);
        //            }
        //            break;
        //        case CompletionStatus.Cancelled:
        //            techMessager.RealeaseMessage("Процесс отменён", MessageType.Exclamation);
        //            break;
        //        default:
        //            break;
        //    }
        //    _appStateMachine.Fire(AppTrigger.EndProcess);
        //}

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
        public MarkPosition MarkPosition { get; set; }
        //private void _mainProcess_ProcessingObjectChanged(object? sender, (IProcObject procObj, int index) e)
        //{
        //    if (e.procObj.IsProcessed)
        //    {
        //        var o = ProcessingObjects.SingleOrDefault(po => po.Id == e.procObj.Id);
        //        ProcessingObjects.Remove(o);
        //    }
        //    else
        //    {
        //        IsBeingProcessedObject = ProcessingObjects.SingleOrDefault(o => o.Id == e.procObj.Id);
        //        IsBeingProcessedIndex = e.index + 1;
        //    }
        //}

        //private void _mainProcess_CurrentWaferChanged(object? sender, IEnumerable<IProcObject> e)
        //{
        //    ProcessingObjects = new(e);
        //}

        private void CancelProcess()
        {
            _mainProcess?.Deny();
        }


        [ICommand]
        private Task TPProcessNext()
        {
            return _mainProcess.Next();
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
