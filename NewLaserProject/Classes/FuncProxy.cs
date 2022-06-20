using MachineClassLibrary.Laser;
using Microsoft.Toolkit.Diagnostics;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    //public class FuncProxy<TAction> : IFuncProxy
    //{
    //    private readonly TAction _action;
    //    public FuncProxy(TAction action)
    //    {
    //        if (typeof(TAction).IsSubclassOf(typeof(Delegate)))
    //        {
    //            _action = action;
    //        }
    //        else
    //        {
    //            throw new InvalidOperationException(typeof(TAction).Name + " is not a delegate type");
    //        }
    //    }
    //    public Action GetActionWithArguments(int arg)
    //    {
    //        var act = _action as Action<int>;
    //        Guard.IsNotNull(act, $"{nameof(_action)} has incorrect signature");
    //        return () => act.Invoke(arg);
    //    }
    //    public Action GetActionWithArguments(double arg)
    //    {
    //        var act = _action as Action<double>;
    //        Guard.IsNotNull(act, $"{nameof(_action)} has incorrect signature");
    //        return () => act.Invoke(arg);
    //    }
    //    public Action GetActionWithArguments(MarkLaserParams arg)
    //    {
    //        var act = _action as Action<MarkLaserParams>;
    //        Guard.IsNotNull(act, $"{nameof(_action)} has incorrect signature");
    //        return () => act.Invoke(arg);
    //    }

    //    public Action GetActionWithArguments(ExtendedParams arg)
    //    {
    //        var act = _action as Action<ExtendedParams>;
    //        Guard.IsNotNull(act, $"{nameof(_action)} has incorrect signature");
    //        return () => act.Invoke(arg);
    //    }
    //}

    public class FuncProxy2<T> : IFuncProxy2<T>
    {
        private readonly Func<T, Task> _func;
        private T _arg;
        public FuncProxy2(Func<T, Task> func)
        {
            _func = func;
        }
        public FuncProxy2(Action action)
        {
            _func = _ => { action.Invoke(); return Task.CompletedTask; };
        }
        public FuncProxy2(Action<T> action)
        {
            _func = arg => { action.Invoke(arg); return Task.CompletedTask; };
        }

        public async Task ExecuteAsync()
        {
            await GetFuncWithArgument(_arg).Invoke();
        }

        public Func<Task> GetFuncWithArgument(T arg)
        {
            return async () => await _func.Invoke(arg);
        }

        public void SetArgument(T arg)
        {
            _arg = arg;
        }
    }
}
