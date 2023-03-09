using NewLaserProject.Classes;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NewLaserProject.Views.Converters
{
    internal class FileAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var isChecked = (bool)value;
                return isChecked ? FileAlignment.AlignByThreePoint : FileAlignment.AlignByCorner;
            }
            catch
            {
                return FileAlignment.AlignByCorner;
            }
        }
    }
}
