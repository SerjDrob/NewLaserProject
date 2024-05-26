using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialFeatures.Update
{
    internal class UpdateMaterialHandler : BaseRequestHandler<Material, UpdateMaterialRequest, UpdateMaterialResponse>
    {
        public UpdateMaterialHandler(IRepository<Material> repository) : base(repository)
        {
        }

        public override async Task<UpdateMaterialResponse> Handle(UpdateMaterialRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.UpdateAsync(request.material, cancellationToken);
            var result = await _repository.GetByIdAsync(request.material.Id);
            return new UpdateMaterialResponse(result);
        }
    }
}
