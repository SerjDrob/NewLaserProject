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
    }
}
