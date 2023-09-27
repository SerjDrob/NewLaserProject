using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialFeatures.Get
{

    public class GetFullMaterialHasTechnologyHandler : IRequestHandler<GetFullMaterialHasTechnologyRequest, GetFullMaterialResponse>
    {
        private readonly IRepository<Material> _repository;

        public GetFullMaterialHasTechnologyHandler(IRepository<Material> repository)
        {
            _repository = repository;
        }

        public async Task<GetFullMaterialResponse> Handle(GetFullMaterialHasTechnologyRequest request, CancellationToken cancellationToken = default)
        {
            var spec = new MaterialsFullHasTechnologySpec();
            var result = await _repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
            var response = new GetFullMaterialResponse(result);
            return response;
        }
    }
}