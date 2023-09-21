using System;
using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialFeatures.Create;

public class CreateGetMaterialHandler : BaseRequestHandler<Material, CreateGetMaterialRequest, GetFullMaterialResponse>
{
    public CreateGetMaterialHandler(IRepository<Material> repository) : base(repository)
    {
    }

    public async override Task<GetFullMaterialResponse> Handle(CreateGetMaterialRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(request.Material, cancellationToken).ConfigureAwait(false);
        if (result is not null)
        {
            var spec = new MaterialFullInfoSpec();
            var response = await _repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
            return new GetFullMaterialResponse(response);
        }
        throw new NullReferenceException();
    }
}
