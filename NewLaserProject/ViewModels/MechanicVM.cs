using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class MechanicVM
    {
        public  double TableX { get; set; }
        public double TableY { get; set; }
        public double TableWidth => 130;
        public double TableOriginX => 3.3;
        public double TableOriginY => 80.59;
        public double LaserOriginX => 0;
        public double LaserOriginY => 0;

        public void SetCoordinates(double x, double y)
        {
            TableX = x;
            TableY = y;
        }
    }
}
