using IBS.DataAccess.MSAP.Data;
using IBS.DataAccess.MSAP.Repository.Msap.IRepository;
using IBS.Models.MSAP;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.MSAP.Repository.Msap
{
    public class VesselScheduleRepository(MsapDbContext db) : Repository<VesselSchedule>(db), IVesselScheduleRepository
    {
        private readonly MsapDbContext _db = db;

        public async Task<IEnumerable<VesselSchedule>> GetSchedulesWithDetailsAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var query = _db.MsapVesselSchedules
                .Include(s => s.Vessel)
                .Include(s => s.Port)
                .Include(s => s.Terminal)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(s => s.PlannedEnd > from.Value);

            if (to.HasValue)
                query = query.Where(s => s.PlannedStart < to.Value);

            return await query
                .OrderBy(s => s.PlannedStart)
                .ToListAsync(ct);
        }
    }
}
