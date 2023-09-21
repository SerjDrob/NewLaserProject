using Ardalis.Specification;

namespace NewLaserProject.Data.Models.MaterialFeatures.Get;

public class MaterialFullInfoSpec : Specification<Material>
{
    public MaterialFullInfoSpec()
    {
        Query.Include(m => m.Technologies);
    }
}
