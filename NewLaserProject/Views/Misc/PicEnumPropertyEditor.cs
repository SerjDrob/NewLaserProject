using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;
using MachineControlsLibrary.Controls;
using NewLaserProject.Views.Converters;
using NewLaserProject.Views.Selectors;

namespace NewLaserProject.Views.Misc
{
    internal class PicEnumPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var items = Enum.GetValues(propertyItem.PropertyType).Cast<object>();
            var element = new ItemsButton
            {
                Items = items,
                ItemSelector = new HatchLoopDirectionDataSelector(),
                Height = 50,
                Width = 65,
            };
            return element;
        }

        public override DependencyProperty GetDependencyProperty() => ItemsButton.SelectedItemProperty;
    }
    /// <summary>
    /// Using converter from millimeters to micrometers and back
    /// </summary>
    internal class NumberPropertyEditor2 : PropertyEditorBase
    {
        public NumberPropertyEditor2()
        {
            Minimum = 0;
            Maximum = 1000;
        }

        public NumberPropertyEditor2(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public double Minimum
        {
            get; set;
        }

        public double Maximum
        {
            get; set;
        }

        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var numericUpDown = new NumericUpDown
            {
                IsReadOnly = propertyItem.IsReadOnly,
                Minimum = Minimum,
                Maximum = Maximum
            };

            return numericUpDown;
        }
        public override DependencyProperty GetDependencyProperty() => NumericUpDown.ValueProperty;
        protected override IValueConverter GetConverter(PropertyItem propertyItem) => new MmToUmConverter();
    }
}
