using MachineClassLibrary.Laser.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface IProcess
    {
        event EventHandler<IEnumerable<IProcObject>> CurrentWaferChanged;
        event EventHandler<(IProcObject,int)> ProcessingObjectChanged;
        void ExcludeObject(IProcObject procObject);
        void IncludeObject(IProcObject procObject);
        void CreateProcess();
        Task Deny();
        Task Next();
        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }
}