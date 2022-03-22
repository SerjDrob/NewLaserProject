using Microsoft.Toolkit.Diagnostics;
using Stateless;
using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes;

public class WaferCornerTeacher : ITeacher
{
    private StateMachine<MyState, MyTrigger> _stateMachine;
    private (bool init, double x, double y) _newCorner = (false, 0, 0);

    public static WaferCornerTeacherBuilder GetBuilder()
    {
        return new WaferCornerTeacherBuilder();
    }
    private WaferCornerTeacher()
    {

    }
    private WaferCornerTeacher(Func<Task> OnCornerTought, Func<Task> RequestPermissionToAccept,
        Func<Task> RequestPermissionToStart, Func<Task> GiveResult, Func<Task> GoCornerPoint)
    {
        _stateMachine = new StateMachine<MyState, MyTrigger>(MyState.Begin, FiringMode.Queued);

        _stateMachine.Configure(MyState.Begin)
            .OnActivateAsync(RequestPermissionToStart)
            .Permit(MyTrigger.Accept, MyState.AtTheCorner)
            .Permit(MyTrigger.Deny, MyState.End)
            .Ignore(MyTrigger.Next);


        _stateMachine.Configure(MyState.AtTheCorner)
            .OnEntryAsync(GoCornerPoint)
            .Permit(MyTrigger.Next, MyState.RequestPermission)
            .Ignore(MyTrigger.Accept)
            .Ignore(MyTrigger.Deny);

        _stateMachine.Configure(MyState.RequestPermission)
           .OnEntryAsync(RequestPermissionToAccept)
           .Permit(MyTrigger.Accept, MyState.HasResult)
           .Permit(MyTrigger.Deny, MyState.End)
           .Ignore(MyTrigger.Next);

        _stateMachine.Configure(MyState.End)
           .OnEntryAsync(OnCornerTought)
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
        return $"(x: {Math.Round(_newCorner.x, 3)}, y: {Math.Round(_newCorner.y, 3)})";
    }
    public async Task Next() => await _stateMachine.FireAsync(MyTrigger.Next);
    public async Task Accept() => await _stateMachine.FireAsync(MyTrigger.Accept);
    public async Task Deny() => await _stateMachine.FireAsync(MyTrigger.Deny);

    public void SetParams(params double[] ps)
    {
        Guard.HasSizeEqualTo(ps, 2, nameof(ps));
        _newCorner = (true, ps[0], ps[1]);
    }

    public double[] GetParams()
    {
        return new double[] { _newCorner.x, _newCorner.y };
    }

    public async Task StartTeach()
    {
       await _stateMachine.ActivateAsync();
    }

    public class WaferCornerTeacherBuilder
    {
        public WaferCornerTeacher Build()
        {
            Guard.IsNotNull(GoCornerPoint, $"{nameof(GoCornerPoint)} isn't set");
            Guard.IsNotNull(OnCornerTought, $"{nameof(OnCornerTought)} isn't set");
            Guard.IsNotNull(RequestPermissionToAccept, $"{nameof(RequestPermissionToAccept)} isn't set");
            Guard.IsNotNull(RequestPermissionToStart, $"{nameof(RequestPermissionToStart)} isn't set");
            Guard.IsNotNull(HasResult, $"{nameof(HasResult)} isn't set");

            return new WaferCornerTeacher(OnCornerTought, RequestPermissionToAccept, RequestPermissionToStart, HasResult, GoCornerPoint);
        }

        private Func<Task> GoCornerPoint;
        private Func<Task> OnCornerTought;
        private Func<Task> RequestPermissionToAccept;
        private Func<Task> RequestPermissionToStart;
        private Func<Task> HasResult;

        public WaferCornerTeacherBuilder SetOnGoCornerPointAction(Func<Task> action)
        {
            GoCornerPoint = action;
            return this;
        }
        public WaferCornerTeacherBuilder SetOnCornerToughtAction(Func<Task> action)
        {
            OnCornerTought = action;
            return this;
        }
        public WaferCornerTeacherBuilder SetOnRequestPermissionToAcceptAction(Func<Task> action)
        {
            RequestPermissionToAccept = action;
            return this;
        }
        public WaferCornerTeacherBuilder SetOnRequestPermissionToStartAction(Func<Task> action)
        {
            RequestPermissionToStart = action;
            return this;
        }
        public WaferCornerTeacherBuilder SetOnHasResultAction(Func<Task> action)
        {
            HasResult = action;
            return this;
        }
    }


    private enum MyState
    {
        Begin,
        AtTheCorner,
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
