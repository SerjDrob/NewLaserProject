using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using HandyControl.Controls;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using MachineClassLibrary.Classes;
using netDxf;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace NewLaserProject.Views
{

    public struct MyLine
    {
        public float X1;
        public float Y1;
        public float X2;
        public float Y2;

        public MyLine(float x1, float y1, float x2, float y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
    }


    /// <summary>
    /// Interaction logic for MainView2.xaml
    /// </summary>
    public partial class MainView : GlowWindow
    {
        private DxfFile _dxfDocumentIx; // Загруженный DXF файл
        private DxfDocument _dxfDocument; // Загруженный DXF файл
        private IMDxfReader _dxfReader;
        private float _zoom = 0.01f; // Масштаб
        private SKPoint _panOffset = new SKPoint(0, 0); // Смещение для панорамирования
        private SKPoint _lastMousePosition; // Последняя позиция мыши
        private bool _drawn = false;
        private List<(MyLine, SKColor)> _lines = new();
        private DateTime lastRedrawTime = DateTime.MinValue;
        private const int RedrawIntervalMs = 16; // ~60 FPS

        public MainView()
        {
            InitializeComponent();

            LoadDxfFile(@"D:\PL126A.dxf"); // Укажите путь к вашему DXF файлу
        }
        //-----
        private void LoadDxfFile(string filePath)
        {
            // Загрузка DXF файла
            //_dxfDocument = DxfDocument.Load(filePath);
        }

        private void OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            var surface = e.Surface;
            var canvas = surface.Canvas;
            var info = e.Info;

            // Очищаем экран
            canvas.Clear(SKColors.White);

            // Применяем масштабирование и панорамирование
            canvas.Translate(_panOffset.X, _panOffset.Y);
            canvas.Scale(_zoom);

            var getRGB = (int rgb) =>
            {
                byte red = (byte)((rgb >> 16) & 0xFF);    // Красный канал
                byte green = (byte)((rgb >> 8) & 0xFF);   // Зеленый канал
                byte blue = (byte)(rgb & 0xFF);     // Синий канал
                return (red, green, blue);
            };

            if (_dxfDocumentIx != null && !_lines.Any())
            {
                var polylines = _dxfDocumentIx.Entities.OfType<DxfLwPolyline>();
                foreach (var polyline in polylines)
                {
                    // polyline.Color.SetByLayer();
                    var color = polyline.Color.ToRGB();
                    var rgb = getRGB(color);
                    var skColor = new SKColor(rgb.red, rgb.green, rgb.blue);

                    var firstVertice = polyline.Vertices.First();
                    var tempLines = polyline.Vertices.Skip(1)
                        .Aggregate(new List<(MyLine, SKColor)>(), (prev, cur) =>
                        {
                            var l = new MyLine((float)firstVertice.X, (float)firstVertice.Y, (float)cur.X, (float)cur.Y);
                            prev.Add((l, skColor));
                            firstVertice = cur;
                            return prev;
                        }, acc =>
                        {
                            if (polyline.IsClosed)
                            {
                                var l = new MyLine
                                (
                                    acc.Last().Item1.X2,
                                    acc.Last().Item1.Y2,
                                    acc.First().Item1.X1,
                                    acc.First().Item1.Y1
                                );
                                acc.Add((l, skColor));
                            }
                            return acc;
                        });
                    _lines.AddRange(tempLines);
                }
            }
            _lines.ForEach(line => DrawLine(canvas, line.Item1, line.Item2));


        }
        private SKBitmap GetBitmapFromCanvas(SKCanvas canvas, SKSurface surface)
        {
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));

            // Создаем снимок текущего состояния поверхности
            using (var image = surface.Snapshot())
            {
                // Преобразуем SKImage в SKBitmap
                return SKBitmap.FromImage(image);
            }
        }
        private void DrawLine(SKCanvas canvas, MyLine line, SKColor color)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = color;
                paint.StrokeWidth = 80;
                paint.IsAntialias = true;

                var startPoint = new SKPoint(line.X1, line.Y1);
                var endPoint = new SKPoint(line.X2, line.Y2);
                canvas.DrawLine(startPoint, endPoint, paint);

            }
        }

        private void DrawCircle(SKCanvas canvas, netDxf.Entities.Circle circle)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.Blue;
                paint.StrokeWidth = 2;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Stroke;

                var center = new SKPoint((float)circle.Center.X, (float)circle.Center.Y);
                var radius = (float)circle.Radius;
                canvas.DrawCircle(center, radius, paint);
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Изменение масштаба
            const float zoomFactor = 1.1f;
            _zoom *= e.Delta > 0 ? zoomFactor : 1 / zoomFactor;
            if ((DateTime.Now - lastRedrawTime).TotalMilliseconds >= RedrawIntervalMs)
            {
                if (sender is SKGLElement element)
                {
                    element.InvalidateVisual();
                }
                lastRedrawTime = DateTime.Now;
            }
            // InvalidateVisual(); // Перерисовываем сцену
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Начало перетаскивания
            _lastMousePosition = GetMousePosition(e);
            this.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Перемещение
                var currentPosition = GetMousePosition(e);
                _panOffset.X += currentPosition.X - _lastMousePosition.X;
                _panOffset.Y += currentPosition.Y - _lastMousePosition.Y;
                _lastMousePosition = currentPosition;

                InvalidateVisual(); // Перерисовываем сцену
            }
            else
            {
                this.ReleaseMouseCapture();
            }
        }

        private SKPoint GetMousePosition(MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            return new SKPoint((float)position.X, (float)position.Y);
        }
        //-----

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Trace.TraceInformation("The application closed");
            Trace.Flush();
            Environment.Exit(0);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = e.Source as DataGrid;
            grid?.UnselectAll();
            if (grid?.SelectedItem is not null)
            {
                grid.UpdateLayout();
                grid.ScrollIntoView(grid.SelectedItem);
            }
        }

        private void SKElement_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            // Получаем поверхность и размеры
            var surface = e.Surface;
            var canvas = surface.Canvas;
            var info = e.Info;

            // Очищаем экран
            canvas.Clear(SKColors.CornflowerBlue);

            // Рисуем линию
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.Red;
                paint.StrokeWidth = 5;
                paint.IsAntialias = true;

                canvas.DrawLine(50, 50, 200, 200, paint);
            }
        }
    }
}
