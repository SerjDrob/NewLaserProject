using MachineClassLibrary.Classes;
using MachineControlsLibrary.Classes;
using netDxf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NewLaserProject.Classes
{
    public class LayGeomAdapter
    {
        private readonly DxfDocument dxfDocument;

        public LayGeomAdapter(DxfDocument dxfDocument)
        {
            this.dxfDocument = dxfDocument;
        }

        public ObservableCollection<LayerGeometryCollection> CalcGeometry()
        {
            var result = new ObservableCollection<LayerGeometryCollection>();
            foreach (var layer in dxfDocument.Layers)
            {

                var collection = new GeometryCollection();
                foreach (var polyline in dxfDocument.LwPolylines.Where(lw => lw.Layer.Name == layer.Name))
                {

                    var figures = polyline.Explode();
                    foreach (var newEntity in figures)
                    {
                        switch (newEntity)
                        {
                            case netDxf.Entities.Line newLine:
                                var lineGeometry = new LineGeometry(new System.Windows.Point(newLine.StartPoint.X, newLine.StartPoint.Y),
                                                                    new System.Windows.Point(newLine.EndPoint.X, newLine.EndPoint.Y));
                                collection.Add(lineGeometry);
                                break;
                            case netDxf.Entities.Arc newArc:

                            default:
                                break;
                        }
                    }

                }
                foreach (var circle in dxfDocument.Circles.Where(lw => lw.Layer.Name == layer.Name))
                {
                    var ellipseGeometry = new EllipseGeometry(new System.Windows.Point(circle.Center.X, circle.Center.Y), circle.Radius, circle.Radius);

                    collection.Add(ellipseGeometry);
                }
                foreach (var line in dxfDocument.Lines.Where(lw => lw.Layer.Name == layer.Name))
                {
                    var lineGeometry = new LineGeometry(new System.Windows.Point(line.StartPoint.X, line.StartPoint.Y),
                                                                    new System.Windows.Point(line.EndPoint.X, line.EndPoint.Y));
                    collection.Add(lineGeometry);
                }
                var layGeom = new LayerGeometryCollection(collection, layer.Name, true, new SolidColorBrush(Color.FromRgb(layer.Color.R, layer.Color.G, layer.Color.B)),
                        new SolidColorBrush(Color.FromRgb(layer.Color.R, layer.Color.G, layer.Color.B)));
                result.Add(layGeom);

            }
            return result;
        }
    }
}
