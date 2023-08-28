using System.ComponentModel.DataAnnotations;
using NewLaserProject.ViewModels.DialogVM;

namespace NewLaserProject.ViewModels.DbVM
{
    internal class WriteTechnologyVM : CommonDialogResultable<TechWizardVM>
    {
        public WriteTechnologyVM()
        {
            TechnologyWizard = new();
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
