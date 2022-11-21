﻿using MachineClassLibrary.Classes;
using MachineClassLibrary.Laser.Entities;
using MachineClassLibrary.GeometryUtility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes
{
    public interface IProcess: IObservable<IProcessNotify>
    {
        event EventHandler<IEnumerable<IProcObject>> CurrentWaferChanged;
        event EventHandler<(IProcObject,int)> ProcessingObjectChanged;
        event EventHandler<ProcessCompletedEventArgs> ProcessingCompleted;
        void ExcludeObject(IProcObject procObject);
        void IncludeObject(IProcObject procObject);
        void CreateProcess();
        Task Deny();
        Task Next();
        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }

    [Flags]
    public enum CompletionStatus
    {
        Success = 1,
        Cancelled = 2
    }

    public class ProcessCompletedEventArgs : EventArgs
    {
        public ProcessCompletedEventArgs(CompletionStatus status, ICoorSystem coorSystem)
        {
            Status = status;
            CoorSystem = coorSystem;
        }

        public CompletionStatus Status { get; init; }
        public ICoorSystem CoorSystem { get; init; }
    }
}