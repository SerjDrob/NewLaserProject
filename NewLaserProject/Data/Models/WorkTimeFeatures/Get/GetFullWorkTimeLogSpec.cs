using Ardalis.Specification;

namespace NewLaserProject.Data.Models.WorkTimeFeatures.Get
{
    public class GetFullWorkTimeLogSpec : Specification<WorkTimeLog>
    {
        public GetFullWorkTimeLogSpec()
        {
            Query.Include(d => d.ProcTimeLogs);
        }
    }
}
