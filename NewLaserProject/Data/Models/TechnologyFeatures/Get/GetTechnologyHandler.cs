using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Get
{
    public class GetTechnologyHandler : BaseRequestHandler<Technology, GetTechnologyByIdRequest, GetTechnologyResponse>
    {
        public GetTechnologyHandler(IRepository<Technology> repository) : base(repository)
        {
        }

        public override async Task<GetTechnologyResponse> Handle(GetTechnologyByIdRequest request, CancellationToken cancellationToken = default)
        {
            var technology = await _repository.GetByIdAsync(request.Id, cancellationToken);
            return new GetTechnologyResponse(technology);
        }
    }
}
