using System.Windows;
using MachineClassLibrary.Laser.Entities;

namespace NewLaserProject.ViewModels
{
    public class ProcObjTabVM
    {
        public int Index
        {
            get;
            set;
        }
        public IProcObject ProcObject
        {
            get;
            set;
        }
        public Visibility Visibility { get; set; } = Visibility.Visible;
    }
}
