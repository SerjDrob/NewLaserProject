using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NewLaserProject.Data.Models
{
    public class Technology:BaseEntity, ICloneable
    {
        [Required]
        public string ProcessingProgram { get; set; }
        [Required]
        public string ProgramName { get; set; }
        public DateTime Created { get; set; }
        public DateTime Altered { get; set; }
        public int MaterialId { get; set; }
        public Material Material { get; set; }
        public List<DefaultLayerEntityTechnology> DefaultLayerEntityTechnologies  { get; set; }
        public object Clone() => MemberwiseClone();
    }
}
