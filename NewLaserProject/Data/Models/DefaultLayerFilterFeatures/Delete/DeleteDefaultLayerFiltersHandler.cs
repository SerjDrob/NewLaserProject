using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Delete
{
    public class DeleteDefaultLayerFiltersHandler : BaseRequestHandler<DefaultLayerFilter, DeleteDefaultLayerFiltersRequest, DeleteDefaultLayerFiltersResponse>
    {
        public DeleteDefaultLayerFiltersHandler(IRepository<DefaultLayerFilter> repository) : base(repository)
        {
        }

        public async override Task<DeleteDefaultLayerFiltersResponse> Handle(DeleteDefaultLayerFiltersRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteRangeAsync(request.Filters, cancellationToken).ConfigureAwait(false);
            return new DeleteDefaultLayerFiltersResponse();
        }
    }
}
