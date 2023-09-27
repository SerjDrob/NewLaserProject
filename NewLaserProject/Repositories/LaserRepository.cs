using Ardalis.Specification.EntityFrameworkCore;
using NewLaserProject.Data;

namespace NewLaserProject.Repositories
{
    public class LaserRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class
    {
        public LaserRepository(LaserDbContext dbContext) : base(dbContext)
        {
        }
    }
}