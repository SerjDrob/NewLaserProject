using MachineClassLibrary.Laser;
using Microsoft.Toolkit.Diagnostics;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public class FuncProxy<T> : IFuncProxy<T>
    {
        private readonly Func<T, Task> _func;
        private T _arg;
        public FuncProxy(Func<T, Task> func)
        {
            _func = func;
        }
        public FuncProxy(Action action)
        {
            _func = _ => { action.Invoke(); return Task.CompletedTask; };
        }
        public FuncProxy(Action<T> action)
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
