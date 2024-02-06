using MediatR;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Update
{
    public record UpdateWorkTimeRequest(WorkTimeLog WorkTimeLog) : IRequest<UpdateWorkTimeResponse>;
}
