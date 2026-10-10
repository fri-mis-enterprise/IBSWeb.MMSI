using IBS.DataAccess.MSAP.Repository.IRepository;
using IBS.Models.MSAP;

namespace IBS.DataAccess.MSAP.Repository.Msap.IRepository
{
    public interface ITariffTableRepository : IRepository<TariffRate>
    {
        Task SaveAsync(CancellationToken cancellationToken);
    }
}
