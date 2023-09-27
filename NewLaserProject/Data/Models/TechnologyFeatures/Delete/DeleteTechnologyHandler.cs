using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Delete
{

    public class DeleteTechnologyHandler : BaseRequestHandler<Technology, DeleteTechnologyRequest, DeleteTechnologyResponse>
    {
        public DeleteTechnologyHandler(IRepository<Technology> repository) : base(repository)
        {
        }

        public async override Task<DeleteTechnologyResponse> Handle(DeleteTechnologyRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAsync(request.Technology, cancellationToken).ConfigureAwait(false);
            return new DeleteTechnologyResponse(true);
        }
    }
}