using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Get
{
    public class GetRuleByMaterialIdHandler : BaseRequestHandler<MaterialEntRule, GetRuleByMaterialIdRequest, GetMaterialEntRuleResponse>
    {
        public GetRuleByMaterialIdHandler(IRepository<MaterialEntRule> repository) : base(repository)
        {
        }

        public async override Task<GetMaterialEntRuleResponse> Handle(GetRuleByMaterialIdRequest request, CancellationToken cancellationToken = default)
        {
            var spec = new GetRuleByMaterialIdSpec(request.Id);
            var result = await _repository.SingleOrDefaultAsync(spec, cancellationToken).ConfigureAwait(false);
            return new GetMaterialEntRuleResponse(result);
        }
    }
}