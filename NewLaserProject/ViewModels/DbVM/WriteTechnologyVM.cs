using System.ComponentModel.DataAnnotations;
using MachineClassLibrary.Laser.Parameters;
using NewLaserProject.ViewModels.DialogVM;

namespace NewLaserProject.ViewModels.DbVM
{
    internal class WriteTechnologyVM : CommonDialogResultable<TechWizardVM>
    {
        public WriteTechnologyVM(ExtendedParams defaultParams)
        {
           TechnologyWizard = new(defaultParams);
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
        public TechWizardVM TechnologyWizard
        {
            get; set;
        }

        public override void SetResult() => SetResult(TechnologyWizard);
    }
}
