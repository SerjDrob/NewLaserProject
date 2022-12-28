using NewLaserProject.Classes;

namespace NewLaserProject.ViewModels
{
    public record PermitSnap(bool Permited): IProcessNotify;
}
