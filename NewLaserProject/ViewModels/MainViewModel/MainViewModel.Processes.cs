using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process;
using NewLaserProject.Properties;
using NewLaserProject.Views;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NewLaserProject.ViewModels
{
    internal partial class MainViewModel
    {
        [ICommand]
        private async Task StartProcess()
        {
            //is dxf valid?
            //using var wafer = new LaserWafer<Circle>(_dxfReader.GetCircles(), topologySize);
            var topologySize = _dxfReader.GetSize();
            using var wafer = new LaserWafer<DxfCurve>(_dxfReader.GetAllDxfCurves2(Path.Combine(_projectDirectory, "TempFiles"), "PAZ"), topologySize);
            wafer.Scale(1F / FileScale);
            if (WaferTurn90) wafer.Turn90();
            if (MirrorX) wafer.MirrorX();
            _pierceSequenceJson = File.ReadAllText($"{_projectDirectory}/TechnologyFiles/CircleListing.json");
            var coorSystem = ProcessUnderCamera ? _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera) : _coorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderLaser);
            //var process = new LaserProcess2<Circle>(wafer, _pierceSequenceJson, _laserMachine, coorSystem, Settings.Default.ZeroPiercePoint);
            var process = new LaserProcess2<DxfCurve>(wafer, _pierceSequenceJson, _laserMachine, coorSystem, Settings.Default.ZeroPiercePoint, WaferThickness);

            try
            {
                OnProcess = true;
                await process.StartAsync();
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

            _threePointsProcess = new ThreePointProcess<DxfCurve>(wafer, points, _pierceSequenceJson, _laserMachine,
                        coorSystem, Settings.Default.ZeroPiercePoint, Settings.Default.ZeroFocusPoint, WaferThickness, techMessager,
                        Settings.Default.XOffset, Settings.Default.YOffset, Settings.Default.PazAngle);

            try
            {
                OnProcess = true;
                await _threePointsProcess.StartAsync();
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
        private Task TPProcessNext()
        {
            return _threePointsProcess.Next();
        }

        [ICommand]
        private void ChooseMaterial()
        {
            var material = new MaterialVM { Width = WaferWidth, Height = WaferHeight, Thickness = 0.5 };
            new MaterialSettingsView
            {
                DataContext = material
            }.ShowDialog();
            WaferWidth = material.Width;
            WaferHeight = material.Height;

        }
    }
}
