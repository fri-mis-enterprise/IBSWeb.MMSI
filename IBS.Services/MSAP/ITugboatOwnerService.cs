using IBS.Models.MSAP.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Services
{
    public interface ITugboatOwnerService
    {
        Task<IEnumerable<TugboatOwner>> GetAllAsync(CancellationToken cancellationToken);
        Task<TugboatOwner?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateAsync(TugboatOwner model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(TugboatOwner model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken);
    }
}
