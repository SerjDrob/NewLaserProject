using MachineClassLibrary.Classes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class LayersProcessingModel
    {
        //private readonly string _fileName;
        private readonly IDxfReader _dxfReader;
        public ObservableCollection<Layer> Layers { get; set; } = new();

        public LayersProcessingModel(IDxfReader dxfReader)
        {

            //_fileName = fileName;
            _dxfReader = dxfReader;
            var layers = dxfReader.GetLayers();// document.Layers;
            foreach (var layer in layers.Keys)
            {
                var lay = new Layer(layer);
                //var objects = layers.GetReferences(layer.Name);
                if (lay.AddObjects(objects))
                {
                    Layers.Add(lay);
                }
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
