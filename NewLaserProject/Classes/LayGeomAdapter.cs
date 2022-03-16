using MachineClassLibrary.Classes;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace NewLaserProject.Classes
{
    public class LayGeomAdapter
    {
        private readonly GeometryAdapter _geomAdapter;
        private readonly IDxfReader _reader;
        public LayGeomAdapter(IDxfReader dxfReader)
        {
            Guard.IsNotNull(dxfReader, nameof(dxfReader));
            _reader = dxfReader;
            _geomAdapter = new(_reader);
        }
        public ObservableCollection<LayerGeometryCollection> LayerGeometryCollections { get => new(CalcGeometry()); }
        public IEnumerable<LayerGeometryCollection> CalcGeometry()
        {
            return _geomAdapter.GetGeometries()
                .GroupBy(ag => ag.LayerName)
                .Select(x => new LayerGeometryCollection(new GeometryCollection(x.Select(y => y.geometry)), x.Key, true, x.First().LayerColor, x.First().GeometryColor));
                
        }
    }
}
