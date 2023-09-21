using MachineClassLibrary.GeometryUtility;

namespace NewLaserProject.Classes.Process.ProcessFeatures
{
    public record ProcCompletionPreview(CompletionStatus Status, ICoorSystem CoorSystem) : IProcessNotify;
}
