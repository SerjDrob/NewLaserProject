using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    public partial class LayersProcessingModel:IObservable<(string layerName, LaserEntity entType, bool isCheck)>
    {
        private readonly IDxfReader _dxfReader;
        private List<IDisposable> _subscriptions;
        private ISubject<(string , LaserEntity, bool)> _subject;
        public ObservableCollection<Layer> Layers { get; private set; }

        public LayersProcessingModel(IDxfReader dxfReader)
        {
            _dxfReader = dxfReader;
            var structure = _dxfReader.GetLayersStructure();

            Func<LaserEntity,bool> predicate = (LaserEntity entity) => entity switch
                {
                    LaserEntity.Circle => true,
                    LaserEntity.Curve => true,
                    _ => false
                };
            


            var layers = structure
                .Where(obj=>obj.Value.Any(v=> predicate(LaserEntDxfTypeAdapter.GetLaserEntity(v.objType))))
                .Select(obj => new Layer(obj.Key, obj.Value
                .Where(o => predicate(LaserEntDxfTypeAdapter.GetLaserEntity(o.objType)))));

            Layers = new(layers);
        }
        [ICommand]
        private void CheckObject(Text text)
        {
            var entType = LaserEntDxfTypeAdapter.GetLaserEntity(text.Value);
            _subject?.OnNext((text.LayerName, entType, text.IsProcessed));
        }

        public void UnCheckItem((string layerName, LaserEntity entType) item)
        {
            var dxfEntName = LaserEntDxfTypeAdapter.GetEntityName(item.entType);
            var text = Layers.SelectMany(l=>l.Objects).SingleOrDefault(t=>t.LayerName == item.layerName && t.Value == dxfEntName);
            if (text is not null) text.IsProcessed = false;
        }

        [ICommand]
        private void ChooseObject(object param)
        {
           
        }

        public IDisposable Subscribe(IObserver<(string, LaserEntity, bool)> observer)
        {
            _subject ??= new Subject<(string, LaserEntity, bool)>();
            _subscriptions ??= new();
            var subscription = _subject.Subscribe(observer);
            _subscriptions.Add(subscription);
            return subscription;
        }
        public void UnSubscribe() => _subscriptions.Clear();
        public event EventHandler<(string,LaserEntity)> ObjectChosenEvent;
    }
}
