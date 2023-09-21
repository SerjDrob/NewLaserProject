using Ardalis.Specification;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get;

public class GetDefaultLayerFiltersFullSpec : Specification<DefaultLayerFilter>
{
    public GetDefaultLayerFiltersFullSpec()
    {
        Query.Include(f => f.DefaultLayerEntityTechnologies);
    }
}
