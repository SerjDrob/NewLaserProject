using MediatR;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Delete
{
    public record DeleteWorkTimeLogRequest(WorkTimeLog WorkTimeLog) : IRequest<DeleteWorkTimeLogResponse>;
}
