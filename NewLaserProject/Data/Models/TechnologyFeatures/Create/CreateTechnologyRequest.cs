using MediatR;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Create
{
    public record CreateTechnologyRequest(Technology Technology) : IRequest<CreateTechnologyResponse>;
}