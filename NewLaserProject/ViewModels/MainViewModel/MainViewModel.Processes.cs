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
        public int ProcessingObjectIndex { get; set; } = -1;

        [ICommand]
        private void ProcGridSelection(SelectionChangedEventArgs e)
        {
            var dataGrid = e.Source as DataGrid;
            if (dataGrid != null && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                // find row for the first selected item
                //DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(e.AddedItems[0]);
                //if (row != null && row.Item != null)
                //{
                //    dataGrid.ScrollIntoView(row.Item);
                //}
                dataGrid.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void ProcessGridChangeSelection(int index)
        {
            foreach (var item in ProcessingObjects)
            {
                item.IsBeingProcessed = false;
            }
            ProcessingObjects[index].IsBeingProcessed = true;
            ProcessingObjects=new(ProcessingObjects);
        }

        public Technology CurrentTechnology { get; set; }
        public string CurrentLayerFilter { get; set; }
        public LaserEntity CurrentEntityType { get; set; }

        [ICommand]
        private async Task StartProcess2()
        {
            var laserSettingsjson = File.ReadAllText(ProjectPath.GetFilePathInFolder("AppSettings", "DefaultLaserParams.json"));

            var laserParams = new JsonDeserializer<MarkLaserParams>()  
                .SetKnownType<PenParams>()
                .SetKnownType<HatchParams>()
                .Deserialize(laserSettingsjson);

            _laserMachine.SetMarkParams(laserParams);

            var topologySize = _dxfReader.GetSize();

            var wafer = new LaserWafer<Curve>(_dxfReader.GetAllCurves("PAZ"), topologySize);
            var waferPoints = new LaserWafer<Point>(_dxfReader.GetPoints(), topologySize);
            wafer.Scale(1F / FileScale);
            waferPoints.Scale(1F / FileScale);
            if (WaferTurn90) wafer.Turn90();
            if (WaferTurn90) waferPoints.Turn90();
            if (MirrorX) wafer.MirrorX();
            if (MirrorX) waferPoints.MirrorX();

            _pierceSequenceJson = File.ReadAllText( ProjectPath.GetFilePathInFolder("TechnologyFiles","CircleListing2.json"));
            var coorSystem = _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);

            var points = waferPoints.Cast<PPoint>();
            
            var entityPreparator = new EntityPreparator(_dxfReader, ProjectPath.GetFolderPath("TempFiles"));

            _mainProcess = new ThreePointProcess(wafer, points, _pierceSequenceJson, _laserMachine,
                        coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                        Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle, entityPreparator);

            //_mainProcess.SwitchCamera += _threePointsProcess_SwitchCamera;
            try
            {
                OnProcess = true;
                await _mainProcess.StartAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                OnProcess = false;
            }
        }

        [ICommand]
        private async Task StartProcess()
        {
            var laserSettingsjson = File.ReadAllText(ProjectPath.GetFilePathInFolder("AppSettings", "DefaultLaserParams.json"));

            var laserParams = new JsonDeserializer<MarkLaserParams>()
                .SetKnownType<PenParams>()
                .SetKnownType<HatchParams>()
                .Deserialize(laserSettingsjson);

            _laserMachine.SetMarkParams(laserParams);
            //TODO determine size by specified layer
            var topologySize = _dxfReader.GetSize();

            ITransformable wafer = CurrentEntityType switch
            {
                LaserEntity.Curve => new LaserWafer<Curve>(_dxfReader.GetAllCurves(CurrentLayerFilter), topologySize),               
                LaserEntity.Circle => new LaserWafer<Circle>(_dxfReader.GetCircles(CurrentLayerFilter), topologySize)
            };

            wafer.SetRestrictingArea(0, 0, WaferWidth, WaferHeight);
            wafer.Scale(1F / FileScale);
            if (WaferTurn90) wafer.Turn90();
            if (MirrorX) wafer.MirrorX();


            _pierceSequenceJson = File.ReadAllText(ProjectPath.GetFilePathInFolder("TechnologyFiles", $"{CurrentTechnology.ProcessingProgram}.json"));
            var coorSystem = _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);
            var entityPreparator = new EntityPreparator(_dxfReader, ProjectPath.GetFolderPath("TempFiles"));

            switch (CurrentEntityType)
            {
                case LaserEntity.Circle:
                    {
                        _mainProcess = new LaserProcess((IEnumerable<IProcObject>)wafer, _pierceSequenceJson, _laserMachine,
                                        coorSystem, Settings.Default.ZeroPiercePoint, WaferThickness, entityPreparator);
                    }
                    break;

                case LaserEntity.Curve:
                    {
                        var waferPoints = new LaserWafer<Point>(_dxfReader.GetPoints(), topologySize);
                        
                        waferPoints.SetRestrictingArea(0, 0, WaferWidth, WaferHeight);
                        if (waferPoints.Count()<3)
                        {
                            techMessager.RealeaseMessage("Невозможно запустить процесс. В области пластины должно быть три референтных точки.", Icon.Exclamation);
                            return;
                        }
                        
                        waferPoints.Scale(1F / FileScale);
                        if (WaferTurn90) waferPoints.Turn90();
                        if (MirrorX) waferPoints.MirrorX();
                        var points = waferPoints.Cast<PPoint>();

                        _mainProcess = new ThreePointProcess((IEnumerable<IProcObject>)wafer, points, _pierceSequenceJson, _laserMachine,
                                        coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                                        Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle, entityPreparator);
                    }
                    break;
                case LaserEntity.Line:
                    break;
                case LaserEntity.Point:
                    break;
                case LaserEntity.None:
                    break;
                default:
                    break;
            }

            //_mainProcess.SwitchCamera += _threePointsProcess_SwitchCamera;
            try
            {
                OnProcess = true;
                await _mainProcess.StartAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                OnProcess = false;
            }
        }


        private TestThreePoints _testThreePointsProcess;

        [ICommand]
        private async Task StartTestThreePoints()
        {


            var topologySize = _dxfReader.GetSize();

            var wafer = new LaserWafer<DxfCurve>(_dxfReader.GetAllDxfCurves2(Path.Combine(_projectDirectory, "TempFiles"), "PAZ"), topologySize);
            var waferPoints = new LaserWafer<MachineClassLibrary.Laser.Entities.Point>(_dxfReader.GetPoints(), topologySize);
            
            wafer.Scale(1F / FileScale);
            waferPoints.Scale(1F / FileScale);
            if (WaferTurn90) wafer.Turn90();
            if (WaferTurn90) waferPoints.Turn90();
            if (MirrorX) wafer.MirrorX();
            if (MirrorX) waferPoints.MirrorX();

            _pierceSequenceJson = File.ReadAllText($"{_projectDirectory}/TechnologyFiles/CircleListing.json");
            var coorSystem = _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);

            var points = waferPoints.Cast<PPoint>();

            _testThreePointsProcess = new TestThreePoints(points, _laserMachine,
                        coorSystem, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                        Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle, Settings.Default.ZeroPiercePoint);

            _testThreePointsProcess.SwitchCamera += _threePointsProcess_SwitchCamera;
            try
            {
                OnProcess = true;
                await _testThreePointsProcess.StartAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                OnProcess = false;
            }

        }

        [ICommand]
        private Task TTPNext()
        {
            return _testThreePointsProcess.Next();
        }


        private void _threePointsProcess_SwitchCamera(object? sender, bool e)
        {
            if (e)
            {
                StartVideoCapture();
            }
            else
            {
                StopVideoCapture();
            }
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
