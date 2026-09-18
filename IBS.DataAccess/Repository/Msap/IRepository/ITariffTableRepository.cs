using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface ITariffTableRepository : IRepository<TariffRate>
    {
        Task SaveAsync(CancellationToken cancellationToken);
    }
}



