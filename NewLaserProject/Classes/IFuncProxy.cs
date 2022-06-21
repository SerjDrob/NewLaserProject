using MachineClassLibrary.BehaviourTree;
using MachineClassLibrary.Laser;
using NewLaserProject;
using NewLaserProject.Classes;
using NewLaserProject.ViewModels;
using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    //public interface IFuncProxy
    //{
    //    Action GetActionWithArguments(double arg);
    //    Action GetActionWithArguments(int arg);
    //    Action GetActionWithArguments(MarkLaserParams arg);
    //    Action GetActionWithArguments(ExtendedParams arg);

    //}
    public interface IFuncProxy<T> : IExecutable
    {
        //void SetArgument(T arg);
        Func<Task> GetFuncWithArgument(T arg);
    }    
        
    public interface IExecutable
    {
        Task ExecuteAsync();
    }
}