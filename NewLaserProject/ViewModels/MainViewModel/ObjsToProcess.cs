#define Snap

using MachineClassLibrary.Laser.Entities;
using System.Collections.Generic;

namespace NewLaserProject.ViewModels
{
    internal class ObjsToProcess
    {
        public IDictionary<string, IEnumerable<(string objType, int count)>> Structure { get; init; }
        public ObjsToProcess(IDictionary<string, IEnumerable<(string objType, int count)>> layersStructure)
        {
            Structure = layersStructure;
            LaserEntity = LaserEntity.None;
        }

        public string Layer { get; set; }
        public LaserEntity LaserEntity { get; set; }
    }

}
