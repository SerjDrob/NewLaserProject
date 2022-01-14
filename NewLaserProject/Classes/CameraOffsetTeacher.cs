using Microsoft.Toolkit.Diagnostics;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class CameraOffsetTeacher : ITeacher
    {
        private StateMachine<MyState, MyTrigger> _stateMachine;
        private (bool init, double dx, double dy) _newOffset = (false, 0, 0);

        public static CameraBiasTeacherBuilder GetBuilder()
        {
            return new CameraBiasTeacherBuilder();
        }
        private CameraOffsetTeacher()
        {

        }
        private CameraOffsetTeacher(Func<Task> GoLoadPoint, Func<Task> GoUnderCamera, Func<Task> GoToSoot,
            Func<Task> OnBiasTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart, Func<Task> GiveResult)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnEntryAsync(RequestPermissionToStart)
                .Permit(MyTrigger.Accept, MyState.AtLoadPoint)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.AtLoadPoint)
                .OnEntryAsync(GoLoadPoint)
                .Permit(MyTrigger.Next, MyState.UnderCamera)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.UnderCamera)
               .OnEntryAsync(GoUnderCamera)
               .Permit(MyTrigger.Next, MyState.AfterShot)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.AfterShot)
               .OnEntryAsync(GoToSoot, "Go under the laser, shoot and back under the camera")
               .Permit(MyTrigger.Next, MyState.RequestPermission)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.RequestPermission)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(OnBiasTought)
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.HasResult)
                .OnEntryAsync(GiveResult)
                .Ignore(MyTrigger.Next)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

            _stateMachine.Activate();

        }

        public override string ToString()
        {
            return $"dx: {_newOffset.dx}, dy: {_newOffset.dy}";
        }
        public async Task Next() => await _stateMachine.FireAsync(MyTrigger.Next);
        public async Task Accept() => await _stateMachine.FireAsync(MyTrigger.Accept);
        public async Task Deny() => await _stateMachine.FireAsync(MyTrigger.Deny);

        public void SetParams(params double[] ps)
        {
            Guard.HasSizeEqualTo(ps, 2, nameof(ps));
            _newOffset = _newOffset.init ? (false, _newOffset.dx - ps[0], _newOffset.dy - ps[1]) : (true, ps[0], ps[1]);
        }
        //public (double dx, double dy) GetOffset() => (_newOffset.dx,_newOffset.dy);

        public double[] GetParams()
        {
            return new double[] { _newOffset.dx, _newOffset.dy };
        }

        public class CameraBiasTeacherBuilder
        {
            public CameraOffsetTeacher Build()
            {
                Guard.IsNotNull(GoToSoot, $"{nameof(GoToSoot)} isn't set");
                Guard.IsNotNull(GoUnderCamera, $"{nameof(GoUnderCamera)} isn't set");
                Guard.IsNotNull(GoUnderCamera, $"{nameof(GoUnderCamera)} isn't set");
                return new CameraOffsetTeacher(GoLoadPoint, GoUnderCamera, GoToSoot, OnBiasTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult);
            }
            private Func<Task> GoToSoot;
            private Func<Task> GoUnderCamera;
            private Func<Task> GoLoadPoint;
            private Func<Task> OnBiasTought;
            private Func<Task> RequestPermissionToAccept;
            private Func<Task> RequestPermissionToStart;
            private Func<Task> HasResult;

            public CameraBiasTeacherBuilder SetOnGoToSootAction(Func<Task> action)
            {
                GoToSoot = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnGoUnderCameraAction(Func<Task> action)
            {
                GoUnderCamera = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnGoLoadPointAction(Func<Task> action)
            {
                GoLoadPoint = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnBiasToughtAction(Func<Task> action)
            {
                OnBiasTought = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnRequestPermissionToAcceptAction(Func<Task> action)
            {
                RequestPermissionToAccept = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnRequestPermissionToStartAction(Func<Task> action)
            {
                RequestPermissionToStart = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnHasResultAction(Func<Task> action)
            {
                HasResult = action;
                return this;
            }
        }


        private enum MyState
        {
            Begin,
            AtLoadPoint,
            UnderCamera,
            AfterShot,
            RequestPermission,
            End,
            HasResult
        }
        private enum MyTrigger
        {
            Next,
            Accept,
            Deny
        }
    }
}
