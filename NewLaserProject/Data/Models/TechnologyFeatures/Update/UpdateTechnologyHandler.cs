using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Data.Models.TechnologyFeatures.Create;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Update
{
    public class UpdateTechnologyHandler : BaseRequestHandler<Technology, UpdateTechnologyRequest, CreateTechnologyResponse>
    {
        public UpdateTechnologyHandler(IRepository<Technology> repository) : base(repository)
        {
        }

        public async override Task<CreateTechnologyResponse> Handle(UpdateTechnologyRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.UpdateAsync(request.Technology, cancellationToken).ConfigureAwait(false);
            return new CreateTechnologyResponse(request.Technology);
        }
    }
}
