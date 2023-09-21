using System;

namespace NewLaserProject.Classes.Process.ProcessFeatures
{
    [Flags]
    public enum CompletionStatus
    {
        Success = 1,
        Cancelled = 2
    }
}