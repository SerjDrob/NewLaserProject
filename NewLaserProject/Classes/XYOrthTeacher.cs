using Microsoft.Toolkit.Diagnostics;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{    
    internal class XYOrthTeacher : ITeacher
    {
        private StateMachine<MyState, MyTrigger> _stateMachine;
        private List<double> _points = new();

        public static XYOrthTeacherBuilder GetBuilder()
        {
            return new XYOrthTeacherBuilder();
        }
        private XYOrthTeacher()
        {

        }
        private XYOrthTeacher(Func<Task> OnXYOrthTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart, Func<Task> GiveResult, Func<Task> GoNextPoint)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnEntryAsync(RequestPermissionToStart)
                .Permit(MyTrigger.Accept, MyState.AtPoint)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.AtPoint)
                .OnEntryAsync(GoNextPoint)
                .PermitReentryIf(MyTrigger.Accept, () => _points.Count < 3)
                .PermitIf(MyTrigger.Accept, MyState.RequestPermission, new Tuple<Func<bool>, string>(() => _points.Count == 3, "Makes sure that count of points is enough to get learning done"))
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);


            _stateMachine.Configure(MyState.RequestPermission)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(OnXYOrthTought)
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
            return _points.Chunk(2)
                          .Select(point => $"(x:{point[0]}, y:{point[1]})")
                          .Aggregate(new StringBuilder("Coordinates: "), (previous, current) => previous.AppendLine(current))
                          .ToString();
        }
        public async Task Next() => await _stateMachine.FireAsync(MyTrigger.Next);
        public async Task Accept()
        {
            await _stateMachine.FireAsync(MyTrigger.Accept);
        }
        public async Task Deny() => await _stateMachine.FireAsync(MyTrigger.Deny);

        public void SetParams(params double[] ps)
        {
            Guard.HasSizeEqualTo(ps, 2, nameof(ps));
            _points.AddRange(ps);
        }

        public double[] GetParams() => _points.ToArray();

        public class XYOrthTeacherBuilder
        {
            public XYOrthTeacherBuilder()
            {

            }
            public XYOrthTeacher Build()
            {
                Guard.IsNotNull(OnXYOrthTought, $"{nameof(OnXYOrthTought)} isn't set");
                Guard.IsNotNull(RequestPermissionToAccept, $"{nameof(RequestPermissionToAccept)} isn't set");
                Guard.IsNotNull(RequestPermissionToStart, $"{nameof(RequestPermissionToStart)} isn't set");
                Guard.IsNotNull(HasResult, $"{nameof(HasResult)} isn't set");
                Guard.IsNotNull(GoNextPoint, $"{nameof(GoNextPoint)} isn't set");
                return new XYOrthTeacher(OnXYOrthTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult, GoNextPoint);
            }
            private Func<Task> OnXYOrthTought;
            private Func<Task> RequestPermissionToAccept;
            private Func<Task> RequestPermissionToStart;
            private Func<Task> HasResult;
            private Func<Task> GoNextPoint;


            public XYOrthTeacherBuilder SetOnXYOrthToughtAction(Func<Task> action)
            {
                OnXYOrthTought = action;
                return this;
            }
            public XYOrthTeacherBuilder SetOnGoNextPointAction(Func<Task> action)
            {
                GoNextPoint = action;
                return this;
            }
            public XYOrthTeacherBuilder SetOnRequestPermissionToAcceptAction(Func<Task> action)
            {
                RequestPermissionToAccept = action;
                return this;
            }
            public XYOrthTeacherBuilder SetOnRequestPermissionToStartAction(Func<Task> action)
            {
                RequestPermissionToStart = action;
                return this;
            }
            public XYOrthTeacherBuilder SetOnHasResultAction(Func<Task> action)
            {
                HasResult = action;
                return this;
            }
        }


        private enum MyState
        {
            Begin,
            AtPoint,
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
