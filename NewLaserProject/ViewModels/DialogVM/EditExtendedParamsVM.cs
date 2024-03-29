﻿using MachineClassLibrary.Laser.Parameters;
using MachineControlsLibrary.CommonDialog;

namespace NewLaserProject.ViewModels.DialogVM
{
    internal class EditExtendedParamsVM : CommonDialogResultable<ExtendedParams>
    {
        public EditExtendedParamsVM(ExtendedParams extendedParams)
        {
            ExtendedParams = extendedParams;
        }

        public ExtendedParams ExtendedParams
        {
            get;
            set;
        }

        public override void SetResult() => SetResult(ExtendedParams);
    }
}
