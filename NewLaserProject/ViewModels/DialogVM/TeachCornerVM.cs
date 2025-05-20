using System;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace NewLaserProject.ViewModels.DialogVM;

[INotifyPropertyChanged]
internal partial class TeachCornerVM : CommonDialogResultable<(double leftX, double leftY, double rightX, double rightY)>
{
    public TeachCornerVM(double currentX, double currentY, LaserMachineSettings settings)
    {
        _currentX = currentX;
        _currentY = currentY;
        XLeftPoint = settings.XLeftPoint ?? throw new NullReferenceException($"{nameof(settings.XLeftPoint)} is null in the ctor {nameof(TeachCornerVM)}");
        XRightPoint = settings.XRightPoint ?? throw new NullReferenceException($"{nameof(settings.XRightPoint)} is null in the ctor {nameof(TeachCornerVM)}");
        YLeftPoint = settings.YLeftPoint ?? throw new NullReferenceException($"{nameof(settings.YLeftPoint)} is null in the ctor {nameof(TeachCornerVM)}");
        YRightPoint = settings.YRightPoint ?? throw new NullReferenceException($"{nameof(settings.YRightPoint)} is null in the ctor {nameof(TeachCornerVM)}");
    }
    private readonly double _currentX;
    private readonly double _currentY;
    public double XLeftPoint { get; set; }
    public double YLeftPoint { get; set; }
    public double XRightPoint { get; set; }
    public double YRightPoint { get; set; }
    [ICommand]
    private void TeachLeftPoint() => (XLeftPoint, YLeftPoint) = (_currentX, _currentY);
    [ICommand]
    private void TeachRightPoint() => (XRightPoint, YRightPoint) = (_currentX, _currentY);
    public override void SetResult() => SetResult((XLeftPoint,YLeftPoint,XRightPoint,YRightPoint));
}
