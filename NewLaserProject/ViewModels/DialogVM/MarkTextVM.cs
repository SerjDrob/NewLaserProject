using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;

namespace NewLaserProject.ViewModels.DialogVM
{
    [AddINotifyPropertyChangedInterface]
    internal partial class MarkTextVM : CommonDialogResultable<MarkTextVM>
    {
        public string? MarkedText { get; set; }
        public string? FileName { get; set; }

        public bool IsDateEnable { get; set; } = true;
        public bool IsTimeEnable { get; set; } = true;
        public double TextHeight { get; set; } = 0.8;
        public double EdgeGap { get; set; } = 0.1;
        [ICommand]
        private void ResetText() => MarkedText = FileName;
        public override void SetResult() => SetResult(this);
    }
}
