using MachineClassLibrary.Classes;
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
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
    {
        public ObservableCollection<IProcObject> ProcessingObjects { get; set; } //= new();
        public IProcObject IsBeingProcessedObject { get; set; }       
        public FileAlignment FileAlignment { get; set; }
                
        public Technology CurrentTechnology { get; set; }
        public string CurrentLayerFilter { get; set; }
        public LaserEntity CurrentEntityType { get; set; }

        
        [ICommand]
        private async Task StartStopProcess(object arg)
        {
            if ((bool)arg)
            {
                //OnProcess = true;
                await _appStateMachine.FireAsync(AppTrigger.StartProcess);
#if PCIInserted
                await _appStateMachine.FireAsync(AppTrigger.EndProcess);
#endif
            }
            else
            {
                await _appStateMachine.FireAsync(AppTrigger.EndProcess);

                //OnProcess = false;
            }
        }        
        
        private async Task StartProcess()
        {           
            //TODO determine size by specified layer
            var topologySize = _dxfReader.GetSize();

            ITransformable wafer = CurrentEntityType switch
            {
                LaserEntity.Curve => new LaserWafer<Curve>(_dxfReader.GetAllCurves(CurrentLayerFilter), topologySize),
                LaserEntity.Circle => new LaserWafer<Circle>(_dxfReader.GetCircles(CurrentLayerFilter).ToList(), topologySize)
            };

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
                        var coorSystem = _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderLaser);
                        _mainProcess = new LaserProcess((IEnumerable<IProcObject>)wafer, _pierceSequenceJson, _laserMachine,
                                        coorSystem, Settings.Default.ZeroPiercePoint, WaferThickness, entityPreparator);
                    }
                    break;

                case FileAlignment.AlignByThreePoint:
                    {
                        var pts = _dxfReader.GetPoints();
                        var waferPoints = new LaserWafer<Point>(pts, topologySize);
                        waferPoints.Scale(1F / FileScale);
                        if (WaferTurn90) waferPoints.Turn90();
                        if (MirrorX) waferPoints.MirrorX();
                        waferPoints.OffsetX((float)WaferOffsetX);
                        waferPoints.OffsetY((float)WaferOffsetY);

                        waferPoints.SetRestrictingArea(0, 0, WaferWidth, WaferHeight);
                        if (waferPoints.Count() < 3)
                        {
                            techMessager.RealeaseMessage("Невозможно запустить процесс. В области пластины должно быть три референтных точки.", MessageType.Exclamation);
                            return;
                        }

                        var points = waferPoints.Cast<PPoint>();
                        var coorSystem = _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);
                        _mainProcess = new ThreePointProcess((IEnumerable<IProcObject>)wafer, points, _pierceSequenceJson, _laserMachine,
                                        coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                                        Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle, entityPreparator, _mediator);
                    }
                    break;

                default:
                    break;
            }

            ProcessingObjects = new((IEnumerable<IProcObject>)wafer);
            ProcessingObjects.CollectionChanged += ProcessingObjects_CollectionChanged;
            //ProcessingObjects[13].IsBeingProcessed = true;
            //IsBeingProcessedObject = ProcessingObjects[13];
            //ProcessingObjects[4].IsProcessed = true;

            _mainProcess.CurrentWaferChanged += _mainProcess_CurrentWaferChanged;
            _mainProcess.ProcessingObjectChanged += _mainProcess_ProcessingObjectChanged;
            _mainProcess.ProcessingCompleted += _mainProcess_ProcessingCompleted;
            //_mainProcess.SwitchCamera += _threePointsProcess_SwitchCamera;
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

        private void _mainProcess_ProcessingCompleted(object? sender, EventArgs e)
        {
            techMessager.RealeaseMessage("Процесс завершён", MessageType.Info);
        }

        private void ProcessingObjects_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var oldItem = e?.OldItems?[0] as IProcObject;
            var newItem = e?.NewItems?[0] as IProcObject;
            if (oldItem is not null && newItem is not null)
            {
                if (newItem.IsBeingProcessed & !oldItem.IsBeingProcessed)
                {
                    IsBeingProcessedObject = newItem;
                }
            }
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
        private void _mainProcess_ProcessingObjectChanged(object? sender, (IProcObject procObj,int index) e)
        {
            if (e.index > -1)
            {
                TestGeometry = e.procObj.ToGeometry();   
                ProcessingObjects[e.index] = e.procObj;
              //  ProcessingObjects = new(ProcessingObjects);
            }
        }

        private void _mainProcess_CurrentWaferChanged(object? sender, IEnumerable<IProcObject> e)
        {
            ProcessingObjects = new(e);
        }

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
