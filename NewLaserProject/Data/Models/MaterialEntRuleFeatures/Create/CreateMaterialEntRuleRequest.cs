using MediatR;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Create
{
    public record CreateMaterialEntRuleRequest(MaterialEntRule MaterialEntRule) : IRequest<CreateMaterialEntRuleResponse>;
}