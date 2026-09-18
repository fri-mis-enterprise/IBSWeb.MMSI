using IBS.Models.MSAP;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IReportRepository
    {
        Task<List<DispatchTicket>> GetDispatchReportData(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default, bool filterByBillingDate = false);
    }
}



