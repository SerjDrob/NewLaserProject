using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialFeatures.Create
{
    public class CreateMaterialHandler : IRequestHandler<CreateMaterialRequest, CreateMaterialResponse>
    {
        private readonly IRepository<Material> _repository;

        public CreateMaterialHandler(IRepository<Material> repository)
        {
            _repository = repository;
        }

        public async Task<CreateMaterialResponse> Handle(CreateMaterialRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _repository.AddAsync(request.Material, cancellationToken);
            var response = new CreateMaterialResponse(result.Id);
            return response;
        }
    }
}
