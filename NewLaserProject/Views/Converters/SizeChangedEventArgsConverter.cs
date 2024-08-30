using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NewLaserProject.Views.Converters
{
    public class SizeChangedEventArgsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var width = (double)values[0];
                var targetWidth = (double)values[1];
                var scale = (double)values[2];
                var result = Math.Round(1000 * scale * targetWidth / width);
                if (values?.Length == 4 && values[3] is ScaleTransform transform)
                {
                    //result /= Math.Abs(transform.ScaleX);
                    //result *= Math.Abs(transform.ScaleX);
                }
                return result.ToString();
            }
            catch (Exception)
            {
                return "1";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class TargetHeightConverter : IMultiValueConverter
    {
        private double _tempHeight;
        private double _tempScale;
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var height = (double)values[0];
                var targetHeight = (double)values[1];
                var scale = (double)values[2];
                var result = Math.Round(height * targetHeight / (scale * 1000));
                _tempHeight = height;
                _tempScale = scale;
                return result;
            }
            catch (Exception)
            {
                return 25d;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var targetHeight = (double)value;
            var result = targetHeight * _tempScale * 1000 / _tempHeight;
            return [_tempHeight, result, _tempScale];
        }
    }
}
