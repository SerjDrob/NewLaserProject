using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        
    }
    public class Layer
    {
        public string Name { get; set; }

        public Layer(string name)
        {
            Name = name;
        }
        public bool AddObjects(IEnumerable<netDxf.DxfObject> dxfObjects)
        {
            Objects = new(
            dxfObjects.Where(o =>
            {
                var spec = new Specification(o.GetType());
                return spec.IsSatisfiedBy;
            }).Select(obj => obj.ToString()).Distinct()
            );
            return Objects.Count > 0;
        }
        public List<string> Objects { get; set; }

        class Specification
        {
            private readonly Type _type;

            public Specification(Type type)
            {
                _type = type;
            }

            public bool IsSatisfiedBy { get => types.Contains(_type); }
            private Type[] types = new Type[] { typeof(netDxf.Entities.Line), typeof(netDxf.Entities.LwPolyline), typeof(netDxf.Entities.Circle) };
        }
    }
}
