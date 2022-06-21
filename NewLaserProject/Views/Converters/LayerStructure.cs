using System.Collections.Generic;

namespace NewLaserProject.Views.Converters
{
    internal class LayerStructure
    {
        public string LayerName { get; set; }
        public IEnumerable<object> Entities { get; set; }
    }
}
