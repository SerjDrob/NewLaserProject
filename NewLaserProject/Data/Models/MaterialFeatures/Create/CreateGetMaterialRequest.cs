using MediatR;
using NewLaserProject.Data.Models.MaterialFeatures.Get;

namespace NewLaserProject.Data.Models.MaterialFeatures.Create
{

    public record CreateGetMaterialRequest(Material Material) : IRequest<GetFullMaterialResponse>;
}