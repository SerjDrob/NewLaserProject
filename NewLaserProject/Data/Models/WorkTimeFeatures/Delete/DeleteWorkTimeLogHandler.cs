using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Delete
{
    public class DeleteWorkTimeLogHandler : BaseRequestHandler<WorkTimeLog, DeleteWorkTimeLogRequest, DeleteWorkTimeLogResponse>
    {
        public DeleteWorkTimeLogHandler(IRepository<WorkTimeLog> repository) : base(repository)
        {
        }
        public override async Task<DeleteWorkTimeLogResponse> Handle(DeleteWorkTimeLogRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAsync(request.WorkTimeLog, cancellationToken).ConfigureAwait(false);
            return new DeleteWorkTimeLogResponse(true);
        }
    }
}
