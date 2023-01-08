using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NewLaserProject.ViewModels.DbVM
{
    internal class WriteTechnologyVM
    {
        public WriteTechnologyVM()
        {
            TechnologyWizard = new();
        }
        [Required]
        public string TechnologyName { get; set; }
        public string MaterialName { get; set; }
        public double MaterialThickness { get; set; }
        public TechWizardViewModel TechnologyWizard { get; set; }
    }

    internal class MaterialEntRuleVM
    {
        [Category("Обработка контура")]
        [DisplayName("Смещение контура, мм")]
        //[Editor(typeof(MMPropertyEditor),typeof(MMPropertyEditor))]
        public double Offset { get; set; }
        [Category("Обработка контура")]
        [DisplayName("Ширина контура, мм")]
        public double Width { get; set; }
    }



    //public class MMPropertyEditor : PropertyEditorBase
    //{
    //    public MMPropertyEditor()
    //    {

    //    }

    //    public MMPropertyEditor(double minimum, double maximum)
    //    {
    //        Minimum = minimum;
    //        Maximum = maximum;
    //    }

    //    public double Minimum { get; set; }

    //    public double Maximum { get; set; }

    //    public override FrameworkElement CreateElement(PropertyItem propertyItem) => new NumericUpDown
    //    {
    //        IsReadOnly = propertyItem.IsReadOnly,
    //        Minimum = Minimum,
    //        Maximum = Maximum,
    //        Increment = 0.01
    //    };

    //    public override DependencyProperty GetDependencyProperty() => NumericUpDown.ValueProperty;
    //}
}
