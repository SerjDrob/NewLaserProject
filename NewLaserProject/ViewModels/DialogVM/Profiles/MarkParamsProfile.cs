using AutoMapper;
using MachineClassLibrary.Laser.Parameters;

namespace NewLaserProject.ViewModels.DialogVM.Profiles
{
    internal class MarkParamsProfile : Profile
    {
        public MarkParamsProfile()
        {
            CreateMap<MarkLaserParams, MarkSettingsVM>()
            .IncludeMembers(s => s.PenParams, s => s.HatchParams);
            CreateMap<PenParams, MarkSettingsVM>(MemberList.None);
            CreateMap<HatchParams, MarkSettingsVM>(MemberList.None);
        }
    }
}