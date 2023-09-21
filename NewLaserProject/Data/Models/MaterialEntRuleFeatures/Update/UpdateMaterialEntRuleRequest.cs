using MediatR;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Update;

public record UpdateMaterialEntRuleRequest(MaterialEntRule MaterialEntRule):IRequest<UpdateMaterialEntRuleResponse>;
