using System.Collections.Generic;

namespace NewLaserProject.Data.Models
{

    public class DefaultLayerFilter : BaseEntity
    {
        public string Filter { get; set; }
        public bool IsVisible { get; set; }
        public List<DefaultLayerEntityTechnology>? DefaultLayerEntityTechnologies { get; set; }
    }
}
