using MachineClassLibrary.BehaviourTree;
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
using System.Diagnostics;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class LaserProcess<T> where T : class
    {
        private readonly string _pierceSequenceJson;
        private readonly LaserMachine _laserMachine;
        private readonly CoorSystem<LMPlace> _coorSystem;
        private CirclePierceParams _circlePierceParams;
        private Sequence _pierceSequence;
        private Sequence _rootSequence;
        private IProcObject<T> _currentObject;
        private IEnumerator<IProcObject<T>> _enumerator;
        private Block _pauseBlock = new Block().BlockMe();


        public LaserProcess(LaserWafer<T> wafer, string jsonPierce, LaserMachine laserMachine, CoorSystem<LMPlace> coorSystem)
        {
            this._pierceSequenceJson = jsonPierce;
            _laserMachine = laserMachine;
            _enumerator = wafer.GetEnumerator();
            _coorSystem = coorSystem;
        }
        private void CreateProcess()
        {

            var iteratorBlock = new Block();

            var btb = new BTBuilderX(_pierceSequenceJson);

            var pTicker = new Ticker();

            //_pierceSequence = btb.SetModuleAction(ModuleType.AddDiameter, new FuncProxy<Action<double>>(tapper => { _circlePierceParams = new CirclePierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
            //                     .SetModuleAction(ModuleType.AddZ, new FuncProxy<Action<double>>(z => _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true)))
            //                     .SetModuleAction(ModuleType.Pierce, new FuncProxy<Action<MarkLaserParams>>(mlp => Pierce(mlp)))
            //                     .SetModuleAction(ModuleType.Delay, new FuncProxy<Action<int>>(delay => Task.Delay(delay).Wait()))
            //                     .GetSequence();

            _pierceSequence = btb.SetModuleAction(typeof(TapperBlock), new FuncProxy<Action<double>>(tapper => { _circlePierceParams = new CirclePierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
                                 .SetModuleAction(typeof(AddZBlock), new FuncProxy<Action<double>>(z => Task.Run(async () => { await _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true); })))
                                 .SetModuleAction(typeof(PierceBlock), new FuncProxy<Action<MarkLaserParams>>(mlp => Pierce(mlp)))
                                 .SetModuleAction(typeof(DelayBlock), new FuncProxy<Action<int>>(delay => Task.Run(async () => { await Task.Delay(delay); })))
                                 .GetSequence();

            //move'n'pierce sequence
            var mpSequence = new Sequence()
                                .Hire(new Leaf(() => Task.Run(async () => await GoCurrentPoint())))
                                //.Hire(_pierceSequence)
                                .Hire(new Leaf(() => new Task(() => { })).WaitForMe().SetBlock(_pauseBlock));

            var mpTicker = new Ticker()
                               .SetActionBeforeWork(() =>
                               {
                                   if (_enumerator.MoveNext())
                                   {
                                       _currentObject = _enumerator.Current;
                                       iteratorBlock.UnBlockMe();
                                   }
                                   else iteratorBlock.BlockMe();
                               })
                               .Hire(mpSequence);

            _rootSequence = new Sequence()
                                .Hire(mpTicker)
                                .Hire(new Leaf(/*_laserMachine.GoThereAsync(LMPlace.Loading)*/() => new Task(() => { })));
        }


        private Task GoCurrentPoint()
        {
            if (_currentObject is null)
            {
                //return Task.CompletedTask;
            }
            try
            {
                Debug.WriteLine($"I'm going to point({_currentObject.X},{_currentObject.Y})");
            }
            catch (Exception ex)
            {

                throw;
            }
            return new Task(() => { });
            // await _laserMachine.MoveGpInPosAsync(Groups.XY, _coorSystem.ToSub(LMPlace.FileOnWaferUnderCamera, _currentObject.X, _currentObject.Y), true);
        }



        private void Pierce(MarkLaserParams markLaserParams)
        {
            _circlePierceParams = new CirclePierceParams(0.1, 1, 0.05, 0.05, Material.Polycor);
            var paramsAdapter = _currentObject switch
            {
                PCircle => new CircleParamsAdapter(_circlePierceParams),
                _ => throw new ArgumentException($"{nameof(_currentObject)} matches isn't found")
            };
            var perfBuilder = new PerforatorBuilder<T>(_currentObject, markLaserParams, paramsAdapter);
            _laserMachine.PierceObjectAsync(perfBuilder).Wait();
        }
        public async Task<bool> Start()
        {
            CreateProcess();
            Debug.Write(_rootSequence);
            return await _rootSequence.DoWorkAsync();
        }
        public void Pause() => _pauseBlock.UnBlockMe();
        public void Resume()
        {
            _pauseBlock.BlockMe();
            _rootSequence.PulseAction(false);
        }
        public void Stop()
        {
            _rootSequence.CancellAction(true);
        }

    }


    public class LaserProcess2<T> where T : class
    {

        private readonly LaserWafer<T> _wafer;
        private readonly string _jsonPierce;
        private readonly LaserMachine _laserMachine;
        private readonly CoorSystem<LMPlace> _coorSystem;
        private StateMachine<State, Trigger> _stateMachine;
        private bool _inProcess = false;
        private CirclePierceParams _circlePierceParams;

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
                .SetModuleAction(typeof(TapperBlock), new FuncProxy<Action<double>>(tapper => { _circlePierceParams = new CirclePierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
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



            void Pierce<TObj>(MarkLaserParams markLaserParams, IProcObject<TObj> procObject) where TObj : class
            {
                //_circlePierceParams = new CirclePierceParams(0.1, 1, 0.05, 0.05, Material.Polycor);
                var paramsAdapter = procObject switch
                {
                    PCircle => new CircleParamsAdapter(_circlePierceParams),
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
