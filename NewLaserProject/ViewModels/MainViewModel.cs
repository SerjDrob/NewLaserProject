using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.VideoCapture;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using NewLaserProject.Views;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NewLaserProject.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal partial class MainViewModel
    {
        private readonly LaserMachine _laserMachine;
        private DxfReader _dxfReader;
        public BitmapImage CameraImage { get; set; }
        public AxisStateView XAxis { get; set; }
        public AxisStateView YAxis { get; set; }
        public AxisStateView ZAxis { get; set; }
        public LayersProcessingModel LPModel { get; set; }
        public TechWizardViewModel TWModel { get; set; }
        private string _pierceSequenceJson = string.Empty;
        public string FileName { get; set; } = "open new file";
        public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();

        public MainViewModel(LaserMachine laserMachine)
        {
            _laserMachine = laserMachine;
            _laserMachine.OnVideoSourceBmpChanged += _laserMachine_OnVideoSourceBmpChanged;
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;
        }
        public MainViewModel()
        {

        }
        private void _laserMachine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            switch (e.Axis)
            {
                case Ax.X:
                    XAxis = new AxisStateView(e.Position, e.NLmt, e.PLmt, e.MotionDone, e.MotionStart);
                    break;
                case Ax.Y:
                    YAxis = new AxisStateView(e.Position, e.PLmt, e.PLmt, e.MotionDone, e.MotionStart);
                    break;
                case Ax.Z:
                    ZAxis = new AxisStateView(e.Position, e.PLmt, e.PLmt, e.MotionDone, e.MotionStart);
                    break;
            }
        }

        private void _laserMachine_OnVideoSourceBmpChanged(object? sender, BitmapEventArgs e)
        {
            CameraImage = e.Image;
        }

        [ICommand]
        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "d:\\";
            openFileDialog.Filter = "dxf files (*.dxf)|*.dxf";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if ((bool)openFileDialog.ShowDialog())
            {
                //Get the path of specified file
                FileName = openFileDialog.FileName;
                _dxfReader = new DxfReader(FileName);
                LayGeoms = new LayGeomAdapter(_dxfReader.Document).CalcGeometry();
            }
            if (File.Exists(FileName))
            {
                LPModel = new(FileName);
                TWModel = new();
                LPModel.ObjectChosenEvent += TWModel.SetObjectsTC;
            }

        }
        [ICommand]
        private void StartProcess() 
        {
           
        }
        [ICommand]
        private void OpenLayersProcessing()
        {
            if (File.Exists(FileName))
            {
                new LayersView()
                {
                    DataContext = new LayersProcessingModel(FileName)
                }.Show();

            }
            else
            {
                MessageBox.Show("Имя файла неверно или файл не существует", "Внимание", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    public class LazerProcess
    {
        private readonly LaserMachine _machine;

        public LazerProcess(LaserMachine machine)
        {
            _machine = machine;
        }

        private void AddDiameter(double taper)
        {

        }
        private void AddZ(double deltaZ)
        {
           _machine.MoveAxInPosAsync(Ax.Z, /* current Z +*/ deltaZ, true).Wait();
        }
        private void Delay(int ms)
        {
            Task.Delay(ms).Wait();
        }
        private void Pierce(PenParams pen, HatchParams hatch)
        {
            var pc = new PCircle(10,12,0,new Circle { Radius = 0.1},"OTV");
            var par = new MarkLaserParams(pen, hatch);
            var cpp = new CirclePierceParams(0.05, 0.5, -0.001, 0.001, Material.Polycor);
            var parAdapt = new CircleParamsAdapter(cpp);
            IPerforatorBuilder pb = new PerforatorBuilder<Circle>(pc, par,parAdapt);
            var perf = pb.Build();
            perf.PierceObjectAsync();
        }
    }
}
/*
 *Pierce,
        AddDiameter,
        AddZ,
        Delay
 */