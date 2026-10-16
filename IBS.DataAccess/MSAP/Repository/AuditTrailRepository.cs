using IBS.DataAccess.MSAP.Data;
using IBS.DataAccess.MSAP.Repository.IRepository;
using IBS.Models.MSAP;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.MSAP.Repository
{
    public class AuditTrailRepository(MsapDbContext db): Repository<AuditTrail>(db), IAuditTrailRepository
    {
        private readonly MsapDbContext _db = db;

        public async Task<(IEnumerable<AuditTrail> Data, int RecordsFiltered, int TotalRecords)> GetPagedAuditTrailsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var query = _db.AuditTrails.AsQueryable();

            if (!string.IsNullOrEmpty(parameters.Search?.Value))
            {
                var search = parameters.Search.Value.ToLower();
                query = query.Where(a =>
                    a.Username.ToLower().Contains(search) ||
                    a.Activity.ToLower().Contains(search) ||
                    a.DocumentType.ToLower().Contains(search) ||
                    (a.ReferenceNumber != null && a.ReferenceNumber.ToLower().Contains(search))
                );
            }

            // Column-specific search
            if (parameters?.Columns != null)
            {
                foreach (var column in parameters.Columns)
                {
                    if (column.Search?.Value is { Length: > 0 } searchValue)
                    {
                        var s = searchValue.ToLower();
                        if (column.Data == "documentType")
                        {
                            query = query.Where(a => a.DocumentType.ToLower() == s);
                        }
                        else if (column.Data == "date")
                        {
                            if (DateTime.TryParse(searchValue, out var parsedDate))
                            {
                                var dateOnly = parsedDate.Date;
                                query = query.Where(a => a.Date.Date == dateOnly);
                            }
                        }
                    }
                }
            }

            int totalRecords = await _db.AuditTrails.CountAsync(cancellationToken);
            int recordsFiltered = await query.CountAsync(cancellationToken);

            // Sorting
            if (parameters?.Order != null && parameters.Order.Any())
            {
                var order = parameters.Order[0];
                var column = parameters.Columns[order.Column].Data;
                var dir = order.Dir.ToLower();

                query = column switch
                {
                    "date" => dir == "asc" ? query.OrderBy(a => a.Date) : query.OrderByDescending(a => a.Date),
                    "username" => dir == "asc" ? query.OrderBy(a => a.Username) : query.OrderByDescending(a => a.Username),
                    "documentType" => dir == "asc" ? query.OrderBy(a => a.DocumentType) : query.OrderByDescending(a => a.DocumentType),
                    "referenceNumber" => dir == "asc" ? query.OrderBy(a => a.ReferenceNumber) : query.OrderByDescending(a => a.ReferenceNumber),
                    _ => query.OrderByDescending(a => a.Date)
                };
            }
            else
            {
                query = query.OrderByDescending(a => a.Date);
            }

            var data = await query
                .Skip(parameters?.Start ?? 0)
                .Take(parameters?.Length ?? 10)
                .ToListAsync(cancellationToken);

            return (data, RecordsFiltered: recordsFiltered, TotalRecords: totalRecords);
        }
    }
}
