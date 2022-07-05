using MachineClassLibrary.Laser.Entities;
using NewLaserProject.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NewLaserProject.Views.Converters
{
    internal class GetDictValueConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length==2)
            {
                if (values[1] is LaserEntity key && values[0] is IDictionary<LaserEntity,IEnumerable<Material>> dict)
                {
                    if(dict.TryGetValue(key, out var result))
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
