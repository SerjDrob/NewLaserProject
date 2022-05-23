using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.ViewModels.DbVM
{
    internal class WriteTechnologyVM
    {
        public WriteTechnologyVM()
        {
            TechnologyWizard = new();
        }
        [Required]
        public string TechnologyName { get; set; }
        public string MaterialName { get; set; }
        public double MaterialThickness { get; set; }
        public TechWizardViewModel TechnologyWizard { get; set; }
    }
}
