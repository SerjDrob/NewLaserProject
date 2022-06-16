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
    }
    public interface IFuncProxy2<T> : IFuncProxy2
    {
        Func<Task> GetFuncWithArguments(T arg);
    }
    public interface IFuncProxy2
    {
        //Func<Task> GetFuncWithArguments<T>(T arg);
    }
}