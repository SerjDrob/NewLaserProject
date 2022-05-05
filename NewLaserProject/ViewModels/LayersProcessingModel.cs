using MachineClassLibrary.Classes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class LayersProcessingModel
    {
        //private readonly string _fileName;
        private readonly IDxfReader _dxfReader;
        public ObservableCollection<Layer> Layers { get; private set; }

        public LayersProcessingModel(IDxfReader dxfReader)
        {
            _dxfReader = dxfReader;
            var structure = _dxfReader.GetLayersStructure();

            var layers = structure
                .Where(obj=>obj.Value.Any())
                .Select(obj => new Layer(obj.Key, obj.Value));
            Layers = new(layers);
        }
        [ICommand]
        private void CheckObject(Text text)
        {
            foreach (var item in Layers.SelectMany(l => l.Objects).Where(o => o.Value != text.Value))
            {
                item.IsProcessed = false;
            }
        }
        [ICommand]
        private void ChooseObject(object param)
        {
            var p = param as Text;
            ObjectChosenEvent(this, new string[] { p.Value, p.Count.ToString() });
        }
        public event EventHandler<string[]> ObjectChosenEvent;
    }
}
