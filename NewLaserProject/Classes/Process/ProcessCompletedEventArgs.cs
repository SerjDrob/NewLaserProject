using MachineClassLibrary.GeometryUtility;
using System;

namespace NewLaserProject.Classes
{
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