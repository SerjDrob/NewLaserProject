using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialFeatures.Get
{

    public class GetFullMaterialHandler : BaseRequestHandler<Material, GetFullMaterialRequest, GetFullMaterialResponse>
    {
        public GetFullMaterialHandler(IRepository<Material> repository) : base(repository)
        {
        }

        public async override Task<GetFullMaterialResponse> Handle(GetFullMaterialRequest request, CancellationToken cancellationToken = default)
        {
            var spec = new MaterialFullInfoSpec();
            var result = await _repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
            var response = new GetFullMaterialResponse(result);
            return response;
        }
    }
}
