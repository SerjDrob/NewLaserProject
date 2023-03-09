using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NewLaserProject.Classes;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Reactive.Subjects;
using MachineControlsLibrary.Controls;
using Point = System.Windows.Point;

namespace NewLaserProject.ViewModels
{
    [INotifyPropertyChanged]
    internal partial class SnapShot:IProcessNotify
    {
        private readonly double snapX;
        private readonly double snapY;
        private readonly ISubject<IProcessNotify> _subject;

        public SnapShot(double snapX, double snapY, ISubject<IProcessNotify> subject)
        {
            this.snapX = snapX;
            this.snapY = snapY;
            _subject = subject;
        }

        public ObservableCollection<LayerGeometryCollection>? LayGeoms { get; set; }
        public ObservableCollection<Point>? GeomPoints { get; set; }
        public double FieldSizeX { get; set; }
        public double FieldSizeY { get; set; }
        public double SpecSizeX { get; set; }
        public double SpecSizeY { get; set; }
        public double MirrorX { get; set; }
        public double Angle { get; set; }
        
        [ICommand]
        private void GotPoint(GeomClickEventArgs? args)
        {
            if (args is not null)
            {
                var tr = new TranslateTransform(snapX, snapY);
                var resultPoint = tr.Transform((Point)args);
                _subject.OnNext(new SnapShotResult(resultPoint));
            }
        }
    }
}
