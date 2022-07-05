using MachineClassLibrary.Laser.Entities;
using NewLaserProject.Data.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace NewLaserProject.ViewModels
{
    public class DefaultTechSelector
    {
        public DefaultLayerFilter DefLayerFilter { get; set; }
        public ObservableCollection<LaserEntity> Entities { get; set; }
        public IDictionary<LaserEntity,IEnumerable<Material>> EntMaterials { get; set; }
        public DefaultTechSelector(DefaultLayerFilter layerFilter, IDictionary<LaserEntity, IEnumerable<Material>> entMaterials)
        {
            DefLayerFilter = layerFilter;
            Entities = new ObservableCollection<LaserEntity>(entMaterials.Keys);
            EntMaterials = entMaterials;
        }
    }
}
