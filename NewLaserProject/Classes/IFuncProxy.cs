using MachineClassLibrary.Laser;
using NewLaserProject;
using NewLaserProject.Classes;
using NewLaserProject.ViewModels;
using System;

namespace NewLaserProject.Classes
{
    public interface IFuncProxy
    {
        Action GetActionWithArguments(double arg);
        Action GetActionWithArguments(int arg);
        Action GetActionWithArguments(MarkLaserParams arg);
    }
}