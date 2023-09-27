using Ardalis.Specification;

namespace NewLaserProject.Data.Models.DefaultLayerEntityTechnologyFeatures.Get
{
    public class GetFullDefaultLayerEntityTechnologySpec : Specification<DefaultLayerEntityTechnology>
    {
        public GetFullDefaultLayerEntityTechnologySpec()
        {
            Query.Include(d => d.DefaultLayerFilter)
                .Include(d => d.Technology)
                .ThenInclude(t => t.Material);
        }
    }
}