using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MachineClassLibrary.Miscellaneous;
using MachineControlsLibrary.Classes;

namespace NewLaserProject.Views.Converters
{
    class PointSpecWinConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var scale = 1d;
            if (values.Count() == 2)
            {
                scale = (double)values[1];
            }
            var points = values?[0] as IEnumerable<Point>;
            var geometries = points?.Select(p => new EllipseGeometry(p, 0.05 * scale, 0.05 * scale))
                .ToArray();
            if (geometries is not null)
            {
                var geomcollection = geometries
                    .Select(g => new GeometryCollection(new[] { g }))
                    .Select(gc => new LayerGeometryCollection(gc, "Points", true, Brushes.Yellow, Brushes.Yellow))
                    .ToArray()
                    .ToObservableCollection();

                return geomcollection;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
