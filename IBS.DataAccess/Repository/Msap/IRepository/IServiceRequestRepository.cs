using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IServiceRequestRepository : IRepository<DispatchTicket>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<ServiceRequestViewModel> GetDispatchTicketSelectLists(ServiceRequestViewModel model, CancellationToken cancellationToken = default);

    }
}



