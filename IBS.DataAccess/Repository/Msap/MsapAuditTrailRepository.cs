using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.Models;
using IBS.DataAccess.Repository.Msap.IRepository;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap;

public sealed class MsapAuditTrailRepository(ApplicationDbContext db) : Repository<MsapAuditTrail>(db), IMsapAuditTrailRepository
{
    public async Task<(IEnumerable<MsapAuditTrail> Data, int RecordsFiltered, int TotalRecords)> GetPagedAuditTrailsAsync(IBS.Models.DataTablesParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = dbSet.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(parameters.Search.Value))
        {
            var search = parameters.Search.Value.ToLower();
            query = query.Where(x => x.Username.ToLower().Contains(search) || x.Activity.ToLower().Contains(search) || x.DocumentType.ToLower().Contains(search));
        }

        var filtered = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Date).Skip(parameters.Start).Take(parameters.Length).ToListAsync(cancellationToken);
        return (data, filtered, total);
    }
}
