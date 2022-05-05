using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Properties;
using System.ComponentModel;

namespace NewLaserProject.ViewModels
{
    internal class MaterialVM
    {
        [Category("Размеры")]
        [DisplayName("Ширина")]
        public double Width { get; set; }
        [Category("Размеры")]
        [DisplayName("Высота")]
        public double Height { get; set; }
        [Category("Размеры")]
        [DisplayName("Толщина")]
        public double Thickness { get; set; }
    }
}
