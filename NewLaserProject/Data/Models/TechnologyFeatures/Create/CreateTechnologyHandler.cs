using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.TechnologyFeatures.Create;

public class CreateTechnologyHandler : BaseRequestHandler<Technology, CreateTechnologyRequest, CreateTechnologyResponse>
{
    public CreateTechnologyHandler(IRepository<Technology> repository) : base(repository)
    {
    }

    public async override Task<CreateTechnologyResponse> Handle(CreateTechnologyRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(request.Technology,cancellationToken).ConfigureAwait(false);
        return new CreateTechnologyResponse(result.Id);
    }
}

