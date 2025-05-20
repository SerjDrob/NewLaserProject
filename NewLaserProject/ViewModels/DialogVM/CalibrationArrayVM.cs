using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NewLaserProject.Classes;

namespace NewLaserProject.ViewModels.DialogVM;

[INotifyPropertyChanged]
internal partial class CalibrationArrayVM : CommonDialogResultable<ArraySize>
{
    public ArraySize Size { get; set; }
    public override void SetResult() => SetResult(Size);
}
