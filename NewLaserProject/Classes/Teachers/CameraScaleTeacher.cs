using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Machine.Machines;
using Microsoft.Toolkit.Diagnostics;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{   

    internal class CameraScaleTeacher : ITeacher
    {
        private StateMachine<MyState, MyTrigger> _stateMachine;

        public static CameraScaleTeacherBuilder GetBuilder()
        {
            return new CameraScaleTeacherBuilder();
        }
        private CameraScaleTeacher()
        {

        }
        private CameraScaleTeacher(Func<Task> GoLoadPoint, Func<Task> GoNAskFirstMarker, Func<Task> AskSecondMarker,
            Func<Task> OnScaleTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart, Func<Task> GiveResult)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnActivateAsync(RequestPermissionToStart)
                .Permit(MyTrigger.Accept, MyState.AtLoadPoint)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);


            _stateMachine.Configure(MyState.AtLoadPoint)
                .OnEntryAsync(GoLoadPoint)
                .Permit(MyTrigger.Next, MyState.UnderFirstMarker)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.UnderFirstMarker)
               .OnEntryAsync(GoNAskFirstMarker)
               .Permit(MyTrigger.Next, MyState.UnderSecondMarker)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.UnderSecondMarker)
               .OnEntryAsync(AskSecondMarker)
               .Permit(MyTrigger.Next, MyState.RequestPermission)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.RequestPermission)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(async () => { await OnScaleTought.Invoke(); TeachingCompleted?.Invoke(this, EventArgs.Empty); })
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.HasResult)
                .OnEntryAsync(async ()=> { await GiveResult.Invoke(); TeachingCompleted?.Invoke(this, EventArgs.Empty); })
                .Ignore(MyTrigger.Next)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

        }



        public async Task NextAsync() => await _stateMachine.FireAsync(MyTrigger.Next);
        public async Task AcceptAsync() => await _stateMachine.FireAsync(MyTrigger.Accept);
        public async Task DenyAsync() => await _stateMachine.FireAsync(MyTrigger.Deny);
        private double _firstMarkerYNScale;

        public event EventHandler TeachingCompleted;

        public void SetParams(params double[] ps)
        {
            Guard.HasSizeEqualTo(ps, 1, nameof(ps));
            _firstMarkerYNScale = ps[0];
        }

        public double[] GetParams()
        {
            return new double[] { _firstMarkerYNScale };
        }

        public async Task StartTeachAsync()
        {
            await _stateMachine.ActivateAsync();
        }

        public void SetResult(double result)
        {
            throw new NotImplementedException();
        }

        public class CameraScaleTeacherBuilder
        {
            public CameraScaleTeacher Build()
            {
                Guard.IsNotNull(GoLoadPoint, $"{nameof(GoLoadPoint)} isn't set");
                Guard.IsNotNull(GoNAskFirstMarker, $"{nameof(GoNAskFirstMarker)} isn't set");
                Guard.IsNotNull(AskSecondMarker, $"{nameof(AskSecondMarker)} isn't set");
                Guard.IsNotNull(OnScaleTought, $"{nameof(OnScaleTought)} isn't set");
                Guard.IsNotNull(RequestPermissionToAccept, $"{nameof(RequestPermissionToAccept)} isn't set");
                Guard.IsNotNull(RequestPermissionToStart, $"{nameof(RequestPermissionToStart)} isn't set");
                Guard.IsNotNull(HasResult, $"{nameof(HasResult)} isn't set");
                return new CameraScaleTeacher(GoLoadPoint, GoNAskFirstMarker, AskSecondMarker, OnScaleTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult);
            }
            private Func<Task> GoNAskFirstMarker;
            private Func<Task> AskSecondMarker;
            private Func<Task> GoLoadPoint;
            private Func<Task> OnScaleTought;
            private Func<Task> RequestPermissionToAccept;
            private Func<Task> RequestPermissionToStart;
            private Func<Task> HasResult;

            public CameraScaleTeacherBuilder SetOnGoNAskFirstMarkerAction(Func<Task> action)
            {
                GoNAskFirstMarker = action;
                return this;
            }
            public CameraScaleTeacherBuilder SetOnAskSecondMarkerAction(Func<Task> action)
            {
                AskSecondMarker = action;
                return this;
            }
            public CameraScaleTeacherBuilder SetOnGoLoadPointAction(Func<Task> action)
            {
                GoLoadPoint = action;
                return this;
            }
            public CameraScaleTeacherBuilder SetOnScaleToughtAction(Func<Task> action)
            {
                OnScaleTought = action;
                return this;
            }
            public CameraScaleTeacherBuilder SetOnRequestPermissionToAcceptAction(Func<Task> action)
            {
                RequestPermissionToAccept = action;
                return this;
            }
            public CameraScaleTeacherBuilder SetOnRequestPermissionToStartAction(Func<Task> action)
            {
                RequestPermissionToStart = action;
                return this;
            }
            public CameraScaleTeacherBuilder SetOnHasResultAction(Func<Task> action)
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
            UnderFirstMarker,
            UnderSecondMarker,
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
