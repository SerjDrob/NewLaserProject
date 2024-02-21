using MachineClassLibrary.Laser.Entities;
using NewLaserProject.Data.Models;

namespace NewLaserProject.ViewModels
{
    public class ObjectForProcessing
    {
        public string? Layer
        {
            get; set;
        }
        public LaserEntity LaserEntity
        {
            get; set;
        }
        public Technology? Technology
        {
            get;
            set;
        }
        
        public void Deconstruct(out string layer, out LaserEntity laserEntity, out int technologyId)
        {
            layer = this.Layer ?? string.Empty;
            laserEntity = this.LaserEntity;
            technologyId = this.Technology?.Id ?? -1;
        }
    }
}
