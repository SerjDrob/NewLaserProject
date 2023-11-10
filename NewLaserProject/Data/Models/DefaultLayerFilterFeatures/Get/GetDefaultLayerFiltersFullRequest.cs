using System.Collections;
using MediatR;

namespace NewLaserProject.Data.Models.DefaultLayerFilterFeatures.Get
{
    public record GetDefaultLayerFiltersFullRequest() : IRequest<GetDefaultLayerFiltersFullResponse>;
}