using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using HandyControl.Tools.Extension;

namespace NewLaserProject.ViewModels.DialogVM
{
    public class MaterialVM:CommonDialogResultable<MaterialVM>
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
