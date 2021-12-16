using MachineClassLibrary.BehaviourTree;
using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class LaserProcess<T> where T : class
    {
        private readonly IEnumerable<IProcObject<T>> _procObjects;
        private readonly string _pierceSequenceJson;
        private readonly LaserMachine _laserMachine;
        private CirclePierceParams _circlePierceParams;
        private Sequence _pierceSequence;
        private Sequence _rootSequence;
        private IProcObject<T> _currentObject;
        private IEnumerator<IProcObject<T>> _enumerator;
        private Block _pauseBlock = new Block().BlockMe();
        private ITeacher _currentTeacher;
        private TeachCommand AcceptCmd;
        private TeachCommand DenyCmd;
        private TeachCommand NextCmd;
        private bool _canTeach = false;

        public LaserProcess(IEnumerable<IProcObject<T>> procObjects, string jsonPierce, LaserMachine laserMachine)
        {
            this._procObjects = procObjects;
            this._pierceSequenceJson = jsonPierce;
            _laserMachine = laserMachine;
            _enumerator = _procObjects.GetEnumerator();
            AcceptCmd = new TeachCommand(_currentTeacher.Accept(), () => _canTeach);
            DenyCmd = new TeachCommand(_currentTeacher.Deny(), () => _canTeach);
            NextCmd = new TeachCommand(_currentTeacher.Next(), () => _canTeach);

        }
        public void Proc()
        {

            var iteratorBlock = new Block();

            var btb = new BTBuilder(_pierceSequenceJson);

            var pTicker = new Ticker();

            _pierceSequence = btb.SetModuleAction(ModuleType.AddDiameter, new FuncProxy<Action<double>>(tapper => { _circlePierceParams = new CirclePierceParams(tapper, 0.5, 0, 0, Material.Polycor); }))
                                 .SetModuleAction(ModuleType.AddZ, new FuncProxy<Action<double>>(z => _laserMachine.MoveAxRelativeAsync(Ax.Z, z, true)))
                                 .SetModuleAction(ModuleType.Pierce, new FuncProxy<Action<MarkLaserParams>>(mlp => Pierce(mlp)))
                                 .SetModuleAction(ModuleType.Delay, new FuncProxy<Action<int>>(delay => Task.Delay(delay).Wait()))
                                 .GetSequence();

            //move'n'pierce sequence
            var mpSequence = new Sequence()
                                .Hire(new Leaf(() => { _laserMachine.MoveGpInPosAsync(MachineClassLibrary.Machine.Groups.XY, new double[] { _currentObject.X, _currentObject.Y }, true); }))
                                .Hire(_pierceSequence)
                                .Hire(new Leaf(() => { }).WaitForMe().SetBlock(_pauseBlock));

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
                                .Hire(new Leaf(() => _laserMachine.GoThereAsync(LMPlace.Loading)));
        }
        private void Pierce(MarkLaserParams markLaserParams)
        {
            var paramsAdapter = _currentObject switch
            {
                PCircle => new CircleParamsAdapter(_circlePierceParams)
            };
            var perfBuilder = new PerforatorBuilder<T>(_currentObject, markLaserParams, paramsAdapter);
            _laserMachine.PierceObjectAsync(perfBuilder).Wait();
        }
        public async Task<bool> Start()
        {
            return await _rootSequence.DoWork();
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

        public async Task TeacherAcceptAsync()
        {
            if (AcceptCmd.CanExecute()) await AcceptCmd.Execute();
        }
        public async Task TeacherDenyAsync()
        {
            if (DenyCmd.CanExecute()) await DenyCmd.Execute();
        }
        public async Task TeacherNext()
        {
            if (NextCmd.CanExecute()) await NextCmd.Execute();
        }

        public void SetCurrentTeacher(Learning learning)
        {
            switch (learning)
            {
                case Learning.LaserOffset:
                    {
                        var teachPosition = new double[] { 1, 1 };
                        double xOffset = 1;
                        double yOffset = 1;


                        var tcb = TeachCameraBias.GetBuilder();
                        tcb.SetOnGoLoadPointAction(() => _laserMachine.GoThereAsync(LMPlace.Loading))
                            .SetOnGoUnderCameraAction(() => _laserMachine.MoveGpInPosAsync(Groups.XY, teachPosition))
                            .SetOnGoToSootAction(() => Task.Run(async () =>
                            {
                                _laserMachine.MoveAxRelativeAsync(Ax.X, xOffset, true);
                                _laserMachine.MoveAxRelativeAsync(Ax.Y, yOffset, true);
                                await _laserMachine.PiercePointAsync();
                                _laserMachine.MoveAxRelativeAsync(Ax.X, -xOffset, true);
                                _laserMachine.MoveAxRelativeAsync(Ax.Y, -yOffset, true);
                            }));
                        _currentTeacher = tcb.Build();
                        _canTeach = true;
                    }
                    break;
                case Learning.Orthogonality:
                    break;
                case Learning.ScannatorIncline:
                    break;
                default:
                    break;
            }


        }
        public enum Learning
        {
            LaserOffset,
            Orthogonality,
            ScannatorIncline
        }
    }
}
