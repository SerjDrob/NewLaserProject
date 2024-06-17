using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace NewLaserProject.ViewModels.DialogVM
{
    [INotifyPropertyChanged]
    internal partial class TeachFocusVM : CommonDialogResultable<(double cameraFocus, double laserFocus)>
    {
        private readonly double _currentZ;

        public TeachFocusVM(double currentZ, LaserMachineSettings settings)
        {
            _currentZ = currentZ;
            ZCamera = settings.ZeroFocusPoint ?? throw new NullReferenceException($"{nameof(settings.ZeroFocusPoint)} is null in the ctor {nameof(TeachFocusVM)}"); ;
            ZLaser = settings.ZeroPiercePoint ?? throw new NullReferenceException($"{nameof(settings.ZeroPiercePoint)} is null in the ctor {nameof(TeachFocusVM)}"); ;
        }

        public double ZCamera { get; set; }
        public double ZLaser { get; set; }
        [ICommand]
        private async Task TeachZCamera()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Толщина подложки")
                .SetDataContext<AskThicknessVM>(vm => vm.Thickness = 0.5d)
                .GetCommonResultAsync<double>();

            if (result.Success)
            {
                ZCamera = _currentZ + result.CommonResult;
            }
        }
        [ICommand]
        private async Task TeachZLaser()
        {
            var result = await Dialog.Show<CommonDialog>()
                .SetDialogTitle("Толщина подложки")
                .SetDataContext<AskThicknessVM>(vm => vm.Thickness = 0.5d)
                .GetCommonResultAsync<double>();

            if (result.Success)
            {
                ZLaser = _currentZ + result.CommonResult;
            }
        }
        public override void SetResult() => SetResult((ZCamera, ZLaser));
    }
}
