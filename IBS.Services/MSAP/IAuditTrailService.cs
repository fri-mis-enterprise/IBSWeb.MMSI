using IBS.Models;

namespace IBS.Services.MSAP
{
    public interface IAuditTrailService
    {
        Task<IEnumerable<MsapAuditTrail>> GetAuditTrailsByEntityAsync(string documentType, int recordId, CancellationToken cancellationToken);

        Task<IEnumerable<MsapAuditTrail>> GetJobOrderTimelineAsync(int jobOrderId, CancellationToken cancellationToken);

        Task<(IEnumerable<MsapAuditTrail> Data, int RecordsFiltered, int TotalRecords)> GetPagedAuditTrailsAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }
}
