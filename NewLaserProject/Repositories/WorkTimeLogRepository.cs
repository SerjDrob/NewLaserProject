using Ardalis.Specification.EntityFrameworkCore;
using NewLaserProject.Data;

namespace NewLaserProject.Repositories
{
    public class WorkTimeLogRepository<T>:RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T: class
    {
        public WorkTimeLogRepository(WorkTimeDbContext dbContext):base(dbContext)
        {
            
        }
    }
}
