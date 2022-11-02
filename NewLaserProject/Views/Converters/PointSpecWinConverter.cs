using MachineControlsLibrary.Classes;
using NewLaserProject.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NewLaserProject.Views.Converters
{
    class PointSpecWinConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var points = values?[0] as IEnumerable<Point>;
            var geometries = points?.Select(p => new EllipseGeometry(p, 50, 50))
                .ToArray();
            if (geometries is not null)
            {
                var geomcollection = geometries
                    .Select(g => new GeometryCollection(new[] {g}))
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
