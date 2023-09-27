using MediatR;

namespace NewLaserProject.Data.Models.MaterialFeatures.Create
{

    public record CreateMaterialRequest(Material Material) : IRequest<CreateMaterialResponse>;
}