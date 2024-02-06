using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Create
{
    public class CreateWorkTimeHandler : BaseRequestHandler<WorkTimeLog, CreateWorkTimeLogRequest, CreateWorkTimeLogResponse>
    {
        public CreateWorkTimeHandler(IRepository<WorkTimeLog> repository) : base(repository)
        {
        }
        public override async Task<CreateWorkTimeLogResponse> Handle(CreateWorkTimeLogRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _repository.AddAsync(request.WorkTimeLog, cancellationToken).ConfigureAwait(false);
            return new CreateWorkTimeLogResponse(result);
        }
    }
}
