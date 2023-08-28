using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace NewLaserProject.ViewModels.DialogVM
{
    [INotifyPropertyChanged]
    internal partial class WriteEditTechnologyVM : CommonDialogResultable<TechWizardVM>
    {
        public WriteEditTechnologyVM(TechWizardVM techWizard)
        {
            Wizard = techWizard;
        }
        public TechWizardVM Wizard
        {
            get;
            set;
        }
        public override void SetResult() => SetResult(Wizard);
    }
}
