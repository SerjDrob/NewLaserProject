using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using HandyControl.Tools.Extension;
using MachineClassLibrary.Laser.Parameters;
using MachineControlsLibrary.CommonDialog;
using Microsoft.Extensions.Logging;
using NewLaserProject.ViewModels.DialogVM;

namespace NewLaserProject.ViewModels.DbVM
{
    internal class WriteTechnologyVM : TechWizardVM, ICommonDialog, IDialogResultable<CommonDialogResult<TechWizardVM>>
    {
        public WriteTechnologyVM(ExtendedParams defaultParams) : base(defaultParams)
        {
        }

        [Required]
        public string TechnologyName
        {
            get; set;
        }
        public string MaterialName
        {
            get; set;
        }
        public double MaterialThickness
        {
            get; set;
        }
        //public TechWizardVM TechnologyWizard
        //{
        //    get; set;
        //}

        public void SetResult() => SetResult(this);
        protected void SetResult(TechWizardVM result)
        {
            Result.CommonResult = result;
        }

        [Browsable(false)]
        public CommonDialogResult<TechWizardVM> Result
        {
            get;
            set;
        }
        [Browsable(false)]
        public Action CloseAction
        {
            get;
            set;
        }
        public void CloseWithSuccess()
        {
            Result = new CommonDialogResult<TechWizardVM> { Success = true };
            SetResult();
            CloseAction();
        }
        public void CloseWithCancel()
        {
            Result = new CommonDialogResult<TechWizardVM> { Success = false };
            SetResult();
            CloseAction();
        }
    }

}
