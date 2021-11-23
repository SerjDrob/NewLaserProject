using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class LayersProcessingModel
    {
        private readonly string _fileName;
        public ObservableCollection<Layer> Layers { get; set; } = new();

        public LayersProcessingModel(string fileName)
        {

            _fileName = fileName;

            var reader = new DxfReader(_fileName);
            var document = reader.Document;
            var layers = document.Layers;
            foreach (var layer in layers)
            {
                var lay = new Layer(layer.Name);
                var objects = layers.GetReferences(layer.Name);
                if (lay.AddObjects(objects))
                {
                    Layers.Add(lay);
                }
            }

        }
        [ICommand]
        private void EditTechnology(Layer layer)
        {
            new TechnologyWizardView()
            {
                DataContext = new TechWizardViewModel()
            }.Show();
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
