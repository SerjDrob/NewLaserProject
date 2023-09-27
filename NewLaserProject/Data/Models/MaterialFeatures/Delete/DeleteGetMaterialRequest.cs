using MediatR;
using NewLaserProject.Data.Models.MaterialFeatures.Get;

namespace NewLaserProject.Data.Models.MaterialFeatures.Delete
{
    public record DeleteGetMaterialRequest(Material Material) : IRequest<GetFullMaterialResponse>;
}