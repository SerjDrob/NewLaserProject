using System.Collections.Generic;

namespace NewLaserProject.Data.Models.DefaultLayerEntityTechnologyFeatures.Get
{
    public record GetFullDefaultLayerEntityTechnologyResponse(IEnumerable<DefaultLayerEntityTechnology> DefaultLayerEntityTechnologies);
}