using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures
{
    public class ChildrenObservableCollection<T> : ObservableCollection<T>
    {
        private readonly T _parent;

        [JsonIgnore]
        public T MyParent
        {
            get => _parent;
        }
        public ChildrenObservableCollection(T parent)
        {
            _parent = parent;
        }
        public ChildrenObservableCollection(T parent, IEnumerable<T> collection) : base(collection)
        {
            _parent = parent;
        }

    }
}
