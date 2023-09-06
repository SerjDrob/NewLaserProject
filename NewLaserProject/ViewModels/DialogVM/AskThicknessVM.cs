using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace NewLaserProject.ViewModels.DialogVM
{
    [INotifyPropertyChanged]
    internal partial class AskThicknessVM : CommonDialogResultable<double>
    {
        [DisplayName("Толщина")]
        public double Thickness
        {
            get; set;
        }
        public override void SetResult() => SetResult(Thickness);
    }
}
