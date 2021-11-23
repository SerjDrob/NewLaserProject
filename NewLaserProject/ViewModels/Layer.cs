using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLaserProject.ViewModels
{
    public class Layer
    {
        public string Name { get; set; }

        public Layer(string name)
        {
            Name = name;
        }
        public bool AddObjects(IEnumerable<netDxf.DxfObject> dxfObjects)
        {
            var res = new Dictionary<string, int>();
            Objects = new(
            dxfObjects.Where(o =>
            {
                var spec = new Specification(o.GetType());
                return spec.IsSatisfiedBy;
            }).Select(obj => obj.ToString())
            .Aggregate(res, (dict, str) => { if(!dict.TryAdd(str, 1)) dict[str]++; return dict; } )
            .Select(obj => new Text { Value=obj.Key, Count = obj.Value})
            );
            return Objects.Count > 0;
        }
        public List<Text> Objects { get; set; }

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
