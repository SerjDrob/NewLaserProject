using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Data.Models
{
    public class Substrate:BaseEntity
    {
        [Range(0.1,2)]
        public double Thickness { get; set; }
        public List<Material> Materials { get; set; }
    }
}
