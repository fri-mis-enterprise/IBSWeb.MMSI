using IBS.DataAccess.MSAP.Repository.IRepository;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Utility.MSAP.Constants;
using IBS.Utility.MSAP.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace IBS.Services.MSAP
{
    public class JobOrderService(IUnitOfWork unitOfWork, ILogger<JobOrderService> logger)
    {
        public async Task<IEnumerable<JobOrder>> GetAllJobOrdersAsync(CancellationToken cancellationToken)
        {
            var jobOrders = await unitOfWork.JobOrder.GetAllJobOrdersWithDetailsAsync(cancellationToken);
            return jobOrders;
        }

        public async Task<JobOrder?> GetJobOrderByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
        }

        public async Task<JobOrderViewModel> PopulateJobOrderViewModelAsync(JobOrderViewModel? viewModel, CancellationToken cancellationToken)
        {
            viewModel ??= new JobOrderViewModel { Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()) };

            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);

            var vessels = await unitOfWork.Vessel.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Vessels = vessels
                .OrderBy(v => v.VesselName)
                .Select(v => new SelectListItem
                {
                    Value = v.VesselId.ToString(),
                    Text = $"{v.VesselName} ({v.VesselType})"
                })
                .ToList();

            var ports = await unitOfWork.Port.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Ports = ports
                .OrderBy(p => p.PortName)
                .Select(p => new SelectListItem
                {
                    Value = p.PortId.ToString(),
                    Text = p.PortName
                })
                .ToList();

            viewModel.Terminals = viewModel.PortId != 0
                ? (await unitOfWork.Terminal.GetAllAsync(t => t.PortId == viewModel.PortId, cancellationToken: cancellationToken))
                    .OrderBy(t => t.TerminalName)
                    .Select(t => new SelectListItem
                    {
                        Value = t.TerminalId.ToString(),
                        Text = t.TerminalName
                    })
                    .ToList()
                : new List<SelectListItem>();

            return viewModel;
        }

        public virtual async Task<ServiceResult<int>> CreateJobOrderAsync(JobOrder jobOrder, string username, CancellationToken cancellationToken)
        {
            try
            {
                var guard = await GuardClosedPeriodAsync(jobOrder.Date, cancellationToken);
                if (guard != null)
                {
                    return ServiceResult<int>.Failure(guard.Message!);
                }

                var timeError = ValidateTimeRange(jobOrder.PlannedStartTime, jobOrder.PlannedEndTime);
                if (timeError != null)
                {
                    return ServiceResult<int>.Failure(timeError);
                }

                jobOrder.Status = MsapConstants.JobOrderStatus.Open;
                jobOrder.CreatedBy = username;
                jobOrder.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    jobOrder.JobOrderNumber = await unitOfWork.JobOrder.GenerateJobOrderNumber(cancellationToken);
                    await unitOfWork.JobOrder.AddAsync(jobOrder, cancellationToken);
                    await unitOfWork.SaveAsync(cancellationToken);
                    await RecordAuditAsync($"Created Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken, jobOrder.JobOrderId, jobOrder.JobOrderNumber);
                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult<int>.Success(jobOrder.JobOrderId, $"Job Order #{jobOrder.JobOrderNumber} created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Job Order");
                return ServiceResult<int>.Failure($"Failed to create Job Order: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public virtual async Task<ServiceResult> UpdateJobOrderAsync(JobOrder model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == model.JobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(jobOrder.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                if (jobOrder.Status != MsapConstants.JobOrderStatus.Open)
                {
                    return ServiceResult.Failure($"Job Order #{jobOrder.JobOrderNumber} is {jobOrder.Status.ToLower()} and cannot be edited.");
                }

                if (await unitOfWork.VesselSchedule.GetAsync(s => s.JobOrderId == model.JobOrderId &&
                    s.Status == MsapConstants.VesselScheduleStatus.Completed, cancellationToken) != null)
                {
                    return ServiceResult.Failure("The vessel booking is completed. Its planned details cannot be revised.");
                }

                if (await unitOfWork.Billing.GetAsync(b => b.JobOrderId == model.JobOrderId && b.Status == MsapConstants.BillingStatus.ForPosting, cancellationToken) != null)
                {
                    return ServiceResult.Failure($"Job Order #{jobOrder.JobOrderNumber} has an unposted billing. Please delete the billing first before editing.");
                }

                var timeError = ValidateTimeRange(model.PlannedStartTime, model.PlannedEndTime);
                if (timeError != null)
                {
                    return ServiceResult.Failure(timeError);
                }

                var old = (jobOrder.CustomerId, jobOrder.VesselId, jobOrder.PortId, jobOrder.TerminalId);

                jobOrder.Date = model.Date;
                jobOrder.CustomerId = model.CustomerId;
                jobOrder.VesselId = model.VesselId;
                jobOrder.PortId = model.PortId;
                jobOrder.TerminalId = model.TerminalId;
                jobOrder.COSNumber = model.COSNumber;
                jobOrder.VoyageNumber = model.VoyageNumber;
                jobOrder.PlannedStartTime = model.PlannedStartTime;
                jobOrder.PlannedEndTime = model.PlannedEndTime;
                jobOrder.PreferredTugboatId = model.PreferredTugboatId;
                jobOrder.RequiredTugCount = model.RequiredTugCount;
                jobOrder.Remarks = model.Remarks;
                jobOrder.EditedBy = username;
                jobOrder.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                // Cascade updates to related records
                await SyncRelatedRecordsAsync(jobOrder, old, cancellationToken);

                await RecordAuditAsync($"Edited Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken, jobOrder.JobOrderId, jobOrder.JobOrderNumber);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success($"Job Order #{jobOrder.JobOrderNumber} updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Job Order {JobOrderId}", model.JobOrderId);
                return ServiceResult.Failure($"Failed to update Job Order: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public Task<ServiceResult> CancelJobOrderAsync(int id, string username, CancellationToken ct = default)
        {
            return ChangeBookingStatusAsync(id, MsapConstants.VesselScheduleStatus.Cancelled, username, ct);
        }

        public Task<ServiceResult> CompleteBookingAsync(int id, string username, CancellationToken ct = default)
        {
            return ChangeBookingStatusAsync(id, MsapConstants.VesselScheduleStatus.Completed, username, ct);
        }

        private async Task<ServiceResult> ChangeBookingStatusAsync(int id, string status, string username, CancellationToken ct)
        {
            var result = ServiceResult.Failure("Failed to update the booking. Please try again.");
            try
            {
                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Booking actions always lock the schedule before its Job Order.
                    var schedule = await unitOfWork.VesselSchedule.GetAsync(s => s.JobOrderId == id, ct);
                    if (schedule != null)
                    {
                        schedule = await unitOfWork.VesselSchedule.GetForUpdateAsync(schedule.VesselScheduleId, ct);
                    }
                    var job = await unitOfWork.JobOrder.GetForUpdateAsync(id, ct);
                    if (job == null)
                    {
                        result = ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                        return;
                    }
                    if (job.Status == MsapConstants.JobOrderStatus.Cancelled ||
                        schedule?.Status is MsapConstants.VesselScheduleStatus.Cancelled or MsapConstants.VesselScheduleStatus.Completed)
                    {
                        result = ServiceResult.Failure("This booking is already completed or cancelled.", ServiceResultStatus.ValidationError);
                        return;
                    }
                    var tickets = (await unitOfWork.DispatchTicket.GetAllAsync(t => t.JobOrderId == id, ct)).ToList();
                    if (status == MsapConstants.VesselScheduleStatus.Cancelled)
                    {
                        var guard = await GuardClosedPeriodAsync(job.Date, ct);
                        if (guard != null)
                        {
                            result = guard;
                            return;
                        }
                        if (job.Status != MsapConstants.JobOrderStatus.Open || tickets.Any(t => t.Status != MsapConstants.DispatchTicketStatus.Deleted || t.BillingId != null)
                            || await unitOfWork.Billing.GetAsync(b => b.JobOrderId == id, ct) != null)
                        {
                            result = ServiceResult.Failure("Cannot cancel this Job Order. Resolve its active Dispatch Tickets and billing first. Posted billing must follow the reversal workflow.", ServiceResultStatus.ValidationError);
                            return;
                        }
                        job.Status = MsapConstants.JobOrderStatus.Cancelled;
                        job.EditedBy = username;
                        job.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        await RecordAuditAsync($"Cancelled Job Order #{job.JobOrderNumber}; linked booking cancelled and records retained.", username, ct, id, job.JobOrderNumber);
                    }
                    else
                    {
                        var active = tickets.Where(t => t.Status != MsapConstants.DispatchTicketStatus.Deleted).ToList();
                        if (schedule == null || active.Count == 0 || active.Any(t => t.DateLeft == null || t.TimeLeft == null || t.DateArrived == null || t.TimeArrived == null
                            || t.DateArrived.Value.ToDateTime(t.TimeArrived.Value) <= t.DateLeft.Value.ToDateTime(t.TimeLeft.Value)
                            || t.DateArrived.Value.ToDateTime(t.TimeArrived.Value) > DateTimeHelper.GetCurrentPhilippineTime()))
                        {
                            result = ServiceResult.Failure("Record valid actual start and end times for all active Dispatch Tickets before completing the vessel booking.", ServiceResultStatus.ValidationError);
                            return;
                        }
                    }
                    if (schedule != null)
                    {
                        schedule.Status = status;
                        schedule.EditedBy = username;
                        schedule.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Vessel schedule #{schedule.VesselScheduleId} marked {status} through Job Order #{job.JobOrderNumber}.", "Vessel Schedule", schedule.VesselScheduleId), ct);
                    }
                    await unitOfWork.SaveAsync(ct);
                    result = ServiceResult.Success(status == MsapConstants.VesselScheduleStatus.Cancelled
                        ? "Job Order and linked vessel booking cancelled. Records were retained."
                        : "Vessel booking completed. Billing and collection can continue through the Job Order.");
                }, ct);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to change booking status for Job Order {Id}", id);
                return ServiceResult.Failure("Failed to update the booking. Please try again.");
            }
        }

        private async Task SyncRelatedRecordsAsync(JobOrder jobOrder, (int CustomerId, int VesselId, int PortId, int TerminalId) old, CancellationToken cancellationToken)
        {
            var criticalChanged = jobOrder.CustomerId != old.CustomerId ||
                                  jobOrder.VesselId != old.VesselId ||
                                  jobOrder.PortId != old.PortId ||
                                  jobOrder.TerminalId != old.TerminalId;

            // 1. Update unbilled dispatch tickets
            var tickets = await unitOfWork.DispatchTicket.GetAllAsync(
                dt => dt.JobOrderId == jobOrder.JobOrderId &&
                      dt.Status != MsapConstants.DispatchTicketStatus.Billed &&
                      dt.Status != MsapConstants.DispatchTicketStatus.Deleted,
                cancellationToken);

            foreach (var ticket in tickets)
            {
                ticket.CustomerId = jobOrder.CustomerId;
                ticket.VesselId = jobOrder.VesselId;
                ticket.VoyageNumber = jobOrder.VoyageNumber;
                ticket.COSNumber = jobOrder.COSNumber;
                ticket.PortId = jobOrder.PortId;
                ticket.TerminalId = jobOrder.TerminalId;
                ticket.Date = jobOrder.Date;

                if (criticalChanged && ticket.Status is not MsapConstants.DispatchTicketStatus.ForTariff)
                {
                    ticket.Status = MsapConstants.DispatchTicketStatus.ForTariff;
                    ticket.DispatchRate = 0;
                    ticket.DispatchBillingAmount = 0;
                    ticket.DispatchDiscount = 0;
                    ticket.DispatchNetRevenue = 0;
                    ticket.BAFRate = 0;
                    ticket.BAFBillingAmount = 0;
                    ticket.BAFDiscount = 0;
                    ticket.BAFNetRevenue = 0;
                    ticket.TotalBilling = 0;
                    ticket.TotalNetRevenue = 0;
                    ticket.ApOtherTugs = 0;
                    ticket.ApOtherTugsIncludesVat = true;
                    ticket.TariffBy = string.Empty;
                    ticket.TariffDate = default;
                    ticket.TariffEditedBy = string.Empty;
                    ticket.TariffEditedDate = null;
                }
            }

            // 2. Update unposted/uncollected billings
            var billings = await unitOfWork.Billing.GetAllAsync(
                b => b.JobOrderId == jobOrder.JobOrderId &&
                     (b.Status == MsapConstants.BillingStatus.ForPosting || b.Status == MsapConstants.BillingStatus.ForCollection),
                cancellationToken);

            foreach (var billing in billings)
            {
                billing.CustomerId = jobOrder.CustomerId;
                billing.VesselId = jobOrder.VesselId;
                billing.VoyageNumber = jobOrder.VoyageNumber;
                billing.COSNumber = jobOrder.COSNumber;
                billing.PortId = jobOrder.PortId;
                billing.TerminalId = jobOrder.TerminalId;
                billing.Date = jobOrder.Date;
            }
        }

        public virtual async Task TryAutoCloseAsync(int jobOrderId, string username, CancellationToken cancellationToken)
        {
            try
            {
                var closedStatuses = new[] { MsapConstants.DispatchTicketStatus.Billed, MsapConstants.DispatchTicketStatus.Deleted };
                var anyUnbilled = await unitOfWork.DispatchTicket.GetAsync(
                    dt => dt.JobOrderId == jobOrderId && !closedStatuses.Contains(dt.Status),
                    cancellationToken) != null;

                if (anyUnbilled)
                {
                    return;
                }

                var jobOrder = await unitOfWork.JobOrder.GetAsync(jo => jo.JobOrderId == jobOrderId, cancellationToken);
                if (jobOrder == null || jobOrder.Status != MsapConstants.JobOrderStatus.Open)
                {
                    return;
                }

                jobOrder.Status = MsapConstants.JobOrderStatus.Closed;
                await RecordAuditAsync($"Auto-closed Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken, jobOrder.JobOrderId, jobOrder.JobOrderNumber);
                await unitOfWork.SaveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-close failed for Job Order {JobOrderId}", jobOrderId);
                throw;
            }
        }

        public async Task<(IEnumerable<JobOrder> Data, int RecordsFiltered, int TotalRecords)> GetPagedJobOrdersAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await unitOfWork.JobOrder.GetPagedJobOrdersAsync(parameters, cancellationToken);
        }

        private async Task RecordAuditAsync(string activity, string username, CancellationToken cancellationToken, int? recordId = null, string? referenceNumber = null)
        {
            var audit = new AuditTrail(username, activity, "Job Order", recordId, referenceNumber);
            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }

        public async Task<ServiceResult> AssignTugboatAsync(int jobOrderId, int tugboatId, string username, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(jobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                if (jobOrder.Status != MsapConstants.JobOrderStatus.Open ||
                    await unitOfWork.VesselSchedule.GetAsync(s => s.JobOrderId == jobOrderId &&
                        (s.Status == MsapConstants.VesselScheduleStatus.Completed || s.Status == MsapConstants.VesselScheduleStatus.Cancelled), cancellationToken) != null)
                {
                    return ServiceResult.Failure("Cannot change tug assignments for a closed, cancelled or operationally completed booking.");
                }

                var guard = await GuardClosedPeriodAsync(jobOrder.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                var tugboat = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == tugboatId, cancellationToken);
                if (tugboat == null)
                {
                    return ServiceResult.Failure("Tugboat not found.", ServiceResultStatus.NotFound);
                }

                // Check if already assigned
                bool alreadyAssigned = jobOrder.PreferredTugboatId == tugboatId || jobOrder.DispatchTickets.Any(dt => dt.TugBoatId == tugboatId);
                if (alreadyAssigned)
                {
                    return ServiceResult.Failure("Tugboat is already assigned to this Job Order.");
                }

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    jobOrder = await unitOfWork.JobOrder.GetForUpdateAsync(jobOrderId, cancellationToken);
                    var booking = await unitOfWork.VesselSchedule.GetAsync(s => s.JobOrderId == jobOrderId, cancellationToken);
                    if (jobOrder?.Status != MsapConstants.JobOrderStatus.Open ||
                        booking?.Status is MsapConstants.VesselScheduleStatus.Completed or MsapConstants.VesselScheduleStatus.Cancelled)
                    {
                        throw new InvalidOperationException("Cannot assign tugs to a closed, cancelled or operationally completed booking.");
                    }
                    if (jobOrder.PreferredTugboatId == null)
                    {
                        jobOrder.PreferredTugboatId = tugboatId;
                    }
                    else
                    {
                        // Assign as an additional tugboat by creating a pending DispatchTicket
                        var services = await unitOfWork.Service.GetAllAsync(cancellationToken: cancellationToken);
                        var service = services.FirstOrDefault();
                        if (service == null)
                        {
                            throw new InvalidOperationException("Cannot assign tugboat: no service configured.");
                        }
                        int serviceId = service.ServiceId;

                        var ticket = new DispatchTicket
                        {
                            JobOrderId = jobOrder.JobOrderId,
                            TugBoatId = tugboatId,
                            CustomerId = jobOrder.CustomerId,
                            VesselId = jobOrder.VesselId,
                            PortId = jobOrder.PortId,
                            TerminalId = jobOrder.TerminalId,
                            ServiceId = serviceId,
                            Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                            DispatchNumber = $"P{jobOrder.JobOrderId:X}T{tugboatId:X}",
                            Status = MsapConstants.DispatchTicketStatus.ForTariff,
                            CreatedBy = username,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                        };

                        await unitOfWork.DispatchTicket.AddAsync(ticket, cancellationToken);
                    }

                    await RecordAuditAsync($"Assigned tugboat {tugboat.TugboatName} to Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken, jobOrder.JobOrderId, jobOrder.JobOrderNumber);
                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success("Tugboat assigned successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning tugboat");
                return ServiceResult.Failure($"Failed to assign tugboat: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UnassignTugboatAsync(int jobOrderId, int tugboatId, string username, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(jobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                if (jobOrder.Status != MsapConstants.JobOrderStatus.Open ||
                    await unitOfWork.VesselSchedule.GetAsync(s => s.JobOrderId == jobOrderId &&
                        (s.Status == MsapConstants.VesselScheduleStatus.Completed || s.Status == MsapConstants.VesselScheduleStatus.Cancelled), cancellationToken) != null)
                {
                    return ServiceResult.Failure("Cannot change tug assignments for a closed, cancelled or operationally completed booking.");
                }

                var guard = await GuardClosedPeriodAsync(jobOrder.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                var tug = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == tugboatId, cancellationToken);
                string? tugboatName = tug?.TugboatName;

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var lockedJob = await unitOfWork.JobOrder.GetForUpdateAsync(jobOrderId, cancellationToken);
                    var booking = await unitOfWork.VesselSchedule.GetAsync(s => s.JobOrderId == jobOrderId, cancellationToken);
                    if (lockedJob?.Status != MsapConstants.JobOrderStatus.Open ||
                        booking?.Status is MsapConstants.VesselScheduleStatus.Completed or MsapConstants.VesselScheduleStatus.Cancelled)
                    {
                        throw new InvalidOperationException("Cannot unassign tugs from a closed, cancelled or operationally completed booking.");
                    }
                    if (jobOrder.PreferredTugboatId == tugboatId)
                    {
                        jobOrder.PreferredTugboatId = null;
                    }

                    var ticketToRemove = jobOrder.DispatchTickets.FirstOrDefault(dt => dt.TugBoatId == tugboatId);
                    if (ticketToRemove != null)
                    {
                        if (ticketToRemove.Status != MsapConstants.DispatchTicketStatus.ForTariff)
                        {
                            throw new InvalidOperationException("Cannot unassign a tugboat with an active or processed dispatch ticket.");
                        }

                        await unitOfWork.DispatchTicket.RemoveAsync(ticketToRemove, cancellationToken);
                    }

                    await RecordAuditAsync($"Unassigned tugboat {tugboatName ?? "Unknown"} from Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken, jobOrder.JobOrderId, jobOrder.JobOrderNumber);
                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success("Tugboat unassigned successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error unassigning tugboat");
                return ServiceResult.Failure($"Failed to unassign tugboat: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<List<SelectListItem>> GetJobOrderSelectListAsync(CancellationToken cancellationToken)
        {
            var jobOrders = await unitOfWork.JobOrder.GetAllJobOrdersWithDetailsAsync(cancellationToken);
            return jobOrders
                .Take(100)
                .Select(j => new SelectListItem
                {
                    Value = j.JobOrderId.ToString(),
                    Text = $"{j.JobOrderNumber} - {j.Vessel.VesselName}"
                })
                .ToList();
        }

        private static string? ValidateTimeRange(DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue && end <= start)
            {
                return "Planned End Time must be strictly after Planned Start Time.";
            }
            return null;
        }

        private async Task<ServiceResult?> GuardClosedPeriodAsync(DateOnly date, CancellationToken ct)
        {
            if (await unitOfWork.PostedPeriod.IsMonthClosedAsync(date.Year, date.Month, ct))
            {
                return ServiceResult.Failure($"Cannot modify: {date:MMMM yyyy} is closed.");
            }

            return null;
        }
    }
}
