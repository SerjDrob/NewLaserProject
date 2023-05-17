using Humanizer;
using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using MachineClassLibrary.Miscellanius;
using MediatR;
using Microsoft.Toolkit.Diagnostics;
using NewLaserProject.Classes.Mediator;
using NewLaserProject.ViewModels;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.Process
{
    internal class PointsProcessSnap : IProcess
    {
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly LaserWafer _serviceWafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private readonly double _zeroZPiercing;
        private readonly double _zeroZCamera;
        private readonly double _waferThickness;
        private CancellationTokenSource _ctSource;
        private readonly InfoMessager _infoMessager;
        private double _xActual;
        private double _xCmd;
        private double _yActual;
        private double _yCmd;
        private readonly double _dX;
        private readonly double _dY;
        private readonly double _pazAngle;
        private readonly EntityPreparator _entityPreparator;
        private double _matrixAngle;
        private IProcess _subProcess;
        private readonly ISubject<IProcessNotify> _subject;
        private List<IDisposable> _subscriptions;
        private readonly bool _underCamera;
        private ICoorSystem _workCoorSys;
        private PureCoorSystem<LMPlace> _pureCoorSys;
        private readonly AligningPoints _aligningPoints;

        public PointsProcessSnap(IEnumerable<IProcObject> wafer, LaserWafer serviceWafer,
            string jsonPierce, LaserMachine laserMachine, ICoorSystem coorSystem,
            double zeroZPiercing, double zeroZCamera, double waferThickness, InfoMessager infoMessager,
            double dX, double dY, double pazAngle, EntityPreparator entityPreparator, ISubject<IProcessNotify> mediator)
        {
            _underCamera = false;// true;
            _wafer = wafer;
            _serviceWafer = serviceWafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zeroZPiercing = zeroZPiercing;
            _zeroZCamera = zeroZCamera;
            _infoMessager = infoMessager;
            _dX = dX;
            _dY = dY;
            _pazAngle = pazAngle;
            _entityPreparator = entityPreparator;
            _waferThickness = waferThickness;
            _ctSource = new CancellationTokenSource();
            _subject = mediator;
            _aligningPoints = AligningPoints.ThreePoints;
        }

        public PointsProcessSnap(IEnumerable<IProcObject> wafer, LaserWafer serviceWafer,
           string jsonPierce, LaserMachine laserMachine, ICoorSystem coorSystem,
           double zeroZPiercing, double zeroZCamera, double waferThickness, InfoMessager infoMessager,
           double dX, double dY, double pazAngle, EntityPreparator entityPreparator,
           ISubject<IProcessNotify> mediator, PureCoorSystem<LMPlace> pureCoorSystem)
            : this(wafer, serviceWafer,
           jsonPierce, laserMachine, coorSystem,
           zeroZPiercing, zeroZCamera, waferThickness, infoMessager,
           dX, dY, pazAngle, entityPreparator, mediator)
        {
            _pureCoorSys = pureCoorSystem;
            _aligningPoints = AligningPoints.TwoPoints;
        }

        public void CreateProcess()
        {
            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);
            var resultPoints = new List<PointF>();
            var originPoints = new List<PointF>();

            _subject.OfType<SnapShotResult>()
                .Subscribe(result =>
                {
                    var point = _serviceWafer.GetPointToWafer(result);
                    originPoints.Add(point);
                    _subject.OnNext(new ProcessMessage("", MsgType.Clear));
                    _stateMachine.FireAsync(Trigger.Next);//TODO how to catch an exception?
                });

            _subject.OfType<ReadyForSnap>()
                .Subscribe(result =>
                {
                    var position = _coorSystem.FromGlobal(_xActual, _yActual);
                    var point = _serviceWafer.GetPointFromWafer(new((float)position[0], (float)position[1]));
                    var request = new ScopedGeomsRequest(5000, 5000, point.X, point.Y);
                    _subject.OnNext(request);
                });

            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;

            SwitchCamera?.Invoke(this, true);

            _stateMachine.Configure(State.Started)
                .OnActivate(() => _infoMessager.RealeaseMessage("Next", MessageType.Info))
                .Permit(Trigger.Next, State.GoRefPoint)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GoRefPoint)
                .OnEntry(() =>
                {
                    _subject.OnNext(new PermitSnap(true));
                    _subject.OnNext(new ProcessMessage($"Сопоставьте {(originPoints.Count + 1)
                        .ToOrdinalWords(GrammaticalGender.Feminine).ApplyCase(GrammaticalCase.Accusative)} точку ", MsgType.Info));
                })
                .OnExit(() =>
                {
                    var xAct = _laserMachine.GetAxActual(Ax.X);
                    var yAct = _laserMachine.GetAxActual(Ax.Y);
                    var x = (float)(xAct + (_underCamera ? 0 : _dX));
                    var y = (float)(yAct + (_underCamera ? 0 : _dY));
                    resultPoints.Add(new(x, y));
                })
                .PermitReentryIf(Trigger.Next, () => resultPoints.Count < (_aligningPoints switch { AligningPoints.ThreePoints => 2, AligningPoints.TwoPoints => 1, _ => 2 }))
                .PermitIf(Trigger.Next, State.GetRefPoint, () => resultPoints.Count == (_aligningPoints switch { AligningPoints.ThreePoints => 2, AligningPoints.TwoPoints => 1, _ => 2 }))
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GetRefPoint)
                .OnEntry(_infoMessager.EraseMessage)
                .OnEntry(() =>
                {
                    _subject.OnNext(new PermitSnap(false));
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    SwitchCamera?.Invoke(this, false);
                    _workCoorSys = _aligningPoints switch {
                        AligningPoints.ThreePoints => new CoorSystem<LMPlace>
                       .ThreePointCoorSystemBuilder()
                       .SetFirstPointPair(originPoints[0], resultPoints[0])
                       .SetSecondPointPair(originPoints[1], resultPoints[1])
                       .SetThirdPointPair(originPoints[2], resultPoints[2])
                       .FormWorkMatrix(0.001, 0.001)
                       .Build(),
                       AligningPoints.TwoPoints => _pureCoorSys
                       .GetTwoPointSystemBuilder()
                       .SetFirstPointPair(originPoints[0], resultPoints[0])
                       .SetSecondPointPair(originPoints[1], resultPoints[1])
                       .FormWorkMatrix(1, 1)
                       .Build()
                    };
                    _matrixAngle = _workCoorSys.GetMatrixAngle();
                    _stateMachine.Fire(Trigger.Next);
                })
                .Permit(Trigger.Next, State.Working)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Working)
                .OnEntryAsync(async () =>
                {
                    _entityPreparator.SetEntityAngle(-_pazAngle - _matrixAngle /*+ Math.PI*/);//TODO make add entity angle method/ fix it for Laserprocess. Get angle from outside!!!
                    _subProcess = new LaserProcess(_wafer, _jsonPierce, _laserMachine, _workCoorSys,
                    _underCamera ? _zeroZCamera : _zeroZPiercing, _waferThickness, _entityPreparator);

                    _subProcess.Subscribe(_subject);

                    _subProcess.CurrentWaferChanged += _process_CurrentWaferChanged;
                    _subProcess.ProcessingObjectChanged += _process_ProcessingObjectChanged;
                    _subProcess.ProcessingCompleted += _subProcess_ProcessingCompleted;
                    _subProcess.CreateProcess();
                    await _subProcess.StartAsync(_ctSource.Token);
                })
                .Ignore(Trigger.Next)
                .Ignore(Trigger.Deny)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Denied)
                .OnEntryAsync(async () =>
                {
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    ProcessingCompleted?.Invoke(this, new ProcessCompletedEventArgs(CompletionStatus.Cancelled, _coorSystem));
                    //ctSource.Cancel();
                });
        }

        private void _subProcess_ProcessingCompleted(object? sender, ProcessCompletedEventArgs args)
        {
            ProcessingCompleted?.Invoke(this, args);
        }

        private void _process_ProcessingObjectChanged(object? sender, (IProcObject, int) e)
        {
            ProcessingObjectChanged?.Invoke(sender, e);
        }

        private void _process_CurrentWaferChanged(object? sender, IEnumerable<IProcObject> e)
        {
            CurrentWaferChanged?.Invoke(sender, e);
        }

        public event EventHandler<bool> SwitchCamera;
        public event EventHandler<IEnumerable<IProcObject>> CurrentWaferChanged;
        public event EventHandler<(IProcObject, int)> ProcessingObjectChanged;
        public event EventHandler<ProcessCompletedEventArgs> ProcessingCompleted;

        private void _laserMachine_OnAxisMotionStateChanged(object? sender, AxisStateEventArgs e)
        {
            switch (e.Axis)
            {
                case Ax.X:
                    _xActual = e.Position;
                    _xCmd = e.CmdPosition;
                    break;
                case Ax.Y:
                    _yActual = e.Position;
                    _yCmd = e.CmdPosition;
                    break;
                default:
                    break;
            }
        }

        public override string ToString()
        {
            return UmlDotGraph.Format(_stateMachine.GetInfo());
        }
        public async Task StartAsync()
        {
            CreateProcess();
            await _stateMachine.ActivateAsync();
        }

        public async Task Next()
        {
            try
            {
                await _stateMachine.FireAsync(Trigger.Next);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task Deny() //=> await _stateMachine.FireAsync(Trigger.Deny);
        {
            _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
            _ctSource.Cancel();
            try
            {
                var success = await _laserMachine.CancelMarkingAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void ExcludeObject(IProcObject procObject)
        {
            _subProcess?.ExcludeObject(procObject);
        }

        public void IncludeObject(IProcObject procObject)
        {
            _subProcess?.IncludeObject(procObject);
        }

        public IDisposable Subscribe(IObserver<IProcessNotify> observer)
        {
            _subscriptions ??= new();
            var subscription = _subject.Subscribe(observer);
            _subscriptions.Add(subscription);
            return subscription;
        }

        public void Dispose()
        {
            _subProcess?.Dispose();
            _subscriptions?.ForEach(s => s.Dispose());
        }

        enum State
        {
            Started,
            GoRefPoint,
            GetRefPoint,
            Working,
            Paused,
            Denied
        }
        enum Trigger
        {
            Next,
            Pause,
            Deny
        }
        enum AligningPoints
        {
            ThreePoints,
            TwoPoints
        }

    }
}
