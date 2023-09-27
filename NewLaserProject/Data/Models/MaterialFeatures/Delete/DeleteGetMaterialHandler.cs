using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialFeatures.Delete
{

    public class DeleteGetMaterialHandler : BaseRequestHandler<Material, DeleteGetMaterialRequest, GetFullMaterialResponse>
    {
        public DeleteGetMaterialHandler(IRepository<Material> repository) : base(repository)
        {
        }

        public async override Task<GetFullMaterialResponse> Handle(DeleteGetMaterialRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAsync(request.Material, cancellationToken).ConfigureAwait(false);
            var spec = new MaterialFullInfoSpec();
            var result = await _repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
            return new GetFullMaterialResponse(result);
        }
    }
}