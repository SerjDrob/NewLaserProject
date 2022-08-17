using System;
using System.Globalization;
using System.Windows.Data;
using MachineClassLibrary.Laser.Entities;

namespace NewLaserProject.Views.Converters
{
    internal class IProcTypeToEntityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var entityType = value as IProcObject;
            return entityType.GetLaserEntity();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
