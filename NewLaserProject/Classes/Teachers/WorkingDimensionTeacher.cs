using Microsoft.Toolkit.Diagnostics;
using Stateless;
using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.Teachers
{

    internal class WorkingDimensionTeacher : ITeacher
    {
        private StateMachine<MyState, MyTrigger> _stateMachine;

        private (double neg, double pos, bool negTought) _newEdges;


        public static CameraBiasTeacherBuilder GetBuilder()
        {
            return new CameraBiasTeacherBuilder();
        }
        private WorkingDimensionTeacher()
        {

        }
        private WorkingDimensionTeacher(Func<Task> AtNegativeEdge, Func<Task> AtPositiveEdge,
            Func<Task> OnDimensionTought, Func<Task> RequestPermissionToAccept, Func<Task> RequestPermissionToStart, Func<Task> GiveResult)
        {
            _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

            _stateMachine.Configure(MyState.Begin)
                .OnActivateAsync(RequestPermissionToStart)
                .Permit(MyTrigger.Accept, MyState.GoNegativeEdge)
                .Permit(MyTrigger.Deny, MyState.End)
                .Ignore(MyTrigger.Next);


            _stateMachine.Configure(MyState.GoNegativeEdge)
                .OnEntryAsync(AtNegativeEdge)
                .Permit(MyTrigger.Next, MyState.GoPositiveEdge)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.GoPositiveEdge)
               .OnEntryAsync(AtPositiveEdge)
               .Permit(MyTrigger.Next, MyState.RequestAcception)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);


            _stateMachine.Configure(MyState.RequestAcception)
               .OnEntryAsync(RequestPermissionToAccept)
               .Permit(MyTrigger.Accept, MyState.HasResult)
               .Permit(MyTrigger.Deny, MyState.End)
               .Ignore(MyTrigger.Next);

            _stateMachine.Configure(MyState.End)
               .OnEntryAsync(OnDimensionTought)
               .Ignore(MyTrigger.Next)
               .Ignore(MyTrigger.Accept)
               .Ignore(MyTrigger.Deny);

            _stateMachine.Configure(MyState.HasResult)
                .OnEntryAsync(GiveResult)
                .Ignore(MyTrigger.Next)
                .Ignore(MyTrigger.Accept)
                .Ignore(MyTrigger.Deny);

        }


        public override string ToString()
        {
            return $"neg: {_newEdges.neg}, pos: {_newEdges.pos}";
        }
        public async Task Next() => await _stateMachine.FireAsync(MyTrigger.Next);
        public async Task Accept() => await _stateMachine.FireAsync(MyTrigger.Accept);
        public async Task Deny() => await _stateMachine.FireAsync(MyTrigger.Deny);

        public void SetParams(params double[] ps)
        {
            Guard.HasSizeEqualTo(ps, 1, nameof(ps));
            _newEdges = _newEdges.negTought ? _newEdges with { pos = ps[0] } : (ps[0], 0, true);
        }

        public double[] GetParams()
        {
            return new double[] { _newEdges.neg, _newEdges.pos };
        }

        public async Task StartTeach()
        {
            await _stateMachine.ActivateAsync();
        }

        public class CameraBiasTeacherBuilder
        {
            public WorkingDimensionTeacher Build()
            {
                Guard.IsNotNull(AtNegativeEdge, $"{nameof(AtNegativeEdge)} isn't set");
                Guard.IsNotNull(AtPositiveEdge, $"{nameof(AtPositiveEdge)} isn't set");
                Guard.IsNotNull(OnDimensionTought, $"{nameof(OnDimensionTought)} isn't set");
                Guard.IsNotNull(RequestAcception, $"{nameof(RequestAcception)} isn't set");
                Guard.IsNotNull(RequestStarting, $"{nameof(RequestStarting)} isn't set");
                Guard.IsNotNull(GiveResult, $"{nameof(GiveResult)} isn't set");
                return new WorkingDimensionTeacher(AtNegativeEdge, AtPositiveEdge, OnDimensionTought, RequestAcception, RequestStarting, GiveResult);
            }
            private Func<Task> OnDimensionTought;
            private Func<Task> RequestAcception;
            private Func<Task> RequestStarting;
            private Func<Task> GiveResult;
            private Func<Task> AtNegativeEdge;
            private Func<Task> AtPositiveEdge;

            public CameraBiasTeacherBuilder SetOnDimensionToughtAction(Func<Task> action)
            {
                OnDimensionTought = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnRequestAcceptionAction(Func<Task> action)
            {
                RequestAcception = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnRequestStartingAction(Func<Task> action)
            {
                RequestStarting = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnGiveResultAction(Func<Task> action)
            {
                GiveResult = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnAtNegativeEdgeAction(Func<Task> action)
            {
                AtNegativeEdge = action;
                return this;
            }
            public CameraBiasTeacherBuilder SetOnAtPositiveEdgeAction(Func<Task> action)
            {
                AtPositiveEdge = action;
                return this;
            }
        }


        private enum MyState
        {
            Begin,
            GoNegativeEdge,
            GoPositiveEdge,
            RequestAcception,
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