using IBS.Models.MSAP;
using IBS.Models.MSAP.Enums;
using IBS.Services.MSAP;
using IBS.Services.MSAP.AccessControl;
using IBS.Utility.MSAP.Constants;

namespace Checks.MSAP
{
    internal static class JobProgressCheck
    {
        public static void Run()
        {
            var job = new JobOrder { JobOrderId = 1, Status = MsapConstants.JobOrderStatus.Open };
            var schedule = new VesselSchedule { VesselScheduleId = 1, Status = "Tentative" };
            Check(JobProgressCalculator.Calculate(null, schedule, [], []).Stage == 0, "Incomplete booking must remain in Planning.");
            schedule.CustomerId = schedule.VesselId = schedule.PortId = schedule.TerminalId = 1;
            schedule.PlannedStart = new DateTime(2030, 1, 1, 8, 0, 0);
            schedule.PlannedEnd = schedule.PlannedStart.AddHours(2);
            schedule.AssignedTugboatIds = "[1]";
            Check(JobProgressCalculator.Calculate(null, schedule, [], []).Stage == 1, "Prepared booking must require Confirmation.");
            Check(JobProgressCalculator.Calculate(job, null, [], []).Stage == 2, "Direct Job Order must start at Service.");
            var first = Ticket(1, "For Billing");
            var second = Ticket(2, "For Tariff");
            var deleted = new DispatchTicket { DispatchTicketId = 3, Status = "Deleted" };
            Check(JobProgressCalculator.Calculate(job, null, [first, second, deleted], []).Stage == 3, "One unfinished tariff must block approved tickets; deleted tickets must be excluded.");
            second.TimeArrived = null;
            Check(JobProgressCalculator.Calculate(job, null, [first, second], []).Stage == 2, "Missing actual times must block tariff progression.");
            second = Ticket(2, "Disapproved");
            var rejected = JobProgressCalculator.Calculate(job, null, [first, second], []);
            Check(rejected.Stage == 3 && rejected.ActionName == "EditTariff", "Disapproved charges must require correction before billing.");
            second.Status = "For Approval";
            Check(JobProgressCalculator.Calculate(job, null, [first, second], []).Stage == 4, "Outstanding approval must block billing.");
            second.Status = "For Billing";
            Check(JobProgressCalculator.Calculate(job, null, [first, second], []).Stage == 5, "All approved tickets must advance to Billing.");
            var bill = new Billing { MsapBillingId = 1, Status = "For Posting", Balance = 100 };
            first.BillingId = 1;
            Check(JobProgressCalculator.Calculate(job, null, [first, second], [bill]).Stage == 5, "An unreserved approved ticket must block posting.");
            second.BillingId = 1;
            Check(JobProgressCalculator.Calculate(job, null, [first, second], [bill]).Stage == 6, "Reserved tickets must advance to Posting.");
            first.Status = second.Status = "Billed";
            bill.Status = "For Collection";
            var split = new Billing { MsapBillingId = 2, Status = "For Posting", Balance = 25 };
            Check(JobProgressCalculator.Calculate(job, null, [first, second], [bill, split]).Stage == 6, "Every split billing must be posted before Collection.");
            split.Status = "For Collection";
            var collection = JobProgressCalculator.Calculate(job, null, [first, second], [bill, split]);
            Check(collection.Stage == 7 && collection.Billings.Count == 2 && collection.Guidance.Contains("125.00", StringComparison.Ordinal), "Collection must aggregate every outstanding billing.");
            bill.Status = "Collected";
            bill.Balance = 0;
            Check(JobProgressCalculator.Calculate(job, null, [first, second], [bill, split]).Stage == 7, "One paid bill must not complete a partially collected job.");
            split.Status = "Collected";
            split.Balance = 0;
            Check(JobProgressCalculator.Calculate(job, schedule, [first, second], [bill, split]).Stage == 8, "All linked bills must be collected to finish.");
            split.Status = "For Collection";
            Check(JobProgressCalculator.Calculate(job, schedule, [first, second], [bill, split]).Stage == 8, "A posted zero-balance bill must not require an impossible collection.");
            split.Status = "Collected";
            bill.Status = "For Posting";
            first.Status = second.Status = "For Billing";
            Check(JobProgressCalculator.Calculate(job, schedule, [first, second], [bill, split]).Stage == 6, "Reversal must return progress to Posting.");
            Check(JobProgressCalculator.Calculate(job, null, [deleted], [bill]).Stage == 6, "Terminal tickets must not block existing billing progression.");
            first.Status = "Unknown";
            Check(JobProgressCalculator.Calculate(job, null, [first], []).Stopped, "Unknown statuses must block progression.");
            first.Status = "Billed";
            Check(JobProgressCalculator.Calculate(job, null, [first], []).Stopped, "Missing billed references must block progression.");
            job.Status = MsapConstants.JobOrderStatus.Invalidated;
            var invalidated = JobProgressCalculator.Calculate(job, null, [], []);
            Check(invalidated.Stopped && invalidated.IsInvalidated && invalidated.Permission == null, "Invalidated jobs must retain a reference-only status without a next action.");
            job.Status = "Cancelled";
            var cancelled = JobProgressCalculator.Calculate(job, schedule, [first], []);
            Check(cancelled.Stopped && cancelled.Permission == null, "Cancelled jobs must not expose a next action.");
            Console.WriteLine("PASS: Job Progress covers planning, mixed tickets, deleted tickets, actual times, rejection, approval, billing, split posting, partial collection, reversal and cancellation.");
        }

        private static DispatchTicket Ticket(int id, string status)
        {
            return new DispatchTicket
            {
                DispatchTicketId = id,
                JobOrderId = 1,
                Status = status,
                DateLeft = new DateOnly(2026, 1, 1),
                DateArrived = new DateOnly(2026, 1, 1),
                TimeLeft = new TimeOnly(8, 0),
                TimeArrived = new TimeOnly(10, 0)
            };
        }

        private static void Check(bool passed, string message)
        {
            if (!passed)
            {
                throw new InvalidOperationException(message);
            }
        }

        internal sealed class CheckAccess : IAccessControlService
        {
            public Task<bool> HasAccessAsync(string userId, params ProcedureEnum[] procedures)
            {
                return Task.FromResult(userId == "progress-allowed");
            }

            public Task<bool> HasAnyAccessAsync(string userId, params ProcedureEnum[] procedures)
            {
                return HasAccessAsync(userId, procedures);
            }
        }
    }
}
