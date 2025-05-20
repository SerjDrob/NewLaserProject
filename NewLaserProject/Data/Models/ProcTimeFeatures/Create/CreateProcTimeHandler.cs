using System;
using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.ProcTimeFeatures.Create
{
    public class CreateProcTimeHandler : BaseRequestHandler<ProcTimeLog, CreateProcTimeRequest, CreateProcTimeResponse>
    {
        public CreateProcTimeHandler(IRepository<ProcTimeLog> repository) : base(repository)
        {
        }

        public override async Task<CreateProcTimeResponse> Handle(CreateProcTimeRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _repository.AddAsync(request.ProcTimeLog, cancellationToken);
                return new CreateProcTimeResponse(result);

            }
            catch (Exception ex)
            {
                return new CreateProcTimeResponse(null); //TODO fix it
            }
        }
    }
}
