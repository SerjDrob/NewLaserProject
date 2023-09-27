using System.Collections.Generic;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get
{
    public record GetDefaultLayerFiltersFullResponse(IEnumerable<DefaultLayerFilter> DefaultLayerFilters);
}