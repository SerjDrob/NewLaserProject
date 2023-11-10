using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Create
{
    public class CreateDefaultLayerFiltersHandler : BaseRequestHandler<DefaultLayerFilter, CreateDefaultLayerFiltersRequest, CreateDefaultLayerFiltersResponse>
    {
        public CreateDefaultLayerFiltersHandler(IRepository<DefaultLayerFilter> repository) : base(repository)
        {
        }

        public async override Task<CreateDefaultLayerFiltersResponse> Handle(CreateDefaultLayerFiltersRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _repository.AddRangeAsync(request.Filters, cancellationToken).ConfigureAwait(false);
            return new CreateDefaultLayerFiltersResponse();
        }
    }
}
