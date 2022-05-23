using NewLaserProject.Data.Models.DTOs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NewLaserProject.Data.Models
{
    public class Material : BaseEntity
    {
        public Material()
        {

        }
        public Material(MaterialDTO material)
        {
            Thickness = material.Thickness;
            Name = material.Name;
        }
        [Required]
        [Range(0.1, 2)]
        public float Thickness { get; set; }

        [Required]
        public string Name { get; set; }

        public List<Technology>? Technologies { get; set; }
    }
}
