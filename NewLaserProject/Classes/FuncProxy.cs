using MachineClassLibrary.Laser;
using Microsoft.Toolkit.Diagnostics;
using NewLaserProject;
using NewLaserProject.Classes;
using NewLaserProject.ViewModels;
using System;

namespace NewLaserProject.Classes
{
    public class FuncProxy<TAction> : IFuncProxy
    {
        private readonly TAction _action;
        public FuncProxy(TAction action)
        {
            if (typeof(TAction).IsSubclassOf(typeof(Delegate)))
            {
                _action = action;
            }
            else
            {
                throw new InvalidOperationException(typeof(TAction).Name + " is not a delegate type");
            }
        }
        public Action GetActionWithArguments(int arg)
        {
            var act = _action as Action<int>;
            Guard.IsNotNull(act, $"{nameof(_action)} has incorrect signature");
            return () => act.Invoke(arg);
        }
        public Action GetActionWithArguments(double arg)
        {
            var act = _action as Action<double>;
            Guard.IsNotNull(act, $"{nameof(_action)} has incorrect signature");
            return () => act.Invoke(arg);
        }
        public Action GetActionWithArguments(MarkLaserParams arg)
        {
            var act = _action as Action<MarkLaserParams>;
            Guard.IsNotNull(act, $"{nameof(_action)} has incorrect signature");
            return () => act.Invoke(arg);
        }
    }
}
