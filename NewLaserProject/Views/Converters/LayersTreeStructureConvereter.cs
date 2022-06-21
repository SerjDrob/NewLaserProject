using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NewLaserProject.Classes;
using NewLaserProject.ViewModels;

namespace NewLaserProject.Views.Converters
{
    internal class LayersTreeStructureConvereter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var structure = value as IDictionary<string, IEnumerable<(string objType, int count)>>;

            var layers = structure?
                .Where(obj => obj.Value.Any())
                .Select(obj => new Layer(obj.Key, obj.Value))
                .ToObservableCollection();

            return layers;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
