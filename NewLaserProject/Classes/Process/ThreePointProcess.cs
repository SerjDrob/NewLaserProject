using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using Microsoft.Toolkit.Diagnostics;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.ViewModels;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.Process
{
    internal class ThreePointProcess:IProcess
    {
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private readonly IEnumerable<PPoint> _refPoints;
        private readonly double _zeroZPiercing;
        private readonly double _zeroZCamera;
        private readonly double _waferThickness;
        private readonly InfoMessager _infoMessager;
        private double _xActual;
        private double _yActual;
        private readonly double _dX;
        private readonly double _dY;
        private readonly double _pazAngle;
        private readonly EntityPreparator _entityPreparator;
        private double _matrixAngle;

        public ThreePointProcess(IEnumerable<IProcObject> wafer, IEnumerable<PPoint> refPoints,
            string jsonPierce, LaserMachine laserMachine, ICoorSystem<LMPlace> coorSystem,
            double zeroZPiercing, double zeroZCamera, double waferThickness, InfoMessager infoMessager, 
            double dX, double dY, double pazAngle)
        {
            Guard.IsEqualTo(refPoints.Count(), 3, nameof(refPoints));
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zeroZPiercing = zeroZPiercing;
            _refPoints = refPoints;
            _zeroZCamera = zeroZCamera;
            _infoMessager = infoMessager;
            _dX = dX;
            _dY = dY;
            _pazAngle = pazAngle;
            _waferThickness = waferThickness;

        }

        public ThreePointProcess(IEnumerable<IProcObject> wafer, IEnumerable<PPoint> refPoints,
            string jsonPierce, LaserMachine laserMachine, ICoorSystem<LMPlace> coorSystem,
            double zeroZPiercing, double zeroZCamera, double waferThickness, InfoMessager infoMessager,
            double dX, double dY, double pazAngle, EntityPreparator entityPreparator)
        {
            Guard.IsEqualTo(refPoints.Count(), 3, nameof(refPoints));
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zeroZPiercing = zeroZPiercing;
            _refPoints = refPoints;
            _zeroZCamera = zeroZCamera;
            _infoMessager = infoMessager;
            _dX = dX;
            _dY = dY;
            _pazAngle = pazAngle;
            _entityPreparator = entityPreparator;
            _waferThickness = waferThickness;

        }


        public void CreateProcess()
        {
            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);
            var resultPoints = new List<PointF>();
            var refPointsEnumerator = _refPoints.GetEnumerator();
            refPointsEnumerator.MoveNext();
            var ctSource = new CancellationTokenSource();
            var originPoints = _refPoints.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();

            CoorSystem<LMPlace> workCoorSys = new();
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;

            SwitchCamera?.Invoke(this, true);

            _stateMachine.Configure(State.Started)
                .OnActivate(() => _infoMessager.RealeaseMessage("Next", ViewModels.Icon.Info))
                .Permit(Trigger.Next, State.GoRefPoint)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GoRefPoint)
                .OnEntryAsync(async () => 
                {
                    _laserMachine.SetVelocity(Velocity.Fast);
                    var refX = refPointsEnumerator.Current.X; 
                    var refY = refPointsEnumerator.Current.Y;
                    var points = _coorSystem.ToGlobal(refX, refY);
                    await Task.WhenAll(_laserMachine.MoveGpInPosAsync(Groups.XY, points),
                                               _laserMachine.MoveAxInPosAsync(Ax.Z, _zeroZCamera - _waferThickness));
                    _infoMessager.RealeaseMessage("Укажите точку и нажмите *", ViewModels.Icon.Info);
                })
                .OnExit(() =>
                {
                    resultPoints.Add(new((float)(_xActual + _dX), (float)(_yActual + _dY)));
                    refPointsEnumerator.MoveNext();
                })
                .PermitReentryIf(Trigger.Next, () => resultPoints.Count < 2)
                .PermitIf(Trigger.Next, State.GetRefPoint, () => resultPoints.Count == 2)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GetRefPoint)
                .OnEntry(_infoMessager.EraseMessage)
                .OnEntry(() => 
                {
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    SwitchCamera?.Invoke(this, false);
                    workCoorSys = new CoorSystem<LMPlace>
                    .ThreePointCoorSystemBuilder<LMPlace>()
                    .SetFirstPointPair(originPoints[0], resultPoints[0])
                    .SetSecondPointPair(originPoints[1], resultPoints[1])
                    .SetThirdPointPair(originPoints[2], resultPoints[2])
                    .FormWorkMatrix(0.001, 0.001, false)
                    .Build();
                    _matrixAngle = workCoorSys.GetMatrixAngle();
                    _stateMachine.Fire(Trigger.Next);
                })                
                .Permit(Trigger.Next, State.Working)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Working)
                .OnEntryAsync(async () => {
                    _entityPreparator.SetEntityAngle(- _pazAngle - _matrixAngle);
                    var process = new LaserProcess(_wafer, _jsonPierce, _laserMachine, workCoorSys,
                    _zeroZPiercing, _waferThickness, _entityPreparator);
                    process.CreateProcess();
                    await process.StartAsync(ctSource.Token);
                })
                .Ignore(Trigger.Next)
                .Ignore(Trigger.Deny)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Denied)
                .OnEntryAsync(async () =>
                {
                    _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged;
                    ctSource.Cancel();
                    _infoMessager.RealeaseMessage("Процесс отменён", ViewModels.Icon.Exclamation);
                });
        }

        public event EventHandler<bool> SwitchCamera;

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
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task Deny() => await _stateMachine.FireAsync(Trigger.Deny);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
    }
}
