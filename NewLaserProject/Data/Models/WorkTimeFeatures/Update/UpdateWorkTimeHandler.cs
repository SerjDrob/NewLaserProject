using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Update
{
    public class UpdateWorkTimeHandler : BaseRequestHandler<WorkTimeLog, UpdateWorkTimeRequest, UpdateWorkTimeResponse>
    {
        public UpdateWorkTimeHandler(IRepository<WorkTimeLog> repository) : base(repository)
        {
        }

        public override async Task<UpdateWorkTimeResponse> Handle(UpdateWorkTimeRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.UpdateAsync(request.WorkTimeLog, cancellationToken);
            return new UpdateWorkTimeResponse();
        }
    }
}
