using MediatR;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Get
{
    public record GetFullWorkTimeLogRequest():IRequest<GetFullWorkTimeLogResponse>;
}
