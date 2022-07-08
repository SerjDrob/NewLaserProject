using MachineClassLibrary.VideoCapture;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class CameraVM
    {
        public BitmapImage CameraImage { get; set; }
        public void OnVideoSourceBmpChanged(object? sender, VideoCaptureEventArgs e)
        {
            CameraImage = e.Image;
        }

    }
}
