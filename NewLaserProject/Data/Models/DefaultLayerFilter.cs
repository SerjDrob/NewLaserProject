using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Data.Models
{

    public class DefaultLayerFilter:BaseEntity
    {
        public string Filter { get; set; }
        public bool IsVisible { get; set; }
        public List<DefaultLayerEntityTechnology>? DefaultLayerEntityTechnologies { get; set; }
    }
}
