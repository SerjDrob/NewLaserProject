using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Update;

public class UpdateMaterialEntRuleHandler : BaseRequestHandler<MaterialEntRule, UpdateMaterialEntRuleRequest, UpdateMaterialEntRuleResponse>
{
    public UpdateMaterialEntRuleHandler(IRepository<MaterialEntRule> repository) : base(repository)
    {
    }

    public async override Task<UpdateMaterialEntRuleResponse> Handle(UpdateMaterialEntRuleRequest request, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(request.MaterialEntRule, cancellationToken).ConfigureAwait(false);    
        return new UpdateMaterialEntRuleResponse(request.MaterialEntRule.Id);
    }
}
