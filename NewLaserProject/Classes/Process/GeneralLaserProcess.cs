using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
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
    internal class GeneralLaserProcess : BaseLaserProcess, IProcess
    {
        private bool disposedValue;
        private StateMachine<State, Trigger> _stateMachine;
        private double _xActual;
        private double _yActual;
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly LaserWafer _serviceWafer;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _baseCoorSystem;
        private readonly ICoorSystem<LMPlace> _teachingPointsCoorSystem;
        private readonly PureCoorSystem<LMPlace> _pureCoorSystem;
        private double _matrixAngle;
        private readonly double _zeroZPiercing;
        private readonly double _zeroZCamera;
        private readonly double _waferThickness;
        private readonly double _dX;
        private readonly double _dY;
        private readonly double _pazAngle;
        private readonly double _waferAngle;
        private readonly Scale _scale;
        private readonly EntityPreparator _entityPreparator;
        private readonly ISubject<IProcessNotify> _subject;
        private List<IProcObject> _excludedObjects;
        private bool _inProcess = false;
        private readonly bool _underCamera;
        private readonly double _zPiercing;
        private readonly FileAlignment _fileAlignment;
        private List<IDisposable> _subscriptions;
        private IProcObject _currentProcObject;
        private readonly Guid _procId = Guid.NewGuid();

        public GeneralLaserProcess(IEnumerable<IProcObject> wafer, LaserWafer serviceWafer,
               string jsonPierce, LaserMachine laserMachine,
               double zeroZPiercing, double zeroZCamera, double waferThickness,
               double dX, double dY, double pazAngle, EntityPreparator entityPreparator,
               ISubject<IProcessNotify> subject, ICoorSystem<LMPlace> baseCoorSystem,
               bool underCamera, FileAlignment aligningPoints, double waferAngle, Scale scale) : base(jsonPierce)
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
            _underCamera = underCamera;
            _zPiercing = _underCamera ? _zeroZCamera : _zeroZPiercing;//TODO use it outside of the constructor
            _underCamera = underCamera;
            _fileAlignment = aligningPoints;
            _waferAngle = waferAngle;
            _scale = scale;
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
            InitialCorner,
            InitialPoints,
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
            if (disposedValue) throw new ObjectDisposedException($"Object {_procId} has already disposed");
            var resultPoints = new List<PointF>();
            var originPoints = new List<PointF>();

            _stateMachine = new StateMachine<State, Trigger>(_fileAlignment switch
            {
                FileAlignment.AlignByCorner => State.InitialCorner,
                _ => State.InitialPoints
            }, FiringMode.Queued);

            var workingTrigger = _stateMachine.SetTriggerParameters<ICoorSystem>(Trigger.Next);

            static int properPointsCount(FileAlignment aligning) => aligning switch
            {
                FileAlignment.AlignByThreePoint => 3,
                FileAlignment.AlignByTwoPoint => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(aligning))
            };
            async Task ProcessingTheWaferAsync(ICoorSystem coorSystem)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested) return;
                var currentWafer = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle() : _wafer;

                for (var i = 0; i < _progTreeParser.MainLoopCount; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested) break;
                    foreach (var procObject in currentWafer)
                    {
                        _currentProcObject = procObject;
                        if (_cancellationTokenSource.Token.IsCancellationRequested) break;
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
                                await _pierceFunction();
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
                                //await _coorFile.WriteLineAsync(pointsLine);
                                //await _coorFile.FlushAsync();
                            }
                            procObject.IsProcessed = true;
                            _subject.OnNext(new ProcObjectChanged(procObject));
                        }
                    }
                }
                _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                var completeStatus = _cancellationTokenSource.Token.IsCancellationRequested ? CompletionStatus.Cancelled : CompletionStatus.Success;
                _subject.OnNext(new ProcCompletionPreview(completeStatus, coorSystem));
            }

            _subject.OfType<SnapShotResult>()
                .Subscribe(result =>
                {
                    var point = _serviceWafer.GetPointToWafer(result);
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
                        //_matrixAngle = coorSys.GetMatrixAngle();
                        _matrixAngle = coorSys.GetMatrixAngle2();

                        _entityPreparator.SetEntityAngle(-_pazAngle + _matrixAngle);
                        //_entityPreparator.SetEntityAngle(_pazAngle + _matrixAngle);

                        _stateMachine.FireAsync(workingTrigger, coorSys);
                    }
                    else
                    {
                        _stateMachine.FireAsync(Trigger.Next);
                    }
                })
                .AddSubscriptionTo(_subscriptions);

            _subject.OfType<ReadyForSnap>()
                .Subscribe(result =>
                {
                    var position = _teachingPointsCoorSystem.FromGlobal(_xActual, _yActual);
                    var point = _serviceWafer.GetPointFromWafer(new((float)position[0], (float)position[1]));
                    var request = new ScopedGeomsRequest(5000, 5000, point.X, point.Y);
                    _subject.OnNext(request);
                })
                .AddSubscriptionTo(_subscriptions);

            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;
           

            _inProcess = true;//TODO is it necessary?


            _stateMachine.Configure(State.InitialCorner)
                .OnActivateAsync(async () =>
                    {
                        _entityPreparator.SetEntityAngle(_waferAngle).AddEntityAngle(_pazAngle);
                        await _stateMachine.FireAsync(workingTrigger, _baseCoorSystem.ExtractSubSystem(_underCamera ? LMPlace.FileOnWaferUnderCamera : LMPlace.FileOnWaferUnderLaser));
                    }
                )
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
                .PermitIf(Trigger.Next, State.Working, () => resultPoints.Count == properPointsCount(_fileAlignment));

            _stateMachine.Configure(State.Working)
                .OnEntryFromAsync(workingTrigger, ProcessingTheWaferAsync)
                .Ignore(Trigger.Pause);

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
            _cancellationTokenSource?.Cancel();
            await _laserMachine.CancelMarkingAsync();
        }
        public void ExcludeObject(IProcObject procObject) => _excludedObjects.Add(procObject);
        public void IncludeObject(IProcObject procObject) => _excludedObjects.Remove(procObject);
        public async Task Next()
        {
            throw new NotImplementedException();
        }
        public async Task StartAsync()
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested) return;
            if (_stateMachine is null)
            {
                CreateProcess();
                await _stateMachine.ActivateAsync().ConfigureAwait(false);
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

        //public void Dispose()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}
        public void Dispose()
        {
            // Никаких Dispose(true) и никаких вызовов GC.SuppressFinalize()
            DisposeManagedResources(); 
            disposedValue = true;
        }

        // Никаких параметров, этот метод должен освобождать только неуправляемые ресурсы
        protected virtual void DisposeManagedResources()
        {
            _subscriptions?.ForEach(s => s.Dispose());
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
            using var fileHandler = _entityPreparator.GetPreparedEntityDxfHandler(_currentProcObject);
            _laserMachine.SetExtMarkParams(new ExtParamsAdapter(extendedParams));
            var result = await _laserMachine.PierceDxfObjectAsync(fileHandler.FilePath).ConfigureAwait(false);
        }
        protected async override Task FuncForDelayBlockAsync(int delay)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);
        }
    }

    internal record ProcessParams(IEnumerable<IProcObject> Wafer, LaserWafer ServiceWafer, LaserMachine LaserMachine, EntityPreparator EntityPreparator, ISubject<IProcessNotify> Subject, ICoorSystem<LMPlace> BaseCoorSystem, FileAlignment AligningPoints, double ZeroZPiercing, double ZeroZCamera, double WaferThickness, double DX, double DY, double PazAngle, double WaferAngle, string JsonPierce, bool UnderCamera);
}
