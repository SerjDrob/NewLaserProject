using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using NewLaserProject.Classes.Geometry;
using NewLaserProject.Classes.ProgBlocks;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class LaserProcess : IProcess
    {
        private readonly IEnumerable<IProcObject> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly ICoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private PierceParams _pierceParams;
        private ProgTreeParser _progTreeParser;
        private readonly double _zPiercing;
        private readonly double _waferThickness;
        private readonly EntityPreparator _entityPreparator;

        
        public LaserProcess(IEnumerable<IProcObject> wafer, string jsonPierce, LaserMachine laserMachine,
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
            
            _progTreeParser = new ProgTreeParser(_jsonPierce);

            var waferEnumerator = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle().GetEnumerator()
                            : _wafer.GetEnumerator();

            _progTreeParser
                .SetModuleFunction<TapperBlock, double>(new FuncProxy<double>(tapper => { _pierceParams = new PierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
                .SetModuleFunction<AddZBlock, double>(new FuncProxy<double>(async z => { await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true); }))
                .SetModuleFunction<PierceBlock, ExtendedParams>(new FuncProxy<ExtendedParams>(async mlp => { await Pierce(mlp, waferEnumerator.Current); }))
                .SetModuleFunction<DelayBlock, int>(new FuncProxy<int>(Task.Delay));

            var pierceFunction = _progTreeParser
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
                    _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness)
                    ))
                .OnEntryAsync(pierceFunction)
                .OnEntry(() => { _inProcess = waferEnumerator.MoveNext(); })
                .PermitReentryIf(Trigger.Next, () => _inProcess)
                .Ignore(Trigger.Pause);


            _stateMachine.Activate();



            async Task Pierce(ExtendedParams markLaserParams, IProcObject procObject)
            {
                using (var fileHandler = _entityPreparator.GetPreparedEntityDxfHandler(procObject))
                {
                    _laserMachine.SetExtMarkParams(new ExtParamsAdapter(markLaserParams));
                    var result = await _laserMachine.PierceDxfObjectAsync(fileHandler.FilePath);
                }
            }
        }

        public async Task StartAsync()
        {
            CreateProcess();

            for (int i = 0; i < _progTreeParser.MainLoopCount; i++)
            {
                _inProcess = true;

                while (_inProcess)
                {
                    await _stateMachine.FireAsync(Trigger.Next);
                }
            }
        }

        public override string ToString()
        {
            return UmlDotGraph.Format(_stateMachine.GetInfo());
        }

        public Task Deny()
        {
            throw new NotImplementedException();
        }

        public Task Next()
        {
            throw new NotImplementedException();
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
