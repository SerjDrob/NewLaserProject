using MediatR;

namespace NewLaserProject.Data.Models.MaterialFeatures.Get
{

    public record GetFullMaterialHasTechnologyRequest() : IRequest<GetFullMaterialResponse>;
}