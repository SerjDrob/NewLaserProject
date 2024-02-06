using System.Collections.Generic;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Get
{
    public record GetFullWorkTimeLogResponse(IEnumerable<WorkTimeLog> Logs);
}
