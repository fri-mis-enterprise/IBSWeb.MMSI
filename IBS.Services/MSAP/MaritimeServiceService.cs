using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MSAP.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.MSAP
{
    public class MaritimeServiceService(IUnitOfWork unitOfWork, ILogger<MaritimeServiceService> logger) : IMaritimeServiceService
    {
        public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.MsapService.GetAllAsync(cancellationToken: cancellationToken);
        }

        public async Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.MsapService.GetAsync(s => s.ServiceId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(Service model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.MsapService.AddAsync(model, cancellationToken);

                var auditTrail = new MsapAuditTrail(username, $"Created new Service #{model.ServiceNumber}", "Service");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.ServiceId, "Service created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Service");
                return ServiceResult<int>.Failure($"Failed to create service: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(Service model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingService = await unitOfWork.MsapService.GetAsync(s => s.ServiceId == model.ServiceId, cancellationToken);
                if (existingService == null)
                {
                    return ServiceResult.Failure("Service not found.", ServiceResultStatus.NotFound);
                }

                existingService.ServiceNumber = model.ServiceNumber;
                existingService.ServiceName = model.ServiceName;

                var auditTrail = new MsapAuditTrail(username, $"Updated Service #{model.ServiceNumber}", "Service");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Service updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Service {ServiceId}", model.ServiceId);
                return ServiceResult.Failure($"Failed to update service: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var service = await unitOfWork.MsapService.GetAsync(s => s.ServiceId == id, cancellationToken);
                if (service == null)
                {
                    return ServiceResult.Failure("Service not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.MsapService.RemoveAsync(service, cancellationToken);

                var auditTrail = new MsapAuditTrail(username, $"Deleted Service #{service.ServiceNumber}", "Service");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Service deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Service {ServiceId}", id);
                return ServiceResult.Failure($"Failed to delete service: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
