namespace NewLaserProject.Data.Models
{
    public class MaterialEntRule:BaseEntity
    {
        public double Offset { get; set; }
        public double Width { get; set; }
        public int MaterialId { get; set; }
        public Material Material { get; set; }
    }
}
