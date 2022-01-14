using Microsoft.Toolkit.Diagnostics;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    internal class LaserHorizontTeacher : ITeacher
    {
        private StateMachine<MyState, MyTrigger> _stateMachine;
        private LaserHorizontTeacher()
        {

        }
        private LaserHorizontTeacher(Func<Task> GoUnderLaser, Func<Task> GoAtFirstPoint, Func<Task> GoAtSecondPoint,
            Func<Task> OnLaserHorizontTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart, Func<Task> GiveResult)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnEntryAsync(RequestPermissionToStart)
                .Permit(MyTrigger.Accept, MyState.UnderLaser)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.UnderLaser)
                .OnEntryAsync(GoUnderLaser)
                .Permit(MyTrigger.Next,MyState.AtFirstPointUnderCamera)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Accept);

            _stateMachine.Configure(MyState.AtFirstPointUnderCamera)
                .OnEntryAsync(GoAtFirstPoint)
                .Permit(MyTrigger.Next,MyState.AtSecondPointUnderCamera)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Accept);

            _stateMachine.Configure(MyState.AtSecondPointUnderCamera)
                .OnEntryAsync(GoAtSecondPoint)
                .Permit(MyTrigger.Next, MyState.RequestPermission)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Accept);

            _stateMachine.Configure(MyState.RequestPermission)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(OnLaserHorizontTought)
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
        public static LaserHorizontTeacherBuilder GetBuilder() => new LaserHorizontTeacherBuilder();

        public async Task Next() => await _stateMachine.FireAsync(MyTrigger.Next);
        public async Task Accept() => await _stateMachine.FireAsync(MyTrigger.Accept);
        public async Task Deny() => await _stateMachine.FireAsync(MyTrigger.Deny);
        
        public double[] GetParams()
        {
            throw new NotImplementedException();
        }               

        public void SetParams(params double[] ps)
        {
            throw new NotImplementedException();
        }
        public class LaserHorizontTeacherBuilder
        {
            public LaserHorizontTeacher Build()
            {
                Guard.IsNotNull(OnLaserHorizontTought, $"{nameof(OnLaserHorizontTought)} isn't set");
                Guard.IsNotNull(RequestPermissionToAccept, $"{nameof(RequestPermissionToAccept)} isn't set");
                Guard.IsNotNull(RequestPermissionToStart, $"{nameof(RequestPermissionToStart)} isn't set");
                Guard.IsNotNull(HasResult, $"{nameof(HasResult)} isn't set");
                Guard.IsNotNull(GoUnderLaser, $"{nameof(GoUnderLaser)} isn't set");
                Guard.IsNotNull(GoAtFirstPoint, $"{nameof(GoAtFirstPoint)} isn't set");
                Guard.IsNotNull(GoAtSecondPoint, $"{nameof(GoAtSecondPoint)} isn't set");
                return new LaserHorizontTeacher(GoUnderLaser, GoAtFirstPoint, GoAtSecondPoint, OnLaserHorizontTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult);
            }
            private Func<Task> GoUnderLaser;
            private Func<Task> GoAtFirstPoint;
            private Func<Task> GoAtSecondPoint;
            private Func<Task> OnLaserHorizontTought;
            private Func<Task> RequestPermissionToAccept;
            private Func<Task> RequestPermissionToStart;
            private Func<Task> HasResult;

            public LaserHorizontTeacherBuilder SetGoUnderLaserAction(Func<Task> action)
            {
                GoUnderLaser = action;
                return this;
            }
            public LaserHorizontTeacherBuilder SetGoAtFirstPointAction(Func<Task> action)
            {
                GoAtFirstPoint = action;
                return this;
            }
            public LaserHorizontTeacherBuilder SetGoAtSecondPointAction(Func<Task> action)
            {
                GoAtSecondPoint = action;
                return this;
            }
            public LaserHorizontTeacherBuilder SetOnLaserHorizontToughtAction(Func<Task> action)
            {
                OnLaserHorizontTought = action;
                return this;
            }
            public LaserHorizontTeacherBuilder SetOnRequestPermissionToAcceptAction(Func<Task> action)
            {
                RequestPermissionToAccept = action;
                return this;
            }
            public LaserHorizontTeacherBuilder SetOnRequestPermissionToStartAction(Func<Task> action)
            {
                RequestPermissionToStart = action;
                return this;
            }
            public LaserHorizontTeacherBuilder SetOnHasResultAction(Func<Task> action)
            {
                HasResult = action;
                return this;
            }
        }
        private enum MyState
        {
            Begin,
            UnderLaser,
            AtFirstPointUnderCamera,
            AtSecondPointUnderCamera,
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
