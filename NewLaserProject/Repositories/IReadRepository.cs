using Ardalis.Specification;

namespace NewLaserProject.Repositories
{
    public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class
    {
    }
}