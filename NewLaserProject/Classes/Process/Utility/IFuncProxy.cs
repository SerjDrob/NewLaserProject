using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.Process.Utility
{
    [Obsolete]
    public interface IFuncProxy<T> : IExecutable
    {
        //void SetArgument(T arg);
        Func<Task> GetFuncWithArgument(T arg);
    }
    [Obsolete]
    public interface IExecutable
    {
        Task ExecuteAsync();
    }
}
