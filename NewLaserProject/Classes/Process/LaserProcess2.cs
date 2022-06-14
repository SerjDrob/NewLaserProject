using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Classes.ProgBlocks;
using Stateless;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class LaserProcess2/*<T> where T : class, IShape*/
    {
        //TODO make it non generic class, LaserWafer<T> is just IEnumerable<IProcObject>

        //private readonly LaserWafer<T> _wafer;
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private PierceParams _pierceParams;
        private BTBuilderY _pierceActionBuilder;
        private readonly double _zPiercing;
        private readonly double _curveAngle;
        private readonly double _waferThickness;

        public LaserProcess2()
        {

        }

        public LaserProcess2(/*LaserWafer<T> */IEnumerable<IProcObject> wafer, string jsonPierce, LaserMachine laserMachine, 
            ICoorSystem<LMPlace> coorSystem, double zPiercing, double waferThickness, double curveAngle = 0)
        {
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zPiercing = zPiercing;
            _curveAngle = curveAngle;
            _waferThickness = waferThickness;
        }


        public void CreateProcess()
        {
            double[] position = { 0, 0 };
            
            _pierceActionBuilder = new BTBuilderY(_jsonPierce);
            var waferEnumerator = _pierceActionBuilder.MainLoopShuffle ? _wafer.Shuffle().GetEnumerator() 
                : _wafer.GetEnumerator();
            
            _pierceActionBuilder
                .SetModuleAction(typeof(TapperBlock), new FuncProxy<Action<double>>(tapper => { _pierceParams = new PierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
                .SetModuleAction(typeof(AddZBlock), new FuncProxy<Action<double>>(z => Task.Run(async () => { await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true); })))
 //TODO fix it!!!
                //               .SetModuleAction(typeof(PierceBlock), new FuncProxy<Action<MarkLaserParams>>(mlp => Pierce(mlp, waferEnumerator.Current)))
                .SetModuleAction(typeof(DelayBlock), new FuncProxy<Action<int>>(delay => Task.Run(async () => { await Task.Delay(delay); })));

            var pierceAction = _pierceActionBuilder.GetTree().GetAction();

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
                    position = _coorSystem.ToGlobal(procObject.X, procObject.Y);
                })            
                .OnEntryAsync(() => Task.WhenAll(
                    _laserMachine.MoveGpInPosAsync(Groups.XY, position, true), 
                    _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness)
                    ))
                //.OnEntryAsync(()=>Task.Delay(1000))
                .OnEntry(pierceAction)
                .OnEntry(() => { _inProcess = waferEnumerator.MoveNext(); })
                .PermitReentryIf(Trigger.Next, () => _inProcess)
                .Ignore(Trigger.Pause);


            _stateMachine.Activate();



            void Pierce<TObj>(MarkLaserParams markLaserParams, IProcObject<TObj> procObject) where TObj : class, IShape
            {
                IParamsAdapting paramsAdapter = procObject switch
                {
                    PCircle => new CircleParamsAdapter(_pierceParams),
                    PCurve or PDxfCurve or PDxfCurve2=> new CurveParamsAdapter(_pierceParams),
                    _ => throw new ArgumentException($"{nameof(waferEnumerator.Current)} matches isn't found")
                };
                var perforator = procObject switch
                {
                    PCurve or PDxfCurve or PDxfCurve2 => new PerforatorFactory<TObj>(procObject, markLaserParams, paramsAdapter).GetPerforator(_curveAngle),
                    PCircle => new PerforatorFactory<TObj>(procObject, markLaserParams, paramsAdapter).GetPerforator()
                };
                _laserMachine.PierceObjectAsync(perforator).Wait();
            }
        }

        public async Task StartAsync()
        {            
            for (int i = 0; i < _pierceActionBuilder.MainLoopCount; i++)
            {
                CreateProcess();
                _inProcess = true;

                while (_inProcess)
                {
                    await _stateMachine.FireAsync(Trigger.Next);
                } 
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

    public class LaserProcess3
    {
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private PierceParams _pierceParams;
        private BTBuilderZ _pierceActionBuilder;
        private readonly double _zPiercing;
        private readonly double _waferThickness;
        private readonly EntityPreparator _entityPreparator;

        public LaserProcess3()
        {

        }

        public LaserProcess3(IEnumerable<IProcObject> wafer, string jsonPierce, LaserMachine laserMachine,
            ICoorSystem<LMPlace> coorSystem, double zPiercing, double waferThickness, EntityPreparator entityPreparator)
        {
            _wafer = wafer;
            _jsonPierce = jsonPierce;
            _laserMachine = laserMachine;
            _coorSystem = coorSystem;
            _zPiercing = zPiercing;
            _waferThickness = waferThickness;
            _entityPreparator = entityPreparator;
        }


        public void CreateProcess()
        {
            double[] position = { 0, 0 };

            _pierceActionBuilder = new BTBuilderZ(_jsonPierce);
            var waferEnumerator = _pierceActionBuilder.MainLoopShuffle ? _wafer.Shuffle().GetEnumerator()
                : _wafer.GetEnumerator();

            _pierceActionBuilder
                .SetModuleFunction<TapperBlock>(new FuncProxy2<double>(tapper => { _pierceParams = new PierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
                .SetModuleFunction<AddZBlock>(new FuncProxy2<double>(async z => { await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true); }))
                .SetModuleFunction<PierceBlock>(new FuncProxy2<ExtendedParams>(async mlp => { await Pierce(mlp, waferEnumerator.Current); }))
                .SetModuleFunction<DelayBlock>(new FuncProxy2<int>(Task.Delay));

            var pierceFunction = _pierceActionBuilder
                .GetTree()
                .GetFunc();

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
                    position = _coorSystem.ToGlobal(procObject.X, procObject.Y);
                })
                .OnEntryAsync(() => Task.WhenAll(
                    _laserMachine.MoveGpInPosAsync(Groups.XY, position, true),
                    _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing)
                    ))
                //.OnEntryAsync(()=>Task.Delay(1000))
                .OnEntryAsync(pierceFunction)
                .OnEntry(() => { _inProcess = waferEnumerator.MoveNext(); })
                .PermitReentryIf(Trigger.Next, () => _inProcess)
                .Ignore(Trigger.Pause);


            _stateMachine.Activate();



            async Task Pierce(ExtendedParams markLaserParams, IProcObject procObject)
            {
                using (var fileHandler = _entityPreparator.GetPreparedEntityDxfHandler(procObject, 100, markLaserParams.HatchWidth))
                {
                    var result = await _laserMachine.PierceDxfObjectAsync(fileHandler.FilePath);
                }
            }
        }

        public async Task StartAsync()
        {
            for (int i = 0; i < _pierceActionBuilder.MainLoopCount; i++)
            {
                CreateProcess();
                _inProcess = true;

                while (_inProcess)
                {
                    await _stateMachine.FireAsync(Trigger.Next);
                }
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
