using MachineClassLibrary.Classes;
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
        private double _matrixAngle;

        public ThreePointProcess(LaserWafer<T> wafer, IEnumerable<PPoint> refPoints,
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


        public void CreateProcess()
        {
            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);
            var resultPoints = new List<PointF>();
            var refPointsEnumerator = _refPoints.GetEnumerator();
            refPointsEnumerator.MoveNext();
            var refX = 0d;// refPointsEnumerator.Current.X;
            var refY = 0d;// refPointsEnumerator.Current.Y;            



            var originPoints = _refPoints.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();

            CoorSystem<LMPlace> workCoorSys = new();
            _laserMachine.OnAxisMotionStateChanged += _laserMachine_OnAxisMotionStateChanged;

            LaserProcess2<T> process = new();


            _stateMachine.Configure(State.Started)
                .OnActivate(() => _infoMessager.RealeaseMessage("Next", ViewModels.Icon.Info))
                .Permit(Trigger.Next, State.GoRefPoint)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.GoRefPoint)
                .OnEntry(() => _laserMachine.SetVelocity(Velocity.Fast))
                .OnEntry(() => { refX = refPointsEnumerator.Current.X; refY = refPointsEnumerator.Current.Y; })
                .OnEntryAsync(() => Task.WhenAll(_laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToGlobal(refX, refY)),
                                              _laserMachine.MoveAxInPosAsync(Ax.Z, _zeroZCamera - _waferThickness)))
                .OnEntry(() => _infoMessager.RealeaseMessage("Укажите точку и нажмите *", ViewModels.Icon.Info))
                .OnExit(() => resultPoints.Add(new((float)(_xActual + _dX), (float)(_yActual + _dY))))
                .OnExit(() => refPointsEnumerator.MoveNext())
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
                    .FormWorkMatrix(0.001, 0.001, false)
                    .Build())
                .OnEntry(() => _matrixAngle = workCoorSys.GetMatrixAngle())
                .OnEntry(() => _stateMachine.Fire(Trigger.Next))
                .Permit(Trigger.Next, State.Working)
                .Permit(Trigger.Deny, State.Denied)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Working)
                .OnEntry(() => process = new LaserProcess2<T>(_wafer, _jsonPierce, _laserMachine, workCoorSys, _zeroZPiercing, _waferThickness, _pazAngle - _matrixAngle))
                .OnEntry(() => process.CreateProcess())
                .OnEntryAsync(() => process.StartAsync())
                .Ignore(Trigger.Next)
                .Ignore(Trigger.Deny)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Denied)
                .OnEntry(() => _laserMachine.OnAxisMotionStateChanged -= _laserMachine_OnAxisMotionStateChanged)
                .OnEntry(() => _infoMessager.RealeaseMessage("Процесс отменён", ViewModels.Icon.Exclamation));

            //void MoveNext()
            //{
            //    var res = refPointsEnumerator.MoveNext();
            //}
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
