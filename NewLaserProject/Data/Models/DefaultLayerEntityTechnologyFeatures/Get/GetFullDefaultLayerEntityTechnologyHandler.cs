using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.DefaultLayerEntityTechnologyFeatures.Get;

public class GetFullDefaultLayerEntityTechnologyHandler : BaseRequestHandler<DefaultLayerEntityTechnology, GetFullDefaultLayerEntityTechnologyRequest, GetFullDefaultLayerEntityTechnologyResponse>
{
    public GetFullDefaultLayerEntityTechnologyHandler(IRepository<DefaultLayerEntityTechnology> repository) : base(repository)
    {
    }

    public async override Task<GetFullDefaultLayerEntityTechnologyResponse> Handle(GetFullDefaultLayerEntityTechnologyRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return new GetFullDefaultLayerEntityTechnologyResponse(result);
    }
}
