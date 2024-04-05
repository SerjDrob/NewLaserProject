using HandyControl.Tools.Extension;
using MachineControlsLibrary.CommonDialog;

namespace NewLaserProject.ViewModels.DialogVM;

internal class AxisSettingsVM:CommonDialogResultable<AxisSettingsVM>
{
    public double VelLow
    {
        get; set;
    }
    public double VelHigh
    {
        get; set;
    }
    public double VelService
    {
        get; set;
    }
    public double Acc
    {
        get; set;
    }

    public override void SetResult() => SetResult(this);
   
}
