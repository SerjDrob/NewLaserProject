using MachineClassLibrary.Laser;
using NewLaserProject;
using NewLaserProject.Classes;
using NewLaserProject.ViewModels;
using System;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface IFuncProxy
    {
        Action GetActionWithArguments(double arg);
        Action GetActionWithArguments(int arg);
        Action GetActionWithArguments(MarkLaserParams arg);
        Action GetActionWithArguments(ExtendedParams arg);

    }
    public interface IFuncProxy2<T> //: IFuncProxy2
    {
        Func<Task> GetFuncWithArguments(T arg);
    }
    //public interface IFuncProxy2
    //{
    //    Func<Task> GetActionWithArguments(double arg);
    //    Func<Task> GetActionWithArguments(int arg);
    //    Func<Task> GetActionWithArguments(MarkLaserParams arg);
    //}
}