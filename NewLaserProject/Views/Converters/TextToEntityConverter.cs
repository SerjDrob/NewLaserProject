using System;
using System.Globalization;
using System.Windows.Data;
using MachineClassLibrary.Laser.Entities;

namespace NewLaserProject.Views.Converters
{
    internal class TextToEntityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var entityName = value as string;
            return LaserEntDxfTypeAdapter.GetLaserEntity(entityName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
