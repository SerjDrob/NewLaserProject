using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.VideoCapture;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using NewLaserProject.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PropertyChanged;
using NewLaserProject.Views;

namespace NewLaserProject.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public partial class MainViewModel
    {
        private readonly LaserMachine _laserMachine;
        private DxfReader _dxfReader;
        public BitmapImage CameraImage { get; set; }
        public AxisStateView XAxis { get; set; }
        public AxisStateView YAxis { get; set; }
        public AxisStateView ZAxis { get; set; }
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

        }
        [ICommand]
        private void StartProcess() { }
        [ICommand]
        private void OpenLayersProcessing()
        {
            if (FileName!=string.Empty)
            {
                new LayersView()
                {
                    DataContext = new LayersProcessingModel(FileName)
                }.Show(); 
            }
        }
    }
}
