using MediatR;

namespace NewLaserProject.Data.Models.ProcTimeFeatures.Create
{
    public record CreateProcTimeRequest(ProcTimeLog ProcTimeLog) : IRequest<CreateProcTimeResponse>;
}
