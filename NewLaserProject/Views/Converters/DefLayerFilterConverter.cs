using NewLaserProject.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace NewLaserProject.Views.Converters
{
    internal class DefTechnologiesConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           // if (value is ListCollectionView defTechsView)
            //{
                if (value is IEnumerable<DefaultLayerEntityTechnology> defLET)
                {
                    return defLET.DistinctBy(d => d.DefaultLayerFilter.Filter);
                }                
            //}
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
