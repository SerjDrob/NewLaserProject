using NewLaserProject.Classes.Process.ProcessFeatures;

namespace NewLaserProject.ViewModels
{
    public record PermitSnap(bool Permited): IProcessNotify;
}
