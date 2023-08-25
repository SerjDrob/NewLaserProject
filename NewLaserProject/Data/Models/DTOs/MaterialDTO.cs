using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NewLaserProject.Data.Models.DTOs
{
    [Description("Материал")]
    public class MaterialDTO
    {
        [DisplayName("Толщина"), Range(0.1, 2), Category("Параметры")]
        public float Thickness
        {
            get; set;
        }

        [DisplayName("Наименование"), Required, Category("Параметры")]
        public string Name
        {
            get; set;
        }
    }
}
