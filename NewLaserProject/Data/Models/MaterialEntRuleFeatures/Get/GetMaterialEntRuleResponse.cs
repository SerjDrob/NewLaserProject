using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using MediatR;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Get;
public record GetMaterialEntRuleResponse(MaterialEntRule MaterialEntRule);
public record GetRuleByMaterialIdRequest(int Id):IRequest<GetMaterialEntRuleResponse>;

public class GetRuleByMaterialIdHandler : BaseRequestHandler<MaterialEntRule, GetRuleByMaterialIdRequest, GetMaterialEntRuleResponse>
{
    public GetRuleByMaterialIdHandler(IRepository<MaterialEntRule> repository) : base(repository)
    {
    }

    public async override Task<GetMaterialEntRuleResponse> Handle(GetRuleByMaterialIdRequest request, CancellationToken cancellationToken = default)
    {
        var spec = new GetRuleByMaterialIdSpec(request.Id);
        var result = await _repository.SingleOrDefaultAsync(spec,cancellationToken).ConfigureAwait(false);
        return new GetMaterialEntRuleResponse(result);
    }
}

public class GetRuleByMaterialIdSpec : SingleResultSpecification<MaterialEntRule>
{
    public GetRuleByMaterialIdSpec(int materialId)
    {
        Query.Where(rule=>rule.MaterialId == materialId);
    }
}
