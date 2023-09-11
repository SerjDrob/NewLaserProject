using System.ComponentModel;

namespace NewLaserProject.Data.Models
{
    public class MaterialEntRule:BaseEntity
    {
        /// <summary>
        /// Measured in um
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// Measured in um
        /// </summary>
        public int Width { get; set; }
        [Browsable(false)]
        public int MaterialId { get; set; }
        [Browsable(false)]
        public Material Material { get; set; }
    }
}
