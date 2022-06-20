﻿using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process;
using NewLaserProject.Properties;
using NewLaserProject.Views;
using System;
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

        [ICommand]
        private async Task StartProcess()
        {
            var laserSettingsjson = File.ReadAllText(ProjectPath.GetFilePathInFolder("AppSettings", "DefaultLaserParams.json"));

            var laserParams = new JsonDeserializer<MarkLaserParams>()  
                .SetKnownType<PenParams>()
                .SetKnownType<HatchParams>()
                .Deserialize(laserSettingsjson);

            _laserMachine.SetMarkParams(laserParams);

            var topologySize = _dxfReader.GetSize();

            var wafer = new LaserWafer<DxfCurve>(_dxfReader.GetAllDxfCurves2(ProjectPath.GetFolderPath("TempFiles"), "PAZ"), topologySize);
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
        private async Task StartThreePointProcess()
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

            //_threePointsProcess = new ThreePointProcess(wafer, points, _pierceSequenceJson, _laserMachine,
            //            coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
            //            Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle);

            var entityPreparator = new EntityPreparator(_dxfReader, ProjectPath.GetFolderPath("TempFiles"));

            _mainProcess = new ThreePointProcess(wafer, points, _pierceSequenceJson, _laserMachine,
                        coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                        Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle);

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
