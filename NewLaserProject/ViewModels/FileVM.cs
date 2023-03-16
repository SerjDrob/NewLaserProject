using MachineClassLibrary.Classes;
using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Controls;
using MachineControlsLibrary.Controls.GraphWin;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Properties;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace NewLaserProject.ViewModels
{

    [INotifyPropertyChanged]
    internal partial class FileVM
    {
        private IDxfReader _dxfReader;
        private DxfEditor? _dxfEditor;
        private readonly ISubject<IProcessNotify> _mediator;
        public bool CanCut { get; set; } = false;
        public bool IsFileLoading { get; set; } = false;

        private LayGeomsEditor _geomsEditor;
        public FileVM(double waferWidth, double waferHeight, ISubject<IProcessNotify> mediator)
        {
            WaferWidth = waferWidth;
            WaferHeight = waferHeight;
            _mediator = mediator;
        }
        public void ResetFileView()
        {
            LayGeoms = new();
        }
        public void SetFileView(IDxfReader dxfReader, float fileScale, bool mirrorX, bool waferTurn90, double waferOffsetX,
            double waferOffsetY, string filePath, IDictionary<string, bool> ignoredLayers)
        {
            _dxfReader = dxfReader;
            _dxfEditor = dxfReader as DxfEditor;
            IgnoredLayers = new(ignoredLayers);
            FileScale = fileScale;
            MirrorX = mirrorX;
            WaferOffsetX = waferOffsetX;
            WaferOffsetY = waferOffsetY;
            WaferTurn90 = waferTurn90;
            _filePath = filePath;
            FileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            OpenFile();

            _mediator.OfType<ScopedGeomsRequest>()
                .Subscribe(request => HandleAsync(request).ContinueWith(result =>
                {
                    _mediator.OnNext(result.Result);
                }));
        }

        public void SetWaferDimensions(double width, double height)
        {
            WaferWidth = width;
            WaferHeight = height;
        }
        public void SetViewFinders(double CameraX, double CameraY, double LaserX, double LaserY)
        {
            CameraViewfinderX = CameraX;
            CameraViewfinderY = CameraY;
            LaserViewfinderX = LaserX;
            LaserViewfinderY = LaserY;
        }
        private void OpenFile()
        {
            var fileSize = _dxfReader.GetSize();
            FileSizeX = Math.Round(fileSize.width);
            FileSizeY = Math.Round(fileSize.height);
            //LayGeoms = new LayGeomAdapter(_dxfReader).LayerGeometryCollections;
            LayGeoms = new LayGeomAdapter(new IMGeometryAdapter(_filePath)).LayerGeometryCollections;
            _geomsEditor = new(LayGeoms);
            MirrorX = Settings.Default.WaferMirrorX;
            WaferTurn90 = Settings.Default.WaferAngle90;
            WaferOffsetX = 0;
            WaferOffsetY = 0;

            //GetScopedGeometries();
        }

        [ICommand]
        private void GotSelection(RoutedSelectionEventArgs args)
        {
            var rect = (Rect)args;
            _geomsEditor?.RemoveBySelection(rect);

            var layers = LayGeoms.Where(l => l.LayerEnable)
                .Select(l => l.LayerName)
                .ToArray();

            _dxfEditor?.RemoveBySelection(layers, rect);
        }

        public void UndoRemoveSelection()
        {
            _geomsEditor?.Undo();
            _dxfEditor?.Undo();
        }

        class LayGeomsEditor
        {
            private readonly IEnumerable<LayerGeometryCollection> _layerGeometryCollections;
            public LayGeomsEditor(in IEnumerable<LayerGeometryCollection> layerGeometryCollections)
            {
                _layerGeometryCollections = layerGeometryCollections;
            }

            private Stack<(string layer, Geometry geometry)[]> _erasedGeometries;

            public void RemoveBySelection(Rect selection)
            {
                _erasedGeometries ??= new();

                var entities = _layerGeometryCollections.Where(c => c.LayerEnable)
                    .SelectMany(e => e.Geometries.Where(item => selection.Contains(item.Bounds)), (lgc, g) => new { lgc.LayerName, g })
                    .ToArray();

                foreach (var item in entities)
                {
                    var res = _layerGeometryCollections.Where(lg => lg.LayerName == item.LayerName)
                        .Single().Geometries
                        .Remove(item.g);
                }
                _erasedGeometries.Push(entities.Select(e => (e.LayerName, e.g)).ToArray());
            }
            public void Undo()
            {
                if (_erasedGeometries is null) return;
                if (_erasedGeometries.TryPop(out var values)) InsertElements(values);
            }
            private void InsertElements((string layer, Geometry geometry)[] elements)
            {
                foreach (var item in elements)
                {
                    _layerGeometryCollections.Where(lg => lg.LayerName == item.layer)
                        .Single()
                        .Geometries.Add(item.geometry);
                }
            }
            public void Reset()
            {
                if (_erasedGeometries is null) return;
                while (_erasedGeometries.TryPop(out var values)) InsertElements(values);
            }


            public IEnumerable<Point> GetScopedLayerPointsCollections(Rect scope)
            {
                var scopeWin = new RectangleGeometry(scope);
                var translation = new TranslateTransform(-scope.Location.X, -scope.Location.Y);

                var result = _layerGeometryCollections
                    .Where(lgc => lgc.LayerEnable)
                    .SelectMany(lgc => lgc.Geometries
                    .Where(g => scope.IntersectsWith(g.Bounds)))
                    .Select(g =>
                    {
                        if (g is EllipseGeometry ellipse)
                        {
                            var center = new Point(ellipse.Center.X + translation.X, ellipse.Center.Y + translation.Y);
                            return (Geometry)(new EllipseGeometry(center, ellipse.RadiusX, ellipse.RadiusY));
                        }
                        return Geometry.Combine(scopeWin, g, GeometryCombineMode.Intersect, translation);
                    })
                    .SelectMany(GetGeometryPoints);

                return result;

                static IEnumerable<Point> GetGeometryPoints(Geometry geometry)
                {
                    if (geometry is PathGeometry path)
                    {
                        return path.GetFlattenedPathGeometry()
                         .Figures.SelectMany(f => f.Segments)
                         .Where(s => s is PolyLineSegment)
                         .Cast<PolyLineSegment>()
                         .SelectMany(ps => ps.Points);
                    }
                    if (geometry is EllipseGeometry ellipse)
                    {
                        return new Point[] { ellipse.Center };
                    }
                    return Enumerable.Empty<Point>();
                }
            }


            public IEnumerable<LayerGeometryCollection> GetScopedLayerGeometryCollections(Rect scope)
            {
                var scopeWin = new RectangleGeometry(scope);
                var translation = new TranslateTransform(-scope.Location.X, -scope.Location.Y);

                return _layerGeometryCollections.Select(lgc =>
                {
                    var geometries =
                    new GeometryCollection(lgc.Geometries
                    .Where(g => scope.IntersectsWith(g.Bounds))
                    .Select(g =>
                    {
                        var geometry = g.Clone();
                        var combGeometry =
                        Geometry.Combine(geometry, scopeWin, GeometryCombineMode.Intersect, translation);
                        return combGeometry;
                    }));
                    return lgc with { Geometries = geometries };
                });
            }
        }



        private string _filePath;
        public string FileName { get; set; }
        public double FileSizeX { get; set; }
        public double FileSizeY { get; set; }
        public double FieldSizeX { get => FileScale * WaferWidth; }
        public double FieldSizeY { get => FileScale * WaferHeight; }
        public double WaferWidth { get; set; } = 48;
        public double WaferHeight { get; set; } = 60;
        public float FileScale { get; set; } = 1000;
        [OnChangedMethod(nameof(TransChanged))]
        public bool MirrorX { get; set; } = true;
        [OnChangedMethod(nameof(TransChanged))]
        public bool WaferTurn90 { get; set; } = true;
        [OnChangedMethod(nameof(TransChanged))]
        public double WaferOffsetX { get; set; }
        [OnChangedMethod(nameof(TransChanged))]
        public double WaferOffsetY { get; set; }
        [OnChangedMethod(nameof(TransChanged))]
        public double FileOffsetX { get; private set; }
        [OnChangedMethod(nameof(TransChanged))]
        public double FileOffsetY { get; private set; }
        public double TeacherPointerX { get; set; }
        public double TeacherPointerY { get; set; }
        public bool TeacherPointerVisibility { get; set; } = false;
        public double CameraViewfinderX { get; set; }
        public double CameraViewfinderY { get; set; }
        public double LaserViewfinderX { get; set; }
        public double LaserViewfinderY { get; set; }
        public TextPosition TextPosition { get; set; } = TextPosition.W;

        public event EventHandler TransformationChanged;

        public void SetTextPosition(Enum position)
        {
            TextPosition = (TextPosition)position;
        }
        public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();
        public ObservableCollection<LayerGeometryCollection> ScopedLayGeoms { get; set; } = new();
        public Dictionary<string, bool> IgnoredLayers { get; set; }
        [ICommand]
        private void AlignWafer(object obj)
        {
            var param = (ValueTuple<object, object, object>)obj;
            var scaleX = (double)param.Item1;
            var scaleY = (double)param.Item2;
            var aligning = (Aligning)param.Item3;
            var size = _dxfReader.GetSize();
            var dx = WaferWidth * FileScale;
            var dy = WaferHeight * FileScale;


            WaferOffsetX = (aligning switch
            {
                Aligning.Right or Aligning.RTCorner or Aligning.RBCorner => -(WaferTurn90 ? (size.height - dx) : (size.width - dx)),
                Aligning.Left or Aligning.LTCorner or Aligning.LBCorner => WaferTurn90 ? (size.height - dx) : (size.width - dx),
                Aligning.Top or Aligning.Bottom or Aligning.Center => 0,
            }) * scaleX / 2;

            WaferOffsetY = (aligning switch
            {
                Aligning.Top or Aligning.RTCorner or Aligning.LTCorner => WaferTurn90 ? (size.width - dy) : (size.height - dy),
                Aligning.Bottom or Aligning.RBCorner or Aligning.LBCorner => -(WaferTurn90 ? (size.width - dy) : (size.height - dy)),
                Aligning.Right or Aligning.Left or Aligning.Center => 0
            }) * scaleY / 2;

            FileOffsetX = (aligning switch
            {
                Aligning.Right or Aligning.RTCorner or Aligning.RBCorner => -(WaferTurn90 ? (size.height - dx) : (size.width - dx)),
                Aligning.Left or Aligning.LTCorner or Aligning.LBCorner => 0,
                Aligning.Top or Aligning.Bottom or Aligning.Center => -(WaferTurn90 ? (size.height - dx) : (size.width - dx)) / 2,
            }) / FileScale;

            FileOffsetY = (aligning switch
            {
                Aligning.Top or Aligning.RTCorner or Aligning.LTCorner => -(WaferTurn90 ? (size.width - dy) : (size.height - dy)),
                Aligning.Bottom or Aligning.RBCorner or Aligning.LBCorner => 0,
                Aligning.Right or Aligning.Left or Aligning.Center => -(WaferTurn90 ? (size.width - dy) : (size.height - dy)) / 2
            }) / FileScale;
        }

        [ICommand]
        private void ChangeMirrorX() => MirrorX ^= true;

        [ICommand]
        private void ChangeTurn90() => WaferTurn90 ^= true;

        private void GetScopedGeometries()
        {
            var scope = new Rect(10000, 10000, 5000, 5000);
            ScopedLayGeoms = _geomsEditor.GetScopedLayerGeometryCollections(scope).ToObservableCollection();
            ScopedLayPoints = _geomsEditor.GetScopedLayerPointsCollections(scope)
                .ToObservableCollection();
        }

        public ObservableCollection<Point> ScopedLayPoints { get; set; } = new();
        private void TransChanged()
        {
            TransformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public Task<SnapShot> HandleAsync(ScopedGeomsRequest request)
        {
            var x = request.X - request.Width / 2;
            var y = request.Y - request.Height / 2;
            var scope = new Rect(x, y, request.Width, request.Height);
            var scopedLayGeoms = _geomsEditor.GetScopedLayerGeometryCollections(scope);
            var scopedLayPoints = _geomsEditor.GetScopedLayerPointsCollections(scope);

            var snapshot = new SnapShot(x, y, _mediator)
            {
                LayGeoms = scopedLayGeoms.ToObservableCollection(),
                GeomPoints = scopedLayPoints.ToObservableCollection(),
                FieldSizeX = request.Width,
                FieldSizeY = request.Height,
                SpecSizeX = request.Width,
                SpecSizeY = request.Height,
                MirrorX = this.MirrorX ? -1 : 1,
                Angle = WaferTurn90 ? 90d : 0d
            };

            return Task.FromResult(snapshot);
        }
    }
}
