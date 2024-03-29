﻿using System.Threading;
using System.Threading.Tasks;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Create
{
    public class CreateMaterialEntRuleHandler : BaseRequestHandler<MaterialEntRule, CreateMaterialEntRuleRequest, CreateMaterialEntRuleResponse>
    {
        public CreateMaterialEntRuleHandler(IRepository<MaterialEntRule> repository) : base(repository)
        {
        }

        public async override Task<CreateMaterialEntRuleResponse> Handle(CreateMaterialEntRuleRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _repository.AddAsync(request.MaterialEntRule, cancellationToken).ConfigureAwait(false);
            return new CreateMaterialEntRuleResponse(result.Id);
        }
    }
}