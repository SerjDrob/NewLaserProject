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
        public ObservableCollection<Layer> Layers { get; init; }

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
        private void ChooseObject(object param)
        {
            var p = param as Text;
            ObjectChosenEvent(this, new string[] { p.Value, p.Count.ToString() });
        }
        public event EventHandler<string[]> ObjectChosenEvent;
    }
}
