using System.Linq;
using Ardalis.Specification;

namespace NewLaserProject.Data.Models.MaterialFeatures.Get;

public class MaterialsFullHasTechnologySpec : Specification<Material>
{
    public MaterialsFullHasTechnologySpec()
    {
        Query.Include(m => m.Technologies)
            .Where(m => m.Technologies != null)
            .Where(m => m.Technologies.Any());
    }
}