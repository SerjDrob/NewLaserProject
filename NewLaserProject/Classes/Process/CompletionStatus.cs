using System;

namespace NewLaserProject.Classes
{
    [Flags]
    public enum CompletionStatus
    {
        Success = 1,
        Cancelled = 2
    }
}