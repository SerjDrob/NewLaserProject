using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get
{

    public class GetDefaultLayerFiltersFullHandler : BaseRequestHandler<DefaultLayerFilter, GetDefaultLayerFiltersFullRequest, GetDefaultLayerFiltersFullResponse>
    {
        public GetDefaultLayerFiltersFullHandler(IRepository<DefaultLayerFilter> repository) : base(repository)
        {
        }

        public async override Task<GetDefaultLayerFiltersFullResponse> Handle(GetDefaultLayerFiltersFullRequest request, CancellationToken cancellationToken = default)
        {
            var spec = new GetDefaultLayerFiltersFullSpec();
            var response = await _repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
            return new GetDefaultLayerFiltersFullResponse(response);
        }
    }
}
