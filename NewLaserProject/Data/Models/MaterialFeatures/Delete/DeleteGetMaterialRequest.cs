using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediatR;
using NewLaserProject.Data.Models.MaterialFeatures.Get;

namespace NewLaserProject.Data.Models.MaterialFeatures.Delete;
public record DeleteGetMaterialRequest(Material Material):IRequest<GetFullMaterialResponse>;
