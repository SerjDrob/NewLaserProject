using MachineClassLibrary.GeometryUtility;

namespace NewLaserProject.Classes
{
    public record ProcCompletionPreview(CompletionStatus Status, ICoorSystem CoorSystem):IProcessNotify;
}
