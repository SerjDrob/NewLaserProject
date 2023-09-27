using Ardalis.Specification;

namespace NewLaserProject.Repositories
{
    public interface IRepository<T> : IRepositoryBase<T> where T : class
    {
    }
}