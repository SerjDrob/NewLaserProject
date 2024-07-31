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
        private List<(double x, double y)> _points = new();

        public event EventHandler TeachingCompleted;

        public static XYOrthTeacherBuilder GetBuilder()
        {
            return new XYOrthTeacherBuilder();
        }
        private XYOrthTeacher()
        {

        }
        private XYOrthTeacher(Func<Task> OnXYOrthTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart,
            Func<Task> GiveResult, Func<Task> GoNextPoint, Func<Task> WriteDownThePoint)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnActivateAsync(RequestPermissionToStart)
                .Permit(MyTrigger.Accept, MyState.AtPoint)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.AtPoint)
                .OnEntryAsync(GoNextPoint)
                .PermitReentryIf(MyTrigger.Next, () => _points.Count < 2)
                .PermitIf(MyTrigger.Next, MyState.RequestPermission, () => _points.Count == 2)
                .OnExitAsync(WriteDownThePoint)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Accept);

            _stateMachine.Configure(MyState.RequestPermission)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(async()=> { await OnXYOrthTought.Invoke(); TeachingCompleted?.Invoke(this, EventArgs.Empty); })
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.HasResult)
                .OnEntryAsync(async()=> { await GiveResult.Invoke(); TeachingCompleted?.Invoke(this, EventArgs.Empty); })
                .Ignore(MyTrigger.Next)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);
        }


        public override string ToString()
        {
            return _points.Select(point => $"(x:{point.x}, y:{point.y})")
                          .Aggregate(new StringBuilder("Coordinates: "), (previous, current) => previous.AppendLine(current))
                          .ToString();
        }
        public async Task Next()
        {
            try
            {
                await _stateMachine.FireAsync(MyTrigger.Next);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task Accept()
        {
            await _stateMachine.FireAsync(MyTrigger.Accept);
        }
        public async Task Deny() => await _stateMachine.FireAsync(MyTrigger.Deny);

        public void SetParams(params double[] ps)
        {
            Guard.HasSizeEqualTo(ps, 2, nameof(ps));
            _points.Add((ps[0], ps[1]));
        }

        public double[] GetParams() => _points.Aggregate(new List<double>(), (acc, tup) => { acc.AddRange(new double[] { tup.x, tup.y }); return acc; }).ToArray();

        public async Task StartTeach()
        {
            await _stateMachine.ActivateAsync();
        }

        public void SetResult(double result)
        {
            throw new NotImplementedException();
        }

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
                Guard.IsNotNull(WriteDownThePoint, $"{nameof(WriteDownThePoint)} isn't set");

                return new XYOrthTeacher(OnXYOrthTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult, GoNextPoint, WriteDownThePoint);
            }
            private Func<Task> OnXYOrthTought;
            private Func<Task> RequestPermissionToAccept;
            private Func<Task> RequestPermissionToStart;
            private Func<Task> HasResult;
            private Func<Task> GoNextPoint;
            private Func<Task> WriteDownThePoint;

            public XYOrthTeacherBuilder SetOnWriteDownThePointAction(Func<Task> action)
            {
                WriteDownThePoint = action;
                return this;
            }
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
            AskPoint,
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
