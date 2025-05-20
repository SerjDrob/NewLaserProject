using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Diagnostics;
using Stateless;

namespace NewLaserProject.Classes
{
    public class CameraOffsetTeacher : ITeacher
    {
        private StateMachine<MyState, MyTrigger> _stateMachine;
        private (bool init, double dx, double dy) _newOffset = (false, 0, 0);

        public event EventHandler TeachingCompleted;

        public static CameraBiasTeacherBuilder GetBuilder()
        {
            return new CameraBiasTeacherBuilder();
        }
        private CameraOffsetTeacher()
        {

        }
        private CameraOffsetTeacher(Func<Task> GoLoadPoint, Func<Task> GoUnderCamera, Func<Task> GoToSoot,
            Func<Task> OnBiasTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart, Func<Task> GiveResult, Func<Task> SearchScorch)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnActivateAsync(RequestPermissionToStart)
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
               .Permit(MyTrigger.Next, MyState.GoShot)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.GoShot)
               .OnEntryAsync(GoToSoot, "Go under the laser, shoot and back under the camera")
               .Permit(MyTrigger.Accept, MyState.AfterShot)
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.AfterShot)
               .OnEntryAsync(SearchScorch)
               .Permit(MyTrigger.Next, MyState.RequestPermission)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.RequestPermission)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(async () => { await OnBiasTought.Invoke(); TeachingCompleted?.Invoke(this, EventArgs.Empty); })
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.HasResult)
                .OnEntryAsync(async () => { await GiveResult.Invoke(); TeachingCompleted?.Invoke(this, EventArgs.Empty); })
                .Ignore(MyTrigger.Next)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

        }


        public override string ToString()
        {
            return $"dx: {_newOffset.dx.ToString("0.###")}, dy: {_newOffset.dy.ToString("0.###")}";
        }
        public async Task NextAsync() => await _stateMachine.FireAsync(MyTrigger.Next);
        public async Task AcceptAsync() => await _stateMachine.FireAsync(MyTrigger.Accept);
        public async Task DenyAsync() => await _stateMachine.FireAsync(MyTrigger.Deny);

        public void SetParams(params double[] ps)
        {
            Guard.HasSizeEqualTo(ps, 2, nameof(ps));
            _newOffset = _newOffset.init ? (false, _newOffset.dx - ps[0], _newOffset.dy - ps[1]) : (true, ps[0], ps[1]);
        }
        //public (double dx, double dy) GetOffset() => (_newOffset.dx,_newOffset.dy);
        public double[] GetParams() => [_newOffset.dx, _newOffset.dy];

        public async Task StartTeachAsync()
        {
            await _stateMachine.ActivateAsync();
        }

        public void SetResult(double result)
        {
            throw new NotImplementedException();
        }

        public class CameraBiasTeacherBuilder
        {
            public CameraOffsetTeacher Build()
            {
                Guard.IsNotNull(GoLoadPoint, $"{nameof(GoLoadPoint)} isn't set");
                Guard.IsNotNull(GoUnderCamera, $"{nameof(GoUnderCamera)} isn't set");
                Guard.IsNotNull(GoToSoot, $"{nameof(GoToSoot)} isn't set");
                Guard.IsNotNull(OnBiasTought, $"{nameof(OnBiasTought)} isn't set");
                Guard.IsNotNull(RequestPermissionToAccept, $"{nameof(RequestPermissionToAccept)} isn't set");
                Guard.IsNotNull(RequestPermissionToStart, $"{nameof(RequestPermissionToStart)} isn't set");
                Guard.IsNotNull(HasResult, $"{nameof(HasResult)} isn't set");
                Guard.IsNotNull(SearchScorch, $"{nameof(SearchScorch)} isn't set");
                return new CameraOffsetTeacher(GoLoadPoint, GoUnderCamera, GoToSoot, OnBiasTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult, SearchScorch);
            }
            private Func<Task> GoToSoot;
            private Func<Task> GoUnderCamera;
            private Func<Task> GoLoadPoint;
            private Func<Task> OnBiasTought;
            private Func<Task> RequestPermissionToAccept;
            private Func<Task> RequestPermissionToStart;
            private Func<Task> HasResult;
            private Func<Task> SearchScorch;

            public CameraBiasTeacherBuilder SetOnGoToShotAction(Func<Task> action)
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
            public CameraBiasTeacherBuilder SetOnSearchScorchAction(Func<Task> action)
            {
                SearchScorch = action;
                return this;
            }
        }


        private enum MyState
        {
            Begin,
            AtLoadPoint,
            UnderCamera,
            GoShot,
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
