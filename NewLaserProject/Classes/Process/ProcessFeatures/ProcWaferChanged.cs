using MachineClassLibrary.Laser.Entities;
using System.Collections.Generic;

namespace NewLaserProject.Classes.Process.ProcessFeatures
{
    public record ProcWaferChanged(IEnumerable<IProcObject> Wafer) : IProcessNotify;
}
