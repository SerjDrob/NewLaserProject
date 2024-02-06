using MediatR;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Create
{
    public record CreateWorkTimeLogRequest(WorkTimeLog WorkTimeLog) : IRequest<CreateWorkTimeLogResponse>;

}
