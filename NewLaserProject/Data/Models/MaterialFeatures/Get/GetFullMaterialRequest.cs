using MediatR;

namespace NewLaserProject.Data.Models.MaterialFeatures.Get
{

    public record GetFullMaterialRequest() : IRequest<GetFullMaterialResponse>;
}