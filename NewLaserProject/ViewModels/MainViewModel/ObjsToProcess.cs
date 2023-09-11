using MachineClassLibrary.Laser.Entities;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using NewLaserProject.Data.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class ObjsToProcess
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
