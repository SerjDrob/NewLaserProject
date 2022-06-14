using MachineClassLibrary.Laser.Entities;

namespace NewLaserProject.Data.Models
{
    public class DefaultLayerEntityTechnology:BaseEntity
    {
        public int DefaultLayerFilterId { get; set; }
        public DefaultLayerFilter DefaultLayerFilter { get; set; }
        public LaserEntity EntityType { get; set; }
        public Technology Technology { get; set; }
        public int TechnologyId { get; set; }
    }
}
