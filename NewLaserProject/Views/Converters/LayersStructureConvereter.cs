using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using MachineClassLibrary.Laser.Entities;
using MachineControlsLibrary.Converters;
using NewLaserProject.Classes;
using NewLaserProject.ViewModels;

namespace NewLaserProject.Views.Converters
{
    internal class LayersStructureConvereter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var structure = value as IDictionary<string, IEnumerable<(string objType, int count)>>;

            var result = structure?.Select(l => new { LayerName = l.Key, Entities = l.Value.Select(o => (object)LaserEntDxfTypeAdapter.GetLaserEntity(o.objType)) })
                        .ToObservableCollection();
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
