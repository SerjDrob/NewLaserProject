using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NewLaserProject.Views.Converters
{
    internal class IProcToGeometryConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var obj = value as IProcObject;
            return obj?.ToGeometry();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
