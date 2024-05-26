using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using MediatR;
using NewLaserProject.Data.Models.Common;
using NewLaserProject.Data.Models.MaterialFeatures.Get;
using NewLaserProject.Repositories;

namespace NewLaserProject.Data.Models.MaterialFeatures.Delete
{

    public class DeleteGetMaterialHandler : BaseRequestHandler<Material, DeleteGetMaterialRequest, GetFullMaterialResponse>
    {
        public DeleteGetMaterialHandler(IRepository<Material> repository) : base(repository)
        {
        }

        public async override Task<GetFullMaterialResponse> Handle(DeleteGetMaterialRequest request, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAsync(request.Material, cancellationToken).ConfigureAwait(false);
            var spec = new MaterialFullInfoSpec();
            var result = await _repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
            return new GetFullMaterialResponse(result);
        }
    }

    public class DeleteFullMaterialHandler : BaseRequestHandler<Material, DeleteFullMaterialRequest, DeleteFullMaterialResponse>
    {
        public DeleteFullMaterialHandler(IRepository<Material> repository) : base(repository)
        {
        }

        public async override Task<DeleteFullMaterialResponse> Handle(DeleteFullMaterialRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var spec = new MaterialFullInfoSpec2();
                await _repository.DeleteRangeAsync(spec).ConfigureAwait(false);
                return new DeleteFullMaterialResponse(true);
            }
            catch (System.Exception)
            {
                return new DeleteFullMaterialResponse(false);
            }
        }
    }

    public record DeleteFullMaterialResponse(bool success);
    public record DeleteFullMaterialRequest():IRequest<DeleteFullMaterialResponse>;
    public class MaterialFullInfoSpec2 : Specification<Material>
    {
        public MaterialFullInfoSpec2()
        {
            Query.Include(m => m.Technologies)
                .Include(m => m.MaterialEntRule);
        }
    }
}
