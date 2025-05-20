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
using System.Windows;
using Humanizer;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Markers;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Machine.MotionDevices;
using MachineClassLibrary.Miscellaneous;
using NewLaserProject.Classes.Process.ProcessFeatures;
using NewLaserProject.ViewModels;
using Stateless;

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
        private double _dX;
        private double _dY;
        private readonly IEnumerable<OffsetPoint> _offsetPoints;
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
        private readonly bool _invertEntAngle;
        private CancellationTokenSource _currentMicroProcCts;
        private CancellationToken _mainCancellationToken;
        private CancellationTokenSource _cancellationTokenSource;
        private CoeffLine _yCoeffLine;
        private CoeffLine _xCoeffLine;
        private bool _goNextPoint;

        public CommonProcess(IEnumerable<(IEnumerable<IProcObject> procObjects, MicroProcess microProcess)> processing,
               LaserWafer wafer,
               LaserMachine laserMachine,
               double zeroZPiercing,
               double zeroZCamera,
               double waferThickness,
               double dX,
               double dY,
               IEnumerable<OffsetPoint> offsetPoints,
               double pazAngle,
               ISubject<IProcessNotify> subject,
               ICoorSystem<LMPlace> baseCoorSystem,
               bool underCamera,
               FileAlignment aligningPoints,
               double waferAngle,
               Scale scale,
               CoeffLine coeffLineX,
               CoeffLine coeffLineY,
               bool invertEntAngle)
        {
            _procWafer = wafer;
            _laserMachine = laserMachine;
            _zeroZPiercing = zeroZPiercing;
            _zeroZCamera = zeroZCamera;
            _waferThickness = waferThickness;
            _dX = dX;
            _dY = dY;
            _offsetPoints = offsetPoints;
            _pazAngle = pazAngle;
            _subject = subject;
            _pureCoorSystem = new PureCoorSystem<LMPlace>(baseCoorSystem.GetMainMatrixElements().GetMatrix3());
            _baseCoorSystem = baseCoorSystem;
            _teachingPointsCoorSystem = aligningPoints == FileAlignment.AlignPrev ? baseCoorSystem : baseCoorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera);
            _underCamera = underCamera;
            _zPiercing = _underCamera ? _zeroZCamera : _zeroZPiercing;//TODO use it outside of the constructor
            _fileAlignment = aligningPoints;
            _waferAngle = waferAngle;
            _scale = scale;
            _processing = processing;



            //var objs = processing.SelectMany(o=>o.procObjects).Skip(7739);
            //var mp = processing.First().microProcess;
            //_processing = Enumerable.Repeat((objs, mp),1);



            //_yCoeffLine = new((30.028, 30.028),
            //    (28.678, 28.683), (24.578, 24.588), (20.478, 20.485), (16.378, 16.389),
            //    (12.278, 12.291), (8.178, 8.180), (4.078, 4.086), (-0.022, -0.016),
            //    (-4.122, -4.122), (-8.222, -8.228), (-12.322, -12.325),
            //    (-16.422, -16.428), (-20.522, -20.531), (-24.622, -24.630));
            //_xCoeffLine = new((0, 0), (-60, -60.011), (-3.35, -3.359), (-4.55, -4.562), (-5.75, -5.766), (-6.95, -6.965), (-8.15, -8.162),
            //    (-9.35, -9.367), (-10.55, -10.566), (-11.75, -11.767), (-12.95, -12.968), (-14.15, -14.169),
            //    (-15.35, -15.369), (-16.55, -16.569), (-17.75, -17.771), (-18.95, -18.969), (-20.15, -20.167),
            //    (-21.35, -21.37), (-22.55, -22.568), (-23.75, -23.772), (-24.95, -24.972), (-26.15, -26.174),
            //    (-27.35, -27.374), (-28.55, -28.572), (-29.75, -29.772), (-30.95, -30.969), (-32.15, -32.166),
            //    (-33.35, -33.37), (-34.55, -34.569), (-35.75, -35.769), (-36.95, -36.968), (-38.15, -38.168),
            //    (-39.35, -39.368), (-40.55, -40.465), (-41.75, -41.766), (-42.95, -42.967), (-44.15, -44.168),
            //    (-45.35, -45.371), (-46.55, -46.570), (-47.75, -47.768), (-48.95, -48.972), (-50.15, -50.172),
            //    (-51.35, -51.372), (-52.55, -52.573), (-53.75, -53.774), (-54.95, -54.977), (-56.15, -56.176),
            //    (-57.35, -57.378));
            _xCoeffLine = true ? coeffLineX : new((-200, -200), (200, 200));
            _yCoeffLine = true ? coeffLineY : new((-200, -200), (200, 200));
            _invertEntAngle = invertEntAngle;
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
                FileAlignment.AlignByCorner or FileAlignment.AlignPrev => State.InitialCorner,
                _ => State.InitialPoints
            }, FiringMode.Queued);

            var workingTrigger = _stateMachine.SetTriggerParameters<ICoorSystem>(Trigger.Next);
            var lineTeachingTrigger = _stateMachine.SetTriggerParameters<ICoorSystem>(Trigger.Preteach);
            var waferEnumerator = _processing.SelectMany(p=>p.procObjects).GetEnumerator();

            static int properPointsCount(FileAlignment aligning) => aligning switch
            {
                FileAlignment.AlignByThreePoint => 3,
                FileAlignment.AlignByTwoPoint => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(aligning))
            };

            async Task ProcessingTheWaferAsync(ICoorSystem coorSystem)
            {
                try
                {
                    if (_mainCancellationToken.IsCancellationRequested) return;
                    _subject.OnNext(new ProcessingStarted(_underCamera));
                    foreach (var item in _processing)
                    {
                        item.microProcess.Subscribe(_subject);//TODO using
                        _currentMicroProcCts = item.microProcess.GetCancellationTokenSource();
                        for (var i = 0; i < item.microProcess.GetMainLoopCount(); i++)
                        {
                            _subject.OnNext(new MainLoopChanged(i + 1));
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
                                    _laserMachine.SetVelocity(Velocity.Service);
                                    try
                                    {
                                        var y = _yCoeffLine[position[1]];
                                        var x = _xCoeffLine[position[0]];
                                        _offsetPoints.GetOffsetByCurCoor(x, y, ref _dX, ref _dY);
                                        x = _underCamera ? x : x + _dX;
                                        y = _underCamera ? y : y + _dY;

                                        var precise = true;
                                        await Task.WhenAll(_laserMachine.MoveAxInPosAsync(Ax.Y, y, precise),
                                                           _laserMachine.MoveAxInPosAsync(Ax.X, x, precise));//.ConfigureAwait(false);
                                        _laserMachine.ResetErrors(Ax.Z);
                                        if (!_underCamera) await _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness);//.ConfigureAwait(false);

                                    }
                                    catch (MotionException ex) when (ex.MotionExStatus == MotionExStatus.AccuracyNotReached)
                                    {
                                        _subject.OnNext(new ProcessException(ex.Message));
                                    }
                                    catch (Exception)
                                    {
                                        throw;
                                    }
                                    procObject.IsBeingProcessed = true;
                                    _subject.OnNext(new ProcObjectChanged(procObject));
                                    if (_inProcess && !_underCamera)
                                    {
                                        try
                                        {
                                            await item.microProcess.InvokePierceFunctionForObjectAsync(procObject);//.ConfigureAwait(false);
                                        }
                                        catch (MarkerException ex)
                                        {
                                            _subject.OnNext(new ProcessMessage(ex.Message, MsgType.Error));
                                            Console.Error.WriteLine(ex.Message);
                                            item.microProcess.Dispose();
                                            goto M1;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.Error.WriteLine(ex.Message);
                                            item.microProcess.Dispose();
                                            goto M1;
                                        }
                                    }
                                    else
                                    {
                                        await Task.Delay(1000);
                                        var pointsLine = string.Empty;
                                        var line = $" X: {Math.Round(procObject.X, 3),8} | Y: {Math.Round(procObject.Y, 3),8} | X: {Math.Round(position[0], 3),8} | Y: {Math.Round(position[1], 3),8} | X: {Math.Round(_laserMachine.GetAxActual(Ax.X), 3),8} | Y: {Math.Round(_laserMachine.GetAxActual(Ax.Y), 3),8}";


                                        await Task.Run(() => { while (!_goNextPoint) ; });
                                        _goNextPoint = false;


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
                        item.microProcess.Dispose();
                    }
M1: _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    var completeStatus = _mainCancellationToken.IsCancellationRequested ? CompletionStatus.Cancelled : CompletionStatus.Success;
                    _subject.OnNext(new ProcessingStopped());
                    _subject.OnNext(new ProcCompletionPreview(completeStatus, coorSystem));
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
            _subject.OfType<SnapShotResult>()
                .Select(result => Observable.FromAsync(async () =>
               {
                   var point = _procWafer.GetPointToWafer(result);
                   originPoints.Add(point);
                   var dblx = _laserMachine.GetAxActual(Ax.X);
                   var dbly = _laserMachine.GetAxActual(Ax.Y);
                   var x = (float)(dblx + (_underCamera ? 0 : _dX));
                   var y = (float)(dbly + (_underCamera ? 0 : _dY));
                   //resultPoints.Add(new(x, y));
                   resultPoints.Add(new((float)dblx, (float)dbly));
                   _subject.OnNext(new ProcessMessage("", MsgType.Clear));
                   _subject.OnNext(new SnapNotAlowed());
                   if (resultPoints.Count == properPointsCount(_fileAlignment))
                   {
                       var coorSys = _fileAlignment switch
                       {
                           FileAlignment.AlignByThreePoint => new CoorSystem<LMPlace>
                          .ThreePointCoorSystemBuilder()
                          .SetFirstPointPair(originPoints[0], resultPoints[0])
                          .SetSecondPointPair(originPoints[1], resultPoints[1])
                          .SetThirdPointPair(originPoints[2], resultPoints[2])
                          //.UseYCoeffLine(_yCoeffLine)
                          //.UseXCoeffLine(_xCoeffLine)
                          .FormWorkMatrix(_scale, _scale)
                          .Build(),
                           FileAlignment.AlignByTwoPoint => _pureCoorSystem
                               .GetTwoPointSystemBuilder()
                               .SetFirstPointPair(originPoints[0], resultPoints[0])
                               .SetSecondPointPair(originPoints[1], resultPoints[1])
                               //.UseXCoeffLine(_xCoeffLine)
                               //.UseYCoeffLine(_yCoeffLine)
                               .FormWorkMatrix(_scale, _scale)// TODO fix it
                               .Build(),
                           //FileAlignment.AlignByCorner => _baseCoorSystem.ExtractSubSystem(_underCamera ? LMPlace.FileOnWaferUnderCamera : LMPlace.FileOnWaferUnderLaser),
                           FileAlignment.AlignByCorner => _baseCoorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera),
                           _ => throw new InvalidOperationException()
                       };
                       //_matrixAngle = coorSys.GetMatrixAngle2();
                       _matrixAngle = coorSys.GetMatrixAngle();

                       _subject.OnNext(new GotAlignment(coorSys)); 


                       foreach (var item in _processing.Select(p => p.microProcess))
                       {
                           //item.SetEntityAngle(-_pazAngle + _matrixAngle);
                           item.SetEntityAngle((_invertEntAngle ? -1:1) * _matrixAngle);//TODO fix the sign's problem
                       }

                       await _stateMachine.FireAsync(workingTrigger, coorSys);
                      // await _stateMachine.FireAsync(lineTeachingTrigger, coorSys);
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
                    var position = _teachingPointsCoorSystem.FromGlobal(_laserMachine.GetAxActual(Ax.X), _laserMachine.GetAxActual(Ax.Y));
                    var point = _procWafer.GetPointFromWafer(new((float)position[0], (float)position[1]));
                    var request = new ScopedGeomsRequest(2.5 * _scale, 2.5 * _scale, point.X, point.Y);
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
                        //item.SetEntityAngle(_waferAngle - _pazAngle);
                        item.SetEntityAngle(/*-*/(_invertEntAngle ? -1 : 1) * _waferAngle);//TODO fix sign's problem
                    }
                    //await _stateMachine.FireAsync(workingTrigger, _baseCoorSystem.ExtractSubSystem(_underCamera ? LMPlace.FileOnWaferUnderCamera : LMPlace.FileOnWaferUnderLaser));
                    if (_fileAlignment == FileAlignment.AlignPrev)
                    {
                        _matrixAngle = _baseCoorSystem.GetMatrixAngle();
                        foreach (var item in _processing.Select(p => p.microProcess))
                        {
                            //item.SetEntityAngle(-_pazAngle + _matrixAngle);
                            item.SetEntityAngle(/*-*/(_invertEntAngle ? -1 : 1) * _matrixAngle);//TODO fix the sign's problem
                        }
                        await _stateMachine.FireAsync(workingTrigger, _baseCoorSystem);
                    }
                    else
                    {
                        await _stateMachine.FireAsync(workingTrigger, _baseCoorSystem.ExtractSubSystem(LMPlace.FileOnWaferUnderCamera));
                    }
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
                .PermitIf(Trigger.Preteach, State.LinePreteaching, () => resultPoints.Count == properPointsCount(_fileAlignment));

            _stateMachine.Configure(State.Working)
                .OnEntryFromAsync(workingTrigger, ProcessingTheWaferAsync)
                //.Permit(Trigger.Next, State.LineTeaching)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.LinePreteaching)
               .OnEntryFrom(lineTeachingTrigger, (ICoorSystem coorSystem) =>
               {
                   if (waferEnumerator.MoveNext())
                   {
                       _subject.OnNext(new ProcessMessage("Нажмите next", MsgType.Info));
                       _teachingLinesCoorSystem = coorSystem;
                   }
               })
               .Permit(Trigger.Next, State.LineTeaching)
               .Ignore(Trigger.Pause);

            var xLineSb = new StringBuilder();
            var yLineSb = new StringBuilder();

            var xLCollection = new List<(string orig, string deriv)>();
            var yLCollection = new List<(string orig, string deriv)>();
            var cultureInfo = CultureInfo.InvariantCulture;
            double xl = 0;
            double yl = 0;
            var teachingLineReentry = false;

            _stateMachine.Configure(State.LineTeaching)
                .OnEntryAsync(async () =>
                {
                    var procObject = _procWafer.GetProcObjectToWafer(waferEnumerator.Current);
                    _subject.OnNext(new ProcObjectChanged(procObject));

                    if (!_excludedObjects?.Any(o => o.Id == procObject.Id) ?? true)
                    {
                        var position = _teachingLinesCoorSystem.ToGlobal(procObject.X, procObject.Y);
                        var vel = _laserMachine.SetVelocity(Velocity.Service);
                        await Task.WhenAll(
                             _laserMachine.MoveAxInPosAsync(Ax.Y, position[1], true),
                             _laserMachine.MoveAxInPosAsync(Ax.X, position[0], true));
                        procObject.IsBeingProcessed = true;
                        _laserMachine.SetVelocity(vel);
                        _subject.OnNext(new ProcObjectChanged(procObject));

                        xLineSb.Append($"({_xActual.ToString(cultureInfo)},");
                        yLineSb.Append($"({_yActual.ToString(cultureInfo)},");
                        xl = position[0];// _xActual;
                        yl = position[1];// _yActual;

                        _subject.OnNext(new ProcessMessage("Скорректируйте и нажмите next", MsgType.Info));

                        procObject.IsProcessed = true;
                        _subject.OnNext(new ProcObjectChanged(procObject));
                    }
                    teachingLineReentry = waferEnumerator.MoveNext();
                })
                .PermitReentryIf(Trigger.Next, () => teachingLineReentry)
                .PermitIf(Trigger.Next,State.Exit,() => !teachingLineReentry)
                .OnExit(t =>
                {
                    _subject.OnNext(new ProcessMessage("", MsgType.Clear));
                    xLineSb.Append($"{_xActual.ToString(cultureInfo)}),");
                    yLineSb.Append($"{_yActual.ToString(cultureInfo)}),");
                    xLCollection.Add((xl.ToString(cultureInfo), _xActual.ToString(cultureInfo)));
                    yLCollection.Add((yl.ToString(cultureInfo), _yActual.ToString(cultureInfo)));

                    _xCoeffLine.AddPoint(xl,_xActual);
                    _yCoeffLine.AddPoint(yl,_yActual);

                    if (t.Destination == State.Exit)
                    {
                        _xCoeffLine.GetValues().SerializeObject(AppPaths.CoefLineX);
                        _yCoeffLine.GetValues().SerializeObject(AppPaths.CoefLineY);
                    }
                });

            _stateMachine.Configure(State.Exit)
                 .OnEntryAsync(() =>
                 {
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
        public void ExcludeObject(IProcObject procObject)
        {
            _excludedObjects ??= new();
            _excludedObjects.Add(procObject);
        }
        public void IncludeObject(IProcObject procObject) => _excludedObjects?.Remove(procObject);
        public async Task Next()
        {
            if (!_underCamera) await _stateMachine.FireAsync(Trigger.Next);//TODO what if push it when processing?
            else _goNextPoint = true;
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
