using MachineClassLibrary.Classes;
using MachineClassLibrary.Machine;
using MachineClassLibrary.Machine.Machines;
using NewLaserProject.Classes.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    internal class TeachLaserMachine
    {
        private ITeacher _currentTeacher;
        private TeachCommand AcceptCmd;
        private TeachCommand DenyCmd;
        private TeachCommand NextCmd;
        private readonly LaserMachine _laserMachine;
        private readonly Learning _subject;
        private bool _canTeach = false;
        //IEnumerable
        //CoorSystem
        public TeachLaserMachine(LaserMachine laserMachine, Learning subject)
        {
            AcceptCmd = new TeachCommand(_currentTeacher.Accept(), () => _canTeach);
            DenyCmd = new TeachCommand(_currentTeacher.Deny(), () => _canTeach);
            NextCmd = new TeachCommand(_currentTeacher.Next(), () => _canTeach);
            _laserMachine = laserMachine;
            _subject = subject;
            _currentTeacher = SelectCurrentTeacher();
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

        private ITeacher SelectCurrentTeacher()
        {
            switch (_subject)
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
                        _canTeach = true;
                        return tcb.Build();
                    }
                    break;
                case Learning.Orthogonality:
                    throw new NotImplementedException();
                case Learning.ScannatorIncline:
                    throw new NotImplementedException();
                default:
                    throw new KeyNotFoundException();
            }
        }

        public void GetResult(ref object result)
        {

        }

        public enum Learning
        {
            LaserOffset,
            Orthogonality,
            ScannatorIncline
        }
    }
}
