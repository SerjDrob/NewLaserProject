using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using Microsoft.Toolkit.Diagnostics;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.ViewModels;
using Stateless;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.Process
{
    internal class ThreePointProcess<T> where T : class, IShape
    {
        private readonly LaserWafer<T> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private PierceParams _pierceParams;
        private readonly IEnumerable<PPoint> _refPoints;
        private readonly double _zPiercing;
        private readonly double _zCamera;
        private readonly InfoMessager _infoMessager;
        private double _xActual;
        private double _yActual;

        public ThreePointProcess(LaserWafer<T> wafer, IEnumerable<PPoint> refPoints,
            string jsonPierce, LaserMachine laserMachine, ICoorSystem<LMPlace> coorSystem,
            double zPiercing, double zCamera, InfoMessager infoMessager)
        {
            Guard.IsEqualTo(refPoints.Count(), 3, nameof(refPoints));
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zPiercing = zPiercing;
            _refPoints = refPoints;
            _zCamera = zCamera;
            _infoMessager = infoMessager;
        }


        public void CreateProcess()
        {
            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);
            var resultPoints = new List<PointF>();
            var refPointsEnumerator = _refPoints.GetEnumerator();
            refPointsEnumerator.MoveNext();
            var refX = refPointsEnumerator.Current.X;
            var refY = refPointsEnumerator.Current.Y;

            var originPoints = _refPoints.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();

            CoorSystem<LMPlace> workCoorSys = new();
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;

            LaserProcess2<T> process = null;


            _stateMachine.Configure(State.Started)
                .Permit(Trigger.Next, State.GoRefPoint)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GoRefPoint)
                .OnEntry(() => _laserMachine.SetVelocity(Velocity.Fast))
                .OnEntryAsync(() => Task.WhenAll(_laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(refX, refY)),
                                              _laserMachine.MoveAxInPosAsync(Ax.Z, _zCamera)))
                .OnEntry(() => _infoMessager.RealeaseMessage("Укажите точку и нажмите *", ViewModels.Icon.Info))
                .OnExit(() => resultPoints.Add(new((float)_xActual, (float)_yActual)))
                .PermitReentryIf(Trigger.Next, () => resultPoints.Count < 2)
                .PermitIf(Trigger.Next, State.GetRefPoint, () => resultPoints.Count == 2)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GetRefPoint)
                .OnEntry(_infoMessager.EraseMessage)
                .OnEntry(() => _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged)
                .OnEntry(() => workCoorSys = new CoorSystem<LMPlace>
                    .ThreePointCoorSystemBuilder<LMPlace>()
                    .SetFirstPointPair(originPoints[0], resultPoints[0])
                    .SetSecondPointPair(originPoints[1], resultPoints[1])
                    .SetThirdPointPair(originPoints[2], resultPoints[2])
                    .Build())
                .OnEntry(()=>_stateMachine.Fire(Trigger.Next))
                .Permit(Trigger.Next, State.Working)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Working)
                .OnEntry(() => process = new LaserProcess2<T>(_wafer, _jsonPierce, _laserMachine, workCoorSys, _zPiercing))
                .OnEntry(process.CreateProcess)
                .OnEntryAsync(process.StartAsync)
                .Ignore(Trigger.Next)
                .Ignore(Trigger.Deny)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Denied)
                .OnEntry(() => _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged)
                .OnEntry(()=>_infoMessager.RealeaseMessage("Процесс отменён", ViewModels.Icon.Exclamation));
            
            
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
