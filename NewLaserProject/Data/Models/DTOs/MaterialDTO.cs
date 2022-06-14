using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Data.Models.DTOs
{
    [Description("Материал")]
    public class MaterialDTO
    {
        [DisplayName("Толщина"), Range(0.1, 2),Category("Параметры")]
        public float Thickness { get; set; }

        [DisplayName("Наименование"),Required,Category("Параметры")]
        public string Name { get; set; }
    }
}
