using System.Collections.Generic;
using MediatR;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Create
{
    public record CreateDefaultLayerFiltersRequest(IEnumerable<DefaultLayerFilter> Filters):IRequest<CreateDefaultLayerFiltersResponse>;
}