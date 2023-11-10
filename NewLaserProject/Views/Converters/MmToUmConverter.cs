using System;
using System.Globalization;
using System.Windows.Data;

namespace NewLaserProject.Views.Converters
{
    internal class MmToUmConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var mm = (double)value;
                var unitLength = UnitsNet.Length.FromMillimeters(mm);
                var um = (int)unitLength.Micrometers;
                return um;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var um =  System.Convert.ToInt32(value);
                var unitLength = UnitsNet.Length.FromMicrometers(um);
                return (double)unitLength.Millimeters;
            }
            catch (Exception)
            {
                return 0d;
            }
        }
    }
}
