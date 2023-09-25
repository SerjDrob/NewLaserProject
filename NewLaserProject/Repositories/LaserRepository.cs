using System.Threading.Tasks;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NewLaserProject.Data;
using NewLaserProject.Data.Models;

namespace NewLaserProject.Repositories;
public class LaserRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class
{
    public LaserRepository(LaserDbContext dbContext) : base(dbContext)
    {
    }
}
