using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Miscellanius;
using NewLaserProject.ViewModels;
using Stateless;

namespace NewLaserProject.Classes.Process
{
    internal class GeneralLaserProcess2 : BaseLaserProcess, IProcess
    {
        private bool disposedValue;
        private IEnumerator<IProcObject> _waferEnumerator;
        private StateMachine<State, Trigger> _stateMachine;
        private double _xActual;
        private double _yActual;
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly LaserWafer _serviceWafer;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _baseCoorSystem;
        private readonly ICoorSystem<LMPlace> _teachingPointsCoorSystem;
        private readonly PureCoorSystem<LMPlace> _pureCoorSystem;
        private ICoorSystem? _workCoorSystem;
        private double _matrixAngle;
        private readonly double _zeroZPiercing;
        private readonly double _zeroZCamera;
        private readonly double _waferThickness;
        private readonly double _dX;
        private readonly double _dY;
        private readonly double _pazAngle;
        private readonly double _waferAngle;
        private readonly EntityPreparator _entityPreparator;
        private readonly ISubject<IProcessNotify> _subject;
        private List<IProcObject> _excludedObjects;
        private bool _inProcess = false;
        private readonly bool _underCamera;
        private readonly double _zPiercing;
        private readonly FileAlignment _fileAlignment;
        private StreamWriter _coorFile;
        private bool _inLoop = false;
        private int _loopCount = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private List<IDisposable> _subscriptions;
        private readonly ConcurrentQueue<Trigger> _triggersQueue;


        public GeneralLaserProcess2(IEnumerable<IProcObject> wafer, LaserWafer serviceWafer,
               string jsonPierce, LaserMachine laserMachine,
               double zeroZPiercing, double zeroZCamera, double waferThickness,
               double dX, double dY, double pazAngle, EntityPreparator entityPreparator,
               ISubject<IProcessNotify> subject, ICoorSystem<LMPlace> baseCoorSystem,
               bool underCamera, FileAlignment aligningPoints, double waferAngle) : base(jsonPierce)
        {
            _wafer = wafer;
            _serviceWafer = serviceWafer;
            _laserMachine = laserMachine;
            _zeroZPiercing = zeroZPiercing;
            _zeroZCamera = zeroZCamera;
            _waferThickness = waferThickness;
            _dX = dX;
            _dY = dY;
            _pazAngle = pazAngle;
            _entityPreparator = entityPreparator;
            _subject = subject;
            _pureCoorSystem = new PureCoorSystem<LMPlace>(baseCoorSystem.GetMainMatrixElements().GetMatrix3());
            _baseCoorSystem = baseCoorSystem;
            _teachingPointsCoorSystem = baseCoorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);
            _underCamera = underCamera;//TODO use it outside of the constructor
            _zPiercing = _underCamera ? _zeroZCamera : _zeroZPiercing;//TODO use it outside of the constructor
            _waferEnumerator = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle().GetEnumerator()
                            : _wafer.GetEnumerator();
            _underCamera = underCamera;
            _fileAlignment = aligningPoints;
            _waferAngle = waferAngle;
            _triggersQueue = new();
        }

        public event EventHandler<IEnumerable<IProcObject>> CurrentWaferChanged;
        public event EventHandler<(IProcObject, int)> ProcessingObjectChanged;
        public event EventHandler<ProcessCompletedEventArgs> ProcessingCompleted;
        enum State
        {
            Teaching,
            Started,
            GoRefPoint,
            GetRefPoint,
            Working,
            Paused,
            Denied,
            Exit,
            Loop
        }
        enum Trigger
        {
            Next,
            Pause,
            Deny
        }

        public void CreateProcess()
        {
            var currentIndex = -1;

            var resultPoints = new List<PointF>();
            var originPoints = new List<PointF>();

            _subject.OfType<SnapShotResult>()
                .Subscribe(result =>
                {
                    var point = _serviceWafer.GetPointToWafer(result);
                    originPoints.Add(point);
                    _subject.OnNext(new ProcessMessage("", MsgType.Clear));
                    _triggersQueue.Enqueue(Trigger.Next);
                });

            _subject.OfType<ReadyForSnap>()
                .Subscribe(result =>
                {
                    var position = _teachingPointsCoorSystem.FromGlobal(_xActual, _yActual);
                    var point = _serviceWafer.GetPointFromWafer(new((float)position[0], (float)position[1]));
                    var request = new ScopedGeomsRequest(5000, 5000, point.X, point.Y);
                    _subject.OnNext(request);
                });

            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;

            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);

            _stateMachine.Configure(State.Teaching)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Started)
                .SubstateOf(State.Teaching)
                .OnActivate(() => _subject.OnNext(new ProcessMessage("Next", MsgType.Info)))
                .PermitDynamic(Trigger.Next, () => _fileAlignment switch
                {
                    FileAlignment.AlignByThreePoint or FileAlignment.AlignByTwoPoint => State.GoRefPoint,
                    FileAlignment.AlignByCorner => State.GetRefPoint,
                    _ => throw new ArgumentOutOfRangeException($"{_fileAlignment} isn't supported")
                });

            _stateMachine.Configure(State.GoRefPoint)
                .SubstateOf(State.Teaching)
                .OnEntry(() =>
                {
                    _subject.OnNext(new PermitSnap(true));
                    _subject.OnNext(new ProcessMessage($"Сопоставьте {(originPoints.Count + 1).ToOrdinalWords(GrammaticalGender.Feminine).ApplyCase(GrammaticalCase.Accusative)} точку ", MsgType.Info));
                })
                .OnExit(() =>
                {
                    var xAct = _xActual;// _laserMachine.GetAxActual(Ax.X);
                    var yAct = _yActual;// _laserMachine.GetAxActual(Ax.Y);
                    var x = (float)(xAct + (_underCamera ? 0 : _dX));
                    var y = (float)(yAct + (_underCamera ? 0 : _dY));
                    resultPoints.Add(new(x, y));
                })
                .PermitReentryIf(Trigger.Next,
                () => resultPoints.Count < (_fileAlignment switch
                {
                    FileAlignment.AlignByThreePoint => 2,
                    FileAlignment.AlignByTwoPoint => 1,
                    _ => 2
                }))
                .PermitIf(Trigger.Next, State.GetRefPoint,
                () => resultPoints.Count == (_fileAlignment switch
                {
                    FileAlignment.AlignByThreePoint => 2,
                    FileAlignment.AlignByTwoPoint => 1,
                    _ => 2
                }));

            _stateMachine.Configure(State.GetRefPoint)
                //.SubstateOf(State.Teaching)
                .OnEntry(() => _subject.OnNext(new ProcessMessage("", MsgType.Clear)))
                .OnEntry(() =>
                {
                    _subject.OnNext(new PermitSnap(false));
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    _workCoorSystem = _fileAlignment switch
                    {
                        FileAlignment.AlignByThreePoint => new CoorSystem<LMPlace>
                       .ThreePointCoorSystemBuilder()
                       .SetFirstPointPair(originPoints[0], resultPoints[0])
                       .SetSecondPointPair(originPoints[1], resultPoints[1])
                       .SetThirdPointPair(originPoints[2], resultPoints[2])
                       .FormWorkMatrix(0.001, 0.001)
                       .Build(),
                        FileAlignment.AlignByTwoPoint => _pureCoorSystem
                            .GetTwoPointSystemBuilder()
                            .SetFirstPointPair(originPoints[0], resultPoints[0])
                            .SetSecondPointPair(originPoints[1], resultPoints[1])
                            .FormWorkMatrix(-1, -1)
                            .Build(),
                        FileAlignment.AlignByCorner => _baseCoorSystem.ExtractSubSystem(_underCamera ? LMPlace.FileOnWaferUnderCamera : LMPlace.FileOnWaferUnderLaser),
                        _ => throw new InvalidOperationException()
                    };
                    _matrixAngle = _workCoorSystem.GetMatrixAngle();
                    _triggersQueue.Enqueue(Trigger.Next);
                })
                .OnExit(() =>
                {
                    _waferEnumerator.MoveNext();
                    if (_fileAlignment == FileAlignment.AlignByCorner) _entityPreparator.SetEntityAngle(_waferAngle).AddEntityAngle(_pazAngle);
                    _entityPreparator.SetEntityAngle(-_pazAngle - _matrixAngle);
                })
                .Permit(Trigger.Next, State.Working);

            _stateMachine.Configure(State.Working)
                .OnEntryAsync(async () =>
                {
                    var procObject = _waferEnumerator.Current;
                    _subject.OnNext(new ProcObjectChanged(procObject));

                    if (!_excludedObjects?.Any(o => o.Id == procObject.Id) ?? true)
                    {
                        var position = _workCoorSystem.ToGlobal(procObject.X, procObject.Y);
                        _laserMachine.SetVelocity(Velocity.Fast);
                        await Task.WhenAll(
                             _laserMachine.MoveAxInPosAsync(Ax.Y, position[1], true),
                             _laserMachine.MoveAxInPosAsync(Ax.X, position[0], true),
                             Task.Run(() => { if (!_underCamera) _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness); })
                             );
                        procObject.IsBeingProcessed = true;

                        if (_inProcess && !_underCamera)
                        {
                            await _pierceFunction();
                        }
                        else
                        {
                            await Task.Delay(1000);
                            var pointsLine = string.Empty;
                            var line = $" X: {Math.Round(procObject.X, 3),8} | Y: {Math.Round(procObject.Y, 3),8} | X: {Math.Round(position[0], 3),8} | Y: {Math.Round(position[1], 3),8} | X: {Math.Round(_laserMachine.GetAxActual(Ax.X), 3),8} | Y: {Math.Round(_laserMachine.GetAxActual(Ax.Y), 3),8}";
                            if (HandyControl.Controls.MessageBox.Ask("Good?") == System.Windows.MessageBoxResult.OK)
                            {
                                pointsLine = "GOOD" + line;
                            }
                            else
                            {
                                pointsLine = $"BAD " + line;
                            }
                            //await _coorFile.WriteLineAsync(pointsLine);
                            //await _coorFile.FlushAsync();
                        }
                        procObject.IsProcessed = true;
                    }
                    _subject.OnNext(new ProcObjectChanged(procObject));
                    _inLoop = _waferEnumerator.MoveNext();
                    currentIndex++;
                    _triggersQueue.Enqueue(Trigger.Next);
                })
                .PermitReentryIf(Trigger.Next, () => _inLoop)
                .PermitIf(Trigger.Next, State.Loop, () => !_inLoop)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Loop)
                .OnEntry(() =>
                {
                    _loopCount++;
                    var currentWafer = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle() : _wafer;
                    CurrentWaferChanged?.Invoke(this, currentWafer);

                    _subject.OnNext(new ProcWaferChanged(currentWafer));

                    _waferEnumerator = currentWafer.GetEnumerator();
                    currentIndex = -1;
                    _inLoop = _waferEnumerator.MoveNext();
                    currentIndex++;
                    _triggersQueue.Enqueue(Trigger.Next);
                })
                .OnExit(() => _inProcess = _loopCount < _progTreeParser.MainLoopCount)
                .PermitIf(Trigger.Next, State.Working, () => _loopCount < _progTreeParser.MainLoopCount)
                .PermitIf(Trigger.Next, State.Exit, () => _loopCount >= _progTreeParser.MainLoopCount);

            _stateMachine.Configure(State.Exit)
                .OnEntry(() =>
                {
                    _subject.OnNext(new ProcCompletionPreview(CompletionStatus.Success, _workCoorSystem));
                })
                .Ignore(Trigger.Next);

            _stateMachine.Configure(State.Denied)
                .OnEntryAsync(() =>
                {
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    ProcessingCompleted?.Invoke(this, new ProcessCompletedEventArgs(CompletionStatus.Cancelled, _workCoorSystem));
                    return Task.CompletedTask;
                    //ctSource.Cancel();
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
            _cancellationTokenSource?.Cancel();
            await _laserMachine.CancelMarkingAsync();
        }
        public void ExcludeObject(IProcObject procObject) => throw new NotImplementedException();
        public void IncludeObject(IProcObject procObject) => throw new NotImplementedException();
        public Task Next()
        {
            if (_stateMachine?.IsInState(State.Teaching) ?? false) _triggersQueue.Enqueue(Trigger.Next);
            return Task.CompletedTask;
        }
        public async Task StartAsync()
        {
            _cancellationTokenSource = new();
            if (_cancellationTokenSource.Token.IsCancellationRequested) return;
            if (_stateMachine is null)
            {
                CreateProcess();
                await _stateMachine.ActivateAsync();

                _inProcess = true;
                for (var i = 0; i < _progTreeParser.MainLoopCount; i++)
                {
                    while (_inProcess)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _inProcess = false;
                            continue;
                        }

                        if (_triggersQueue.TryDequeue(out var trigger))
                        {
                            await _stateMachine.FireAsync(trigger);
                        }
                    }
                }
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Trace.TraceInformation("The process ended");
                    Trace.Flush();
                }
                else
                {
                    Trace.TraceInformation("The process was interrupted by user");
                    Trace.Flush();
                    _subject.OnNext(new ProcCompletionPreview(CompletionStatus.Cancelled, _workCoorSystem));
                }
            }
        }
        public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GeneralLaserProcess()
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

        protected override Task FuncForTapperBlockAsync(double tapper)
        {
            _entityPreparator.SetEntityContourOffset(tapper);
            return Task.CompletedTask;
        }
        protected async override Task FuncForAddZBlockAsync(double z)
        {
            await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true);
        }
        protected async override Task FuncForPierseBlockAsync(ExtendedParams extendedParams)
        {
            using var fileHandler = _entityPreparator.GetPreparedEntityDxfHandler(_waferEnumerator.Current);
            _laserMachine.SetExtMarkParams(new ExtParamsAdapter(extendedParams));
            var result = await _laserMachine.PierceDxfObjectAsync(fileHandler.FilePath).ConfigureAwait(false);
        }
        protected async override Task FuncForDelayBlockAsync(int delay)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);
        }
    }
}
