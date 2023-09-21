using MediatR;
using NewLaserProject.Data.Models.TechnologyFeatures.Create;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Update;
public record UpdateTechnologyRequest(Technology Technology) : IRequest<CreateTechnologyResponse>;
