using System.Linq;
using Ardalis.Specification;

namespace NewLaserProject.Data.Models.MaterialEntRuleFeatures.Get
{
    public class GetRuleByMaterialIdSpec : SingleResultSpecification<MaterialEntRule>
    {
        public GetRuleByMaterialIdSpec(int materialId)
        {
            Query.Where(rule => rule.MaterialId == materialId);
        }
    }
}