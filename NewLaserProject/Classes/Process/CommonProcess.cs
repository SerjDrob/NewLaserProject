using Humanizer;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Miscellanius;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.ViewModels;
using Stateless;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MachineClassLibrary.Laser.Markers;

namespace NewLaserProject.Classes.Process
{
    internal class CommonProcess : IProcess
    {
        private bool disposedValue;
        private StateMachine<State, Trigger> _stateMachine;
        private double _xActual;
        private double _yActual;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _baseCoorSystem;
        private readonly ICoorSystem<LMPlace> _teachingPointsCoorSystem;
        private ICoorSystem _teachingLinesCoorSystem;

        private readonly PureCoorSystem<LMPlace> _pureCoorSystem;
        private double _matrixAngle;
        private readonly double _zeroZPiercing;
        private readonly double _zeroZCamera;
        private double _waferThickness;
        private readonly double _dX;
        private readonly double _dY;
        private readonly double _pazAngle;
        private readonly double _waferAngle;
        private readonly Scale _scale;
        private readonly ISubject<IProcessNotify> _subject;
        private List<IProcObject> _excludedObjects;
        private bool _inProcess = false;
        private readonly bool _underCamera;
        private readonly double _zPiercing;
        private readonly FileAlignment _fileAlignment;
        private List<IDisposable> _subscriptions;
        private readonly Guid _procId = Guid.NewGuid();
        private readonly IEnumerable<(IEnumerable<IProcObject> procObjects, MicroProcess microProcess)> _processing;
        private readonly LaserWafer _procWafer;

        private CancellationTokenSource _currentMicroProcCts;
        private CancellationToken _mainCancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        public CommonProcess(IEnumerable<(IEnumerable<IProcObject> procObjects, MicroProcess microProcess)> processing,
               LaserWafer wafer, LaserMachine laserMachine,
               double zeroZPiercing, double zeroZCamera, double waferThickness,
               double dX, double dY, double pazAngle,
               ISubject<IProcessNotify> subject, ICoorSystem<LMPlace> baseCoorSystem,
               bool underCamera, FileAlignment aligningPoints, double waferAngle, Scale scale)
        {
            _procWafer = wafer;
            _laserMachine = laserMachine;
            _zeroZPiercing = zeroZPiercing;
            _zeroZCamera = zeroZCamera;
            _waferThickness = waferThickness;
            _dX = dX;
            _dY = dY;
            _pazAngle = pazAngle;
            _subject = subject;
            _pureCoorSystem = new PureCoorSystem<LMPlace>(baseCoorSystem.GetMainMatrixElements().GetMatrix3());
            _baseCoorSystem = baseCoorSystem;
            _teachingPointsCoorSystem = baseCoorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);
            _underCamera = underCamera;
            _zPiercing = _underCamera ? _zeroZCamera : _zeroZPiercing;//TODO use it outside of the constructor
            _fileAlignment = aligningPoints;
            _waferAngle = waferAngle;
            _scale = scale;
            _processing = processing;
        }

        enum State
        {
            Teaching,
            Started,
            GoRefPoint,
            GetRefPoint,
            InitialCorner,
            InitialPoints,
            Working,
            Paused,
            Denied,
            Exit,
            Loop,
            LinePreteaching,
            LineTeaching
        }
        enum Trigger
        {
            Next,
            Pause,
            Deny,
            Preteach
        }

        public void CreateProcess()
        {
            if (disposedValue) throw new ObjectDisposedException($"Object {_procId} has already disposed");
            var resultPoints = new List<PointF>();
            var originPoints = new List<PointF>();
            _subject.OnNext(new ProcWaferChanged(_processing.SelectMany(p => p.procObjects)));
            _stateMachine = new StateMachine<State, Trigger>(_fileAlignment switch
            {
                FileAlignment.AlignByCorner => State.InitialCorner,
                _ => State.InitialPoints
            }, FiringMode.Queued);

            var workingTrigger = _stateMachine.SetTriggerParameters<ICoorSystem>(Trigger.Next);
            var waferEnumerator = _procWafer.GetEnumerator();

            static int properPointsCount(FileAlignment aligning) => aligning switch
            {
                FileAlignment.AlignByThreePoint => 3,
                FileAlignment.AlignByTwoPoint => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(aligning))
            };

            async Task ProcessingTheWaferAsync(ICoorSystem coorSystem)
            {
                if (_mainCancellationToken.IsCancellationRequested) return;
                _subject.OnNext(new ProcessingStarted());
                foreach (var item in _processing)
                {
                    _currentMicroProcCts = item.microProcess.GetCancellationTokenSource();
                    for (var i = 0; i < item.microProcess.GetMainLoopCount(); i++)
                    {
                        var currentObjects = item.microProcess.IsLoopShuffle ? item.procObjects.Shuffle() : item.procObjects;
                        if (_mainCancellationToken.IsCancellationRequested) break;
                        foreach (var pObject in currentObjects)
                        {
                            var procObject = _procWafer.GetProcObjectToWafer(pObject);
                            if (_mainCancellationToken.IsCancellationRequested) break;
                            _subject.OnNext(new ProcObjectChanged(procObject));

                            if (!_excludedObjects?.Any(o => o.Id == procObject.Id) ?? true)
                            {
                                var position = coorSystem.ToGlobal(procObject.X, procObject.Y);
                                _laserMachine.SetVelocity(Velocity.Fast);
                                await Task.WhenAll(
                                     _laserMachine.MoveAxInPosAsync(Ax.Y, position[1], true),
                                     _laserMachine.MoveAxInPosAsync(Ax.X, position[0], true),
                                     Task.Run(async () => { if (!_underCamera) await _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness); })
                                     );
                                procObject.IsBeingProcessed = true;
                                _subject.OnNext(new ProcObjectChanged(procObject));
                                if (_inProcess && !_underCamera)
                                {
                                    try
                                    {
                                        await item.microProcess.InvokePierceFunctionForObjectAsync(procObject);
                                    }
                                    catch (MarkerException ex)
                                    {
                                        _subject.OnNext(new ProcessMessage("Потеряна связь с платой ШИМ. Процесс был завершен!", MsgType.Error));
                                        Console.Error.WriteLine(ex.Message);
                                        goto M1;
                                    }
                                }
                                else
                                {
                                    await Task.Delay(1000);
                                    var pointsLine = string.Empty;
                                    var line = $" X: {Math.Round(procObject.X, 3),8} | Y: {Math.Round(procObject.Y, 3),8} | X: {Math.Round(position[0], 3),8} | Y: {Math.Round(position[1], 3),8} | X: {Math.Round(_laserMachine.GetAxActual(Ax.X), 3),8} | Y: {Math.Round(_laserMachine.GetAxActual(Ax.Y), 3),8}";
                                    if (HandyControl.Controls.MessageBox.Ask($" X: {Math.Round(position[0], 3),8} | Y: {Math.Round(position[1], 3),8}") == System.Windows.MessageBoxResult.OK)
                                    {
                                        pointsLine = "GOOD" + line;
                                    }
                                    else
                                    {
                                        pointsLine = $"BAD " + line;
                                    }
                                }
                                procObject.IsProcessed = true;
                                _subject.OnNext(new ProcObjectChanged(procObject));
                            }
                        }
                    }
                }
 M1:            _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                var completeStatus = _mainCancellationToken.IsCancellationRequested ? CompletionStatus.Cancelled : CompletionStatus.Success;
                _subject.OnNext(new ProcessingStopped());
                _subject.OnNext(new ProcCompletionPreview(completeStatus, coorSystem));
            }
            _subject.OfType<SnapShotResult>()
                .Select(result => Observable.FromAsync(async () =>
               {
                   var point = _procWafer.GetPointToWafer(result);
                   originPoints.Add(point);
                   var x = (float)(_xActual + (_underCamera ? 0 : _dX));
                   var y = (float)(_yActual + (_underCamera ? 0 : _dY));
                   resultPoints.Add(new(x, y));
                   _subject.OnNext(new ProcessMessage("", MsgType.Clear));
                   if (resultPoints.Count == properPointsCount(_fileAlignment))
                   {
                       var coorSys = _fileAlignment switch
                       {
                           FileAlignment.AlignByThreePoint => new CoorSystem<LMPlace>
                          .ThreePointCoorSystemBuilder()
                          .SetFirstPointPair(originPoints[0], resultPoints[0])
                          .SetSecondPointPair(originPoints[1], resultPoints[1])
                          .SetThirdPointPair(originPoints[2], resultPoints[2])
                          .FormWorkMatrix(_scale, _scale)
                          .Build(),
                           FileAlignment.AlignByTwoPoint => _pureCoorSystem
                               .GetTwoPointSystemBuilder()
                               .SetFirstPointPair(originPoints[0], resultPoints[0])
                               .SetSecondPointPair(originPoints[1], resultPoints[1])
                               .FormWorkMatrix(1, 1)
                               .Build(),
                           FileAlignment.AlignByCorner => _baseCoorSystem.ExtractSubSystem(_underCamera ? LMPlace.FileOnWaferUnderCamera : LMPlace.FileOnWaferUnderLaser),
                           _ => throw new InvalidOperationException()
                       };
                       _matrixAngle = coorSys.GetMatrixAngle2();


                       foreach (var item in _processing.Select(p => p.microProcess))
                       {
                           item.SetEntityAngle(-_pazAngle + _matrixAngle);
                       }

                       await _stateMachine.FireAsync(workingTrigger, coorSys);
                   }
                   else
                   {
                       await _stateMachine.FireAsync(Trigger.Next);
                   }
               }))
                .Concat()
                .Subscribe()
                .AddSubscriptionTo(_subscriptions);

            _subject.OfType<ReadyForSnap>()
                .Subscribe(result =>
                {
                    var position = _teachingPointsCoorSystem.FromGlobal(_xActual, _yActual);
                    var point = _procWafer.GetPointFromWafer(new((float)position[0], (float)position[1]));
                    var request = new ScopedGeomsRequest(5 * _scale, 5 * _scale, point.X, point.Y);
                    _subject.OnNext(request);
                })
                .AddSubscriptionTo(_subscriptions);

            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;


            _inProcess = true;//TODO is it necessary?


            _stateMachine.Configure(State.InitialCorner)
                .OnActivateAsync(async () =>
                {
                    foreach (var item in _processing.Select(p => p.microProcess))
                    {
                        item.SetEntityAngle(_waferAngle - _pazAngle);
                    }
                    await _stateMachine.FireAsync(workingTrigger, _baseCoorSystem.ExtractSubSystem(_underCamera ? LMPlace.FileOnWaferUnderCamera : LMPlace.FileOnWaferUnderLaser));
                })
                .Permit(Trigger.Next, State.Working);

            _stateMachine.Configure(State.InitialPoints)
                .OnActivateAsync(() => _stateMachine.FireAsync(Trigger.Next))
                .Permit(Trigger.Next, State.GoRefPoint);

            _stateMachine.Configure(State.Teaching)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GoRefPoint)
                .SubstateOf(State.Teaching)
                .OnEntry(() =>
                {
                    _subject.OnNext(new PermitSnap(true));
                    _subject.OnNext(new ProcessMessage($"Сопоставьте {(originPoints.Count + 1).ToOrdinalWords(GrammaticalGender.Feminine).ApplyCase(GrammaticalCase.Accusative)} точку ", MsgType.Info));
                })
                .PermitReentryIf(Trigger.Next, () => resultPoints.Count < properPointsCount(_fileAlignment))
                .PermitIf(Trigger.Next, State.Working, () => resultPoints.Count == properPointsCount(_fileAlignment))
                .Permit(Trigger.Preteach, State.LinePreteaching);

            _stateMachine.Configure(State.Working)
                .OnEntryFromAsync(workingTrigger, ProcessingTheWaferAsync)
                .Permit(Trigger.Next, State.LineTeaching)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.LinePreteaching)
               .OnEntryAsync(() =>
               {
                   if (waferEnumerator.MoveNext())
                   {
                       _subject.OnNext(new ProcessMessage("Нажмите next", MsgType.Info));
                   }
                   return Task.CompletedTask;
               })
               .Permit(Trigger.Next, State.LineTeaching)
               .Ignore(Trigger.Pause);

            var xLineSb = new StringBuilder();
            var yLineSb = new StringBuilder();

            var xLCollection = new List<(string orig, string deriv)>();
            var yLCollection = Array.Empty<(string orig, string deriv)>();
            var cultureInfo = CultureInfo.InvariantCulture;
            double xl = 0;
            double yl = 0;

            _stateMachine.Configure(State.LineTeaching)
                .OnEntryAsync(async () =>
                {
                    var procObject = waferEnumerator.Current;
                    _subject.OnNext(new ProcObjectChanged(procObject));

                    if (!_excludedObjects?.Any(o => o.Id == procObject.Id) ?? true)
                    {
                        var position = _teachingLinesCoorSystem.ToGlobal(procObject.X, procObject.Y);
                        _laserMachine.SetVelocity(Velocity.Fast);
                        await Task.WhenAll(
                             _laserMachine.MoveAxInPosAsync(Ax.Y, position[1], true),
                             _laserMachine.MoveAxInPosAsync(Ax.X, position[0], true));
                        procObject.IsBeingProcessed = true;

                        _subject.OnNext(new ProcObjectChanged(procObject));

                        xLineSb.Append($"({_xActual.ToString(cultureInfo)},");
                        yLineSb.Append($"({_yActual.ToString(cultureInfo)},");
                        xl = _xActual;
                        yl = _yActual;

                        _subject.OnNext(new ProcessMessage("Скорректируйте и нажмите next", MsgType.Info));

                        procObject.IsProcessed = true;
                        _subject.OnNext(new ProcObjectChanged(procObject));
                    }
                })
                .PermitDynamic(Trigger.Next, () => waferEnumerator.MoveNext() ? State.LineTeaching : State.Exit)
                .OnExit(() =>
                {
                    _subject.OnNext(new ProcessMessage("", MsgType.Clear));
                    xLineSb.Append($"{_xActual.ToString(cultureInfo)}),");
                    yLineSb.Append($"{_yActual.ToString(cultureInfo)}),");
                    var p = (xl.ToString(cultureInfo), _xActual.ToString(cultureInfo));
                    xLCollection.Add(p);
                    yLCollection.Append((yl.ToString(cultureInfo), _yActual.ToString(cultureInfo)));

                });

            _stateMachine.Configure(State.Exit)
                 .OnEntryAsync(() =>
                 {
                     //Trace.WriteLine(xLineSb.ToString());
                     //Trace.WriteLine(yLineSb.ToString());
                     //var ser = JsonSerializer.CreateDefault();
                     //var writer = System.IO.TextWriter.Null;
                     //ser.Serialize(writer, xLCollection);
                     //writer.Flush();
                     //var xstr = writer.ToString();

                     //writer = System.IO.TextWriter.Null;
                     //ser.Serialize(writer, yLCollection);
                     //writer.Flush();
                     //var ystr = writer.ToString();

                     _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                     _subject.OnNext(new ProcCompletionPreview(CompletionStatus.Success, _teachingLinesCoorSystem));
                     return Task.CompletedTask;
                 });

            _stateMachine.Configure(State.Denied)
                .OnEntryAsync(() =>
                {
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    _subject.OnNext(new TeachingCancelledByUser());
                    return Task.CompletedTask;
                });
        }
        private void _laserMachine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            switch (e.Axis)
            {
                case Ax.X:
                    _xActual = e.Position;
                    break;
                case Ax.Y:
                    _yActual = e.Position;
                    break;
                default:
                    break;
            }
        }
        public async Task Deny()
        {
            _currentMicroProcCts?.Cancel();
            _cancellationTokenSource?.Cancel();
            await _laserMachine.CancelMarkingAsync();
        }
        public void ExcludeObject(IProcObject procObject) => _excludedObjects.Add(procObject);
        public void IncludeObject(IProcObject procObject) => _excludedObjects.Remove(procObject);
        public async Task Next()
        {
            await _stateMachine.FireAsync(Trigger.Next);
        }
        public async Task StartAsync()
        {
            _cancellationTokenSource = new();
            _mainCancellationToken = _cancellationTokenSource.Token;
            if (_mainCancellationToken.IsCancellationRequested) return;
            if (_stateMachine is not null) await _stateMachine.ActivateAsync().ConfigureAwait(false);
        }
        public IDisposable Subscribe(IObserver<IProcessNotify> observer)
        {
            _subscriptions ??= new();
            var subscription = _subject.Subscribe(observer);
            _subscriptions.Add(subscription);
            return subscription;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _subscriptions?.ForEach(s => s.Dispose());
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Никаких Dispose(true) и никаких вызовов GC.SuppressFinalize()
            DisposeManagedResources();
            disposedValue = true;
        }
        protected virtual void DisposeManagedResources()
        {
            _subscriptions?.ForEach(s => s.Dispose());
        }

        public void ChangeParams(ProcessParams processParams)
        {
            _waferThickness = processParams.WaferThickness;
        }
    }
}
