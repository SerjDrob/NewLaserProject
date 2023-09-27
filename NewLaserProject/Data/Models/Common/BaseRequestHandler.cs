using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.Common
{
    public abstract class BaseRequestHandler<TEntity, TRequest, TResponse>
        : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse> where TEntity : class
    {
        protected readonly IRepository<TEntity> _repository;

        protected BaseRequestHandler(IRepository<TEntity> repository)
        {
            _repository = repository;
        }

        public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
    }
}

