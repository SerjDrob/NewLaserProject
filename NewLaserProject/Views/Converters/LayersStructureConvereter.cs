using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Miscellaneous;

namespace NewLaserProject.Views.Converters
{
    internal class LayersStructureConvereter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var structure = value as IDictionary<string, IEnumerable<(string objType, int count)>>;

            var result = structure?
                .Select(l => new LayerStructure { LayerName = l.Key, Entities = l.Value.Select(o => (object)LaserEntDxfTypeAdapter.GetLaserEntity(o.objType)) })
                        .ToObservableCollection();
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
