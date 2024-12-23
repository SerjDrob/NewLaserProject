﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Miscellaneous;
using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Controls;
using MachineControlsLibrary.Controls.GraphWin;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using NewLaserProject.Classes.Process.ProcessFeatures;
using PropertyChanged;
using Point = System.Windows.Point;

namespace NewLaserProject.ViewModels
{

    [INotifyPropertyChanged]
    internal partial class FileVM : IDisposable
    {
        private IDxfReader _dxfReader;
        private DxfEditor? _dxfEditor;
        private readonly ISubject<IProcessNotify> _mediator;
        public bool CanCut { get; set; } = false;
        public bool IsFileLoading { get; set; } = false;
        public event EventHandler<bool> CanUndoChanged;
        public event EventHandler<Point> OnFileClicked;
        private LayGeomsEditor _geomsEditor;
        private IDisposable _requestSubscription;
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
            double waferOffsetY, double fileOffsetX, double fileOffsetY, string filePath, IDictionary<string, bool> ignoredLayers)
        {
            _dxfReader = dxfReader;
            _dxfEditor = dxfReader as DxfEditor;
            if (_dxfEditor is not null) _dxfEditor.CanUndoChanged += _dxfEditor_CanUndoChanged;
            IgnoredLayers = new(ignoredLayers);
            FileScale = fileScale;
            MirrorX = mirrorX;
            


            WaferTurn90 = waferTurn90;
            _filePath = filePath;
            FileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            MarkText = System.IO.Path.GetFileNameWithoutExtension(filePath);
            OpenFile();
            _isFileOpened = true;

            WaferOffsetX = waferOffsetX;
            WaferOffsetY = waferOffsetY;
            FileOffsetX = fileOffsetX;
            FileOffsetY = fileOffsetY;

            _requestSubscription?.Dispose();
            _requestSubscription = _mediator.OfType<ScopedGeomsRequest>()
                .Select(request => Observable.FromAsync(async () =>
                {
                    var snapshot = await HandleAsync(request);
                    _mediator.OnNext(snapshot);
                }))
                .Concat()
                .Subscribe();
        }
        public string MarkText { get; set; }
        public void SetMarkText(string text) => MarkText = text;
        private void _dxfEditor_CanUndoChanged(object? sender, bool e) => CanUndoChanged?.Invoke(this, e);
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
            LayGeoms = new LayGeomAdapter(new IMGeometryAdapter(_filePath)).LayerGeometryCollections;
            _geomsEditor = new(LayGeoms);
            //MirrorX = mirrorX;
            //WaferTurn90 = turn90;
            WaferOffsetX = 0;
            WaferOffsetY = 0;
        }

        public ObservableCollection<LayerGeometryCollection> Route
        {
            get;
            set;
        } = new();


        public void AddRoute(IEnumerable<LayerGeometryCollection> layerGeometryCollections)
        {
            LayGeoms.Add(layerGeometryCollections.First());
        }

        [ICommand]
        private void GotSelection(RoutedSelectionEventArgs args)
        {
            var rect = (Rect)args;
            var layers = LayGeoms.Where(l => l.LayerEnable)
                .Select(l => l.LayerName)
                .ToArray();
            GotSelectionHandler(layers, rect);
        }


        public void GotSelectionHandler(string[] layers, Rect selection)
        {
            _geomsEditor?.RemoveBySelection(selection);
            _dxfEditor?.RemoveBySelection(layers, selection);
        }
        public void GotSelectionMultipleHandler(string[] layers, Rect selection)
        {
            foreach (var layer in layers)
            {
                _geomsEditor?.RemoveBySelection(selection,layer); 
            }
            _dxfEditor?.RemoveBySelection(layers, selection);
        }

        public Dictionary<string,bool> GetIgnoredLayers()
         => LayGeoms.ToDictionary(l=>l.LayerName,l=>l.LayerEnable);

        [ICommand]
        private void GotPointClicked(RoutedPointClickedEventArgs args)
        {
            if (_isFileOpened)
            {
                Point point = args;
                var x = point.X;
                var y = point.Y;
                var serviceWafer = new LaserWafer((FileSizeX, FileSizeY));

                serviceWafer.Scale(1 / FileScale);
                /*
                if (WaferTurn90) serviceWafer.Turn90();
                if (MirrorX) serviceWafer.MirrorX();
                serviceWafer.OffsetX((float)FileOffsetX);
                serviceWafer.OffsetY((float)FileOffsetY);
                */
                var fromwafer = serviceWafer.GetPointToWafer(new((float)x, (float)y));
                OnFileClicked?.Invoke(this, new Point(fromwafer.X, fromwafer.Y));
            }
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

                var entities = _layerGeometryCollections
                    .Where(c => c.LayerEnable)
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
            
            public void RemoveBySelection(Rect selection, string layer)
            {
                _erasedGeometries ??= new();

                var entities = _layerGeometryCollections
                    .Where(c => c.LayerName == layer)
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
        private bool disposedValue;
        private bool _isFileOpened;

        public string FileName { get; set; }
        public double FileSizeX { get; set; }
        public double FileSizeY { get; set; }
        public double FieldSizeX => FileScale * WaferWidth;
        public double FieldSizeY => FileScale * WaferHeight;
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
        public bool IsMarkTextVisible
        {
            get;
            set;
        }

        public event EventHandler TransformationChanged;

        public void SetTextPosition(Enum position)
        {
            TextPosition = (TextPosition)position;
        }
        public ObservableCollection<LayerGeometryCollection> LayGeoms { get; set; } = new();
        public ObservableCollection<LayerGeometryCollection> ScopedLayGeoms { get; set; } = new();
        public Dictionary<string, bool> IgnoredLayers { get; set; }
        public bool IsCircleButtonVisible
        {
            get;
            set;
        } = true;
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
                Angle = WaferTurn90 ? 90d : 0d,
                Scale = FileScale
            };

            return Task.FromResult(snapshot);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _requestSubscription.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FileVM()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
