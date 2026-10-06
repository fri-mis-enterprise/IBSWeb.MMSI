using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IBS.DataAccess.MSAP.Data
{
    public class MsapDbContextFactory : IDesignTimeDbContextFactory<MsapDbContext>
    {
        public MsapDbContext CreateDbContext(string[] args)
        {
            var connection = Environment.GetEnvironmentVariable("ConnectionStrings__MMSIConnection")
                ?? "Host=localhost;Database=mmsi_msap_design;Username=postgres";
            var options = new DbContextOptionsBuilder<MsapDbContext>()
                .UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "msap"))
                .Options;
            return new MsapDbContext(options);
        }
    }
}
