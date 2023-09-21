using System.Collections.Generic;

namespace NewLaserProject.Data.Models.MaterialFeatures.Get;

public record GetFullMaterialResponse(IEnumerable<Material> Materials);
