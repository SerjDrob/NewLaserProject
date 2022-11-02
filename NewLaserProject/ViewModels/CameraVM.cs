using GongSolutions.Wpf.DragDrop;
using MachineClassLibrary.VideoCapture;
using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Controls;
using MediatR;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;


namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class CameraVM
    {
        private readonly IMediator _mediator;
        private readonly Mediator<ScopedGeomsRequest, SnapShot> _mediator2;

        public CameraVM()
        {

        }
        public CameraVM(IMediator mediator)
        {
            _mediator = mediator;
        }
        public CameraVM(Mediator<ScopedGeomsRequest, SnapShot> mediator)
        {
            _mediator2 = mediator;
        }
        public bool SnapshotVisible { get; set; }
        public double ScaleMarkersRatioFirst { get; private set; } = 0.1;//TODO should it come from out there?
        public double ScaleMarkersRatioSecond { get => 1 - ScaleMarkersRatioFirst; }
        public bool TeachScaleMarkerEnable { get; set; } = false;

        public BitmapImage? CameraImage { get; set; }
        public event EventHandler<(double x, double y)>? VideoClicked;
        public void OnVideoSourceBmpChanged(object? sender, VideoCaptureEventArgs e)
        {
            CameraImage = e.Image;
        }

        [ICommand]
        private void VideoClick((double x, double y) coordinates) => VideoClicked?.Invoke(this, coordinates);

        [ICommand]
        private async Task OpenTargetWindow()
        {
            var request = new ScopedGeomsRequest(3000, 3000, 10000, 10000);
            //var response = await _mediator.Send(request);

            var response = await _mediator2.Send(request);
            SnapshotVisible = true;
            SnapShot = response;
        }

        public SnapShot SnapShot { get; set; }
    }
}
