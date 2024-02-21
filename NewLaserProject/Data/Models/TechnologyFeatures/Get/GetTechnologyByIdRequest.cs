using MediatR;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Get
{
    public record GetTechnologyByIdRequest(int Id) : IRequest<GetTechnologyResponse>;
}
