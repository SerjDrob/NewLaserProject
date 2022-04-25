using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Classes.ProgBlocks;
using Stateless;
using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class LaserProcess2<T> where T : class, IShape
    {

        private readonly LaserWafer<T> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly CoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private PierceParams _pierceParams;

        public LaserProcess2(LaserWafer<T> wafer, string jsonPierce, LaserMachine laserMachine, CoorSystem<LMPlace> coorSystem)
        {
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
        }


        public void CreateProcess()
        {
            double[] position = { 0, 0 };
            var waferEnumerator = _wafer.GetEnumerator();
            var pierceAction = new BTBuilderY(_jsonPierce)
                .SetModuleAction(typeof(TapperBlock), new FuncProxy<Action<double>>(tapper => { _pierceParams = new PierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
                .SetModuleAction(typeof(AddZBlock), new FuncProxy<Action<double>>(z => Task.Run(async () => { await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true); })))
                .SetModuleAction(typeof(PierceBlock), new FuncProxy<Action<MarkLaserParams>>(mlp => Pierce(mlp, waferEnumerator.Current)))
                .SetModuleAction(typeof(DelayBlock), new FuncProxy<Action<int>>(delay => Task.Run(async () => { await Task.Delay(delay); })))
                .GetTree()
                .GetAction();

            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);

            _stateMachine.Configure(State.Started)
                .OnActivate(() => { _inProcess = waferEnumerator.MoveNext(); })
                .Ignore(Trigger.Pause)
                .Permit(Trigger.Deny, State.Denied)
                .Permit(Trigger.Next, State.Working);

            _stateMachine.Configure(State.Working)
                .OnEntry(() =>
                {
                    var procObject = waferEnumerator.Current;
                    position = _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, procObject.X, procObject.Y);
                })
                .OnEntryAsync(() => _laserMachine.MoveGpInPosAsync(Groups.XY, position, true))                
                .OnEntry(pierceAction)
                .OnEntryAsync(() => Task.Delay(1000))
                .OnEntry(() => { _inProcess = waferEnumerator.MoveNext(); })
                .PermitReentryIf(Trigger.Next, () => _inProcess)
                .Ignore(Trigger.Pause);


            _stateMachine.Activate();



            void Pierce<TObj>(MarkLaserParams markLaserParams, IProcObject<TObj> procObject) where TObj : class, IShape
            {
                IParamsAdapting paramsAdapter = procObject switch
                {
                    PCircle => new CircleParamsAdapter(_pierceParams),
                    PCurve => new CurveParamsAdapter(_pierceParams),
                    _ => throw new ArgumentException($"{nameof(waferEnumerator.Current)} matches isn't found")
                };
                var perfBuilder = new PerforatorBuilder<TObj>(procObject, markLaserParams, paramsAdapter);

                _laserMachine.PierceObjectAsync(perfBuilder).Wait();
            }
        }

        public async Task StartAsync()
        {
            CreateProcess();
            _inProcess = true;

            while (_inProcess)
            {
                await _stateMachine.FireAsync(Trigger.Next);
            }

        }



        enum State
        {
            Started,
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
