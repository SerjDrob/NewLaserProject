using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NewLaserProject.Classes
{
    public class ChildrenObservCollection<T> : ObservableCollection<T>
    {
        private readonly T _parent;

        [JsonIgnore]
        public T MyParent { get => _parent; }
        public ChildrenObservCollection(T parent)
        {
            _parent = parent;
        }
        public ChildrenObservCollection(T parent, IEnumerable<T> collection):base(collection)
        {
            _parent = parent;
        }
       
    }
}
