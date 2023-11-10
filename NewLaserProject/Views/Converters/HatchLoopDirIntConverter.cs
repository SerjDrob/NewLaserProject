using NewLaserProject.ViewModels.DialogVM;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NewLaserProject.Views.Converters
{
    internal class HatchLoopDirIntConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var num = (HatchLoopDirection)value;
                return num;
            }
            catch (Exception)
            {
                return HatchLoopDirection.Hatch_OUT;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return (int)value;
            }
            catch (Exception)
            {
                return (int)HatchLoopDirection.Hatch_OUT;
            }
        }
    }
}
