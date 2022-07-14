using IxMilia.Dxf.Objects;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Data.Models.DTOs;
using NewLaserProject.Data.Models;
using NewLaserProject.Properties;
using NewLaserProject.Views.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PropertyChanged;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class FileVM
    {
        private IDxfReader _dxfReader;
        public FileVM(double waferWidth, double waferHeight)
        {
            WaferWidth = waferWidth;
            WaferHeight = waferHeight;            
        }

        public void SetFileView(IDxfReader dxfReader,int fileScale, bool mirrorX, bool waferTurn90, double waferOffsetX,
            double waferOffsetY)
        {
            _dxfReader= dxfReader;
            FileScale= fileScale;
            MirrorX= mirrorX;
            WaferOffsetX= waferOffsetX;
            WaferOffsetY= waferOffsetY;
            WaferTurn90 = waferTurn90;
            OpenFile();
        }

        public void SetWaferDimensions(double width, double height)
        {
            WaferWidth = width;
            WaferHeight = height;
        }
        public void SetViewFinders(double CameraX, double CameraY, double LaserX, double LaserY)
        {
            CameraViewfinderX = CameraX;
            CameraViewfinderY = CameraY;
            LaserViewfinderX = LaserX;
            LaserViewfinderY = LaserY;
        }
        private void OpenFile()
        {
            var fileSize = _dxfReader.GetSize();
            FileSizeX = Math.Round(fileSize.width);
            FileSizeY = Math.Round(fileSize.height);
            LayGeoms = new LayGeomAdapter(_dxfReader).LayerGeometryCollections;

            MirrorX = Settings.Default.WaferMirrorX;
            WaferTurn90 = Settings.Default.WaferAngle90;
            WaferOffsetX = 0;
            WaferOffsetY = 0;
        }

        public double FileSizeX { get; set; }
        public double FileSizeY { get; set; }
        public double FieldSizeX { get => FileScale * WaferWidth; }
        public double FieldSizeY { get => FileScale * WaferHeight; }
        public double WaferWidth { get; set; } = 48;
        public double WaferHeight { get; set; } = 60;
        public int FileScale { get; set; } = 1000;
        [OnChangedMethod(nameof(TransChanged))]
        public bool MirrorX { get; set; } = true;
        [OnChangedMethod(nameof(TransChanged))]
        public bool WaferTurn90 { get; set; } = true;
        [OnChangedMethod(nameof(TransChanged))]
        public double WaferOffsetX { get; set; }
        [OnChangedMethod(nameof(TransChanged))]
        public double WaferOffsetY { get; set; }
        public double TeacherPointerX { get; set; }
        public double TeacherPointerY { get; set; }
        public bool TeacherPointerVisibility { get; set; } = false;
        public double CameraViewfinderX { get; set; } 
        public double CameraViewfinderY { get; set; }
        public double LaserViewfinderX { get; set; }
        public double LaserViewfinderY { get; set; }
        public event EventHandler TransformationChanged;
        
        public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();
        public Dictionary<string, bool> IgnoredLayers { get; set; }
        [ICommand]
        private void AlignWafer(object obj)
        {
            var param = (ValueTuple<object, object, object>)obj;
            var scaleX = (double)param.Item1;
            var scaleY = (double)param.Item2;
            var aligning = (Aligning)param.Item3;
            var size = _dxfReader.GetSize();
            var dx = WaferWidth * FileScale;
            var dy = WaferHeight * FileScale;

            WaferOffsetX = aligning switch
            {
                Aligning.Right or Aligning.RTCorner or Aligning.RBCorner => -(WaferTurn90 ? (size.height - dx) : (size.width - dx)) * scaleX / 2,
                Aligning.Left or Aligning.LTCorner or Aligning.LBCorner => (WaferTurn90 ? (size.height - dx) : (size.width - dx)) * scaleX / 2,
                Aligning.Top or Aligning.Bottom or Aligning.Center => 0,
            };

            WaferOffsetY = aligning switch
            {
                Aligning.Top or Aligning.RTCorner or Aligning.LTCorner => (WaferTurn90 ? (size.width - dy) : (size.height - dy)) * scaleY / 2,
                Aligning.Bottom or Aligning.RBCorner or Aligning.LBCorner => -(WaferTurn90 ? (size.width - dy) : (size.height - dy)) * scaleY / 2,
                Aligning.Right or Aligning.Left or Aligning.Center => 0
            };

        }

        [ICommand]
        private void ChangeMirrorX() => MirrorX ^= true;

        [ICommand]
        private void ChangeTurn90() => WaferTurn90 ^= true;


        private void TransChanged()
        {
            TransformationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
