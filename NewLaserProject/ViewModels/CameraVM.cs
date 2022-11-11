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
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class CameraVM
    {
        private readonly ISubject<INotification> _mediator;
        public CameraVM(ISubject<INotification> mediator)
        {
            _mediator = mediator;
            _mediator.OfType<SnapShot>()
                .Subscribe(Handle);
            _mediator.OfType<PermitSnap>()
                .Subscribe(result => SnapShotButtonVisible = result.Permited);
            _mediator.OfType<SnapShotResult>()
                .Subscribe(result => SnapshotVisible = false);
        }
        public bool SnapshotVisible { get; set; }
        public bool SnapShotButtonVisible { get; set; }
        public double ScaleMarkersRatioFirst { get; private set; } = 0.1;//TODO should it come from out there?
        public double ScaleMarkersRatioSecond { get => 1 - ScaleMarkersRatioFirst; }
        public bool TeachScaleMarkerEnable { get; set; } = false;
        public SnapShot SnapShot { get; set; }

        public BitmapImage? CameraImage { get; set; }
        public event EventHandler<(double x, double y)>? VideoClicked;
        public void OnVideoSourceBmpChanged(object? sender, VideoCaptureEventArgs e)
        {
            CameraImage = e.Image;
        }

        [ICommand]
        private void VideoClick((double x, double y) coordinates) => VideoClicked?.Invoke(this, coordinates);

        [ICommand]
        private void OpenTargetWindow()
        {            
            _mediator.OnNext(new ReadyForSnap());
        }

        public void Handle(SnapShot notification)
        {
            SnapShot = notification;
            SnapshotVisible = true;
        }
    }
       
}
