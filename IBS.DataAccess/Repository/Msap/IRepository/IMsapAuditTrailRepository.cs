using IBS.DataAccess.Repository.IRepository;
using IBS.Models;

namespace IBS.DataAccess.Repository.Msap.IRepository;

public interface IMsapAuditTrailRepository : IRepository<MsapAuditTrail>
{
    Task<(IEnumerable<MsapAuditTrail> Data, int RecordsFiltered, int TotalRecords)> GetPagedAuditTrailsAsync(IBS.Models.DataTablesParameters parameters, CancellationToken cancellationToken = default);
}
