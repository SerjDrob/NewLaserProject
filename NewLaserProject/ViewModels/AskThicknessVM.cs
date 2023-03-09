using HandyControl.Tools.Extension;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class AskThicknessVM:IDialogResultable<AskThicknessVM>
    {
        public double Thickness { get; set; }
        public AskThicknessVM Result { get => this; set { } }
        public Action CloseAction { get; set; }
    }
}
