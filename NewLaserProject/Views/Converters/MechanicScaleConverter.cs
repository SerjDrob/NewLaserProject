using System;
using System.Globalization;
using System.Windows.Data;

namespace NewLaserProject.Views.Converters
{
    internal class MechanicScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var tw = (double)values[0];
                var pos = (double)values[1];
                var mechW = (double)values[2];
                var sign = 1;
                if(parameter is string str)
                {
                    if (int.TryParse(str, out var result)) sign = result;
                }
                var res =  sign * tw * pos / mechW;
                return res;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
