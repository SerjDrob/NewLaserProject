using MachineClassLibrary.VideoCapture;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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
        public double ScaleMarkersRatioFirst { get; private set; } = 0.1;//TODO should it come from out there?
        public double ScaleMarkersRatioSecond { get => 1 - ScaleMarkersRatioFirst; }
        public BitmapImage? CameraImage { get; set; }
        public event EventHandler<(double x, double y)>? VideoClicked;
        public void OnVideoSourceBmpChanged(object? sender, VideoCaptureEventArgs e)
        {
            CameraImage = e.Image;
        }
        
        [ICommand]
        private void VideoClick((double x, double y) coordinates) => VideoClicked?.Invoke(this, coordinates);

    }
}
