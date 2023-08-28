using System.ComponentModel;

namespace NewLaserProject.Data.Models
{
    public class MaterialEntRule:BaseEntity
    {
        public double Offset { get; set; }
        public double Width { get; set; }
        [Browsable(false)]
        public int MaterialId { get; set; }
        [Browsable(false)]
        public Material Material { get; set; }
    }
}
