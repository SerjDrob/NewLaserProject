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
            foreach (var layerKV in _reader.GetLayers())
            {
                var geometries = ((IEnumerable<AdaptedGeometry>)_geomAdapter.GetGeometries()).Where(ag => ag.LayerName == layerKV.Key).Select(ag => ag.geometry);
                var layerColor = GeometryAdapter.GetColorFromArgb(layerKV.Value);
                yield return new LayerGeometryCollection(new GeometryCollection(geometries), layerKV.Key, true, layerColor, layerColor);
            }
        }
    }
}
