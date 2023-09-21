using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using NewLaserProject.Classes.Process.ProcessFeatures;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface IProcess: IObservable<IProcessNotify>, IDisposable
    {
        void ExcludeObject(IProcObject procObject);
        void IncludeObject(IProcObject procObject);
        void CreateProcess();
        Task Deny();
        Task Next();
        Task StartAsync();
    }
}