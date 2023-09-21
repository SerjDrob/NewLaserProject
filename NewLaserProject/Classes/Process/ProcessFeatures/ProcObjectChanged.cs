using MachineClassLibrary.Laser.Entities;

namespace NewLaserProject.Classes.Process.ProcessFeatures
{
    public record ProcObjectChanged(IProcObject ProcObject) : IProcessNotify;
}
