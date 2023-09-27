using MediatR;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Delete
{
    public record DeleteTechnologyRequest(Technology Technology) : IRequest<DeleteTechnologyResponse>;
}