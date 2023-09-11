using System.Collections.Generic;
using System.Linq;

namespace NewLaserProject.ViewModels
{
    public class Layer
    {
        public string Name { get; init; }
        public List<Text> Objects { get; init; }
        public Layer(string name, IEnumerable<(string objType, int objCount)> objects)
        {
            Name = name;
            Objects = objects.Select(obj=>new Text { Value=obj.objType, Count=obj.objCount, LayerName = name }).ToList(); 
        }
    }
}
