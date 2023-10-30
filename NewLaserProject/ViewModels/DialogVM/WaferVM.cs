using System.ComponentModel;
using MachineControlsLibrary.CommonDialog;

namespace NewLaserProject.ViewModels.DialogVM
{
    public class WaferVM : CommonDialogResultable<WaferVM>
    {
        [Category("Размеры")]
        [DisplayName("Ширина")]
        public double Width
        {
            get; set;
        }
        [Category("Размеры")]
        [DisplayName("Высота")]
        public double Height
        {
            get; set;
        }
        [Category("Размеры")]
        [DisplayName("Толщина")]
        public double Thickness
        {
            get; set;
        }
        [Browsable(false)]

        public override void SetResult() => SetResult(this);
    }

}
