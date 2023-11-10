using System.Collections.Generic;
using MediatR;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Delete
{
    public record DeleteDefaultLayerFiltersRequest(IEnumerable<DefaultLayerFilter> Filters) : IRequest<DeleteDefaultLayerFiltersResponse>;
}