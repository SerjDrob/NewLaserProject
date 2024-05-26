using MediatR;

namespace NewLaserProject.Data.Models.MaterialFeatures.Update
{
    internal record UpdateMaterialRequest(Material material):IRequest<UpdateMaterialResponse>;
}
