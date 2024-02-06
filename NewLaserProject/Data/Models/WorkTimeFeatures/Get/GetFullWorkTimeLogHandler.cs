using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Get
{
    public class GetFullWorkTimeLogHandler : BaseRequestHandler<WorkTimeLog, GetFullWorkTimeLogRequest, GetFullWorkTimeLogResponse>
    {
        public GetFullWorkTimeLogHandler(IRepository<WorkTimeLog> repository) : base(repository)
        {
        }

        public async override Task<GetFullWorkTimeLogResponse> Handle(GetFullWorkTimeLogRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _repository.ListAsync(new GetFullWorkTimeLogSpec(), cancellationToken).ConfigureAwait(false);
            var response = new GetFullWorkTimeLogResponse(result);
            return response;
        }
    }
}
