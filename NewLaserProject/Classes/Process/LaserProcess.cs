using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Laser.Parameters;
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
        private bool _inLoop = false;
        private int _loopCount = 0; 
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
            _progTreeParser = new ProgTreeParser(_jsonPierce);

            var waferEnumerator = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle().GetEnumerator()
                            : _wafer.GetEnumerator();

            _progTreeParser
                //TODO don't pass the taper, it should be calculated value respective the taper and other tech params like specified tolerance
                .SetModuleFunction<TaperBlock, double>(new FuncProxy<double>(taper => _entityPreparator.SetEntityContourOffset(taper)))
                .SetModuleFunction<AddZBlock, double>(new FuncProxy<double>(async z => { await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true); }))
                .SetModuleFunction<PierceBlock, ExtendedParams>(new FuncProxy<ExtendedParams>(async mlp => { await Pierce(mlp, waferEnumerator.Current); }))
                .SetModuleFunction<DelayBlock, int>(new FuncProxy<int>(Task.Delay));

            var pierceFunction = _progTreeParser
                .GetTree()
                .GetFunc();

            _stateMachine = new StateMachine<State, Trigger>(State.Started, FiringMode.Queued);

            _stateMachine.Configure(State.Started)
                .OnActivate(() => { _inLoop = waferEnumerator.MoveNext(); })
                .Ignore(Trigger.Pause)
                .Permit(Trigger.Deny, State.Denied)
                .Permit(Trigger.Next, State.Working);

            _stateMachine.Configure(State.Working)
                .OnEntryAsync(async () => 
                {                    
                    var procObject = waferEnumerator.Current;
                    var position = _coorSystem.ToGlobal(procObject.X, procObject.Y);
                    _laserMachine.SetVelocity(Velocity.Fast);
                    await Task.WhenAll(
                    _laserMachine.MoveGpInPosAsync(Groups.XY, position, true),
                    _laserMachine.MoveAxInPosAsync(Ax.Z, _zPiercing - _waferThickness));
                    await pierceFunction();
                    _inLoop = waferEnumerator.MoveNext();
                })
                .PermitReentryIf(Trigger.Next, () => _inLoop)
                .PermitIf(Trigger.Next, State.Loop,() => !_inLoop)
                .Ignore(Trigger.Pause);

            _stateMachine.Configure(State.Loop)
                .OnEntry(() =>
                {
                    _loopCount++;
                    waferEnumerator = _progTreeParser.MainLoopShuffle ? _wafer.Shuffle().GetEnumerator()
                            : _wafer.GetEnumerator();
                    _inLoop = waferEnumerator.MoveNext();
                })
                .OnExit(() => _inProcess = false)
                .PermitIf(Trigger.Next, State.Working, () => _loopCount < _progTreeParser.MainLoopCount)
                .PermitIf(Trigger.Next, State.Exit, () => _loopCount >= _progTreeParser.MainLoopCount);

            _stateMachine.Configure(State.Exit)
                .OnEntry(() => { })
                .Ignore(Trigger.Next);

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
            if (_stateMachine is null) CreateProcess();

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
            Denied,
            Loop,
            Exit
        }
        enum Trigger
        {
            Next,
            Pause,
            Deny
        }
    }
        
    //public abstract class BaseLaserProcess
    //{
    //    private readonly ProgTreeParser _progTreeParser;
    //    protected Func<Task> pierceFunction;

    //    public BaseLaserProcess(ProgTreeParser progTreeParser)
    //    {
    //        _progTreeParser = progTreeParser;

    //        _progTreeParser
    //            .SetModuleFunction<TapperBlock>(FuncForTapperBlock)
    //            .SetModuleFunction<AddZBlock>(FuncForAddZBlock)
    //            .SetModuleFunction<PierceBlock>(FuncForPierseBlock)
    //            .SetModuleFunction<DelayBlock>(FuncForDelayBlock);

    //        pierceFunction = _progTreeParser
    //           .GetTree()
    //           .GetFunc();
    //    }

    //    protected abstract Task FuncForTapperBlock(double tapper);
    //    protected abstract Task FuncForAddZBlock(double z);
    //    protected abstract Task FuncForPierseBlock(ExtendedParams extendedParams);
    //    protected abstract Task FuncForDelayBlock(int delay);

    //}
}
