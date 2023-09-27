using MediatR;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Get
{
    public record GetRuleByMaterialIdRequest(int Id) : IRequest<GetMaterialEntRuleResponse>;
}