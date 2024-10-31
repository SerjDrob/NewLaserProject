using MachineClassLibrary.Classes;
using MachineClassLibrary.GeometryUtility;
using NewLaserProject.Classes.Process.ProcessFeatures;

namespace NewLaserProject.ViewModels
{
    public record GotAlignment(ICoorSystem CoorSystem):IProcessNotify;
}
