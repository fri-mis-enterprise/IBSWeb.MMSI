using IBS.Models.MSAP;
using IBS.Models.MSAP.MasterFile;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.MSAP.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<MsapDbContext>();

            // 1. Seed Company
            if (!await context.Companies.AnyAsync(c => c.CompanyName == "MMSI"))
            {
                var company = new Company
                {
                    CompanyCode = "MMS",
                    CompanyName = "MMSI",
                    CompanyAddress = "Office Address 14th Floor Jollibee Centre, San Miguel Ave., Pasig City",
                    CompanyTin = "000-000-000-000",
                    BusinessStyle = "Maritime Services",
                    IsActive = true,
                    CreatedBy = "SYSTEM",
                    CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(8), DateTimeKind.Unspecified)
                };
                context.Companies.Add(company);
                await context.SaveChangesAsync();
            }

            // 4. Seed Terms
            var termsToSeed = new List<Terms>
            {
                new() { TermsCode = "10D", NumberOfDays = 10, NumberOfMonths = 0 },
                new() { TermsCode = "15D", NumberOfDays = 15, NumberOfMonths = 0 },
                new() { TermsCode = "15PDC", NumberOfDays = 15, NumberOfMonths = 0 },
                new() { TermsCode = "20D", NumberOfDays = 20, NumberOfMonths = 0 },
                new() { TermsCode = "21D", NumberOfDays = 21, NumberOfMonths = 0 },
                new() { TermsCode = "30D", NumberOfDays = 30, NumberOfMonths = 0 },
                new() { TermsCode = "30PDC", NumberOfDays = 30, NumberOfMonths = 0 },
                new() { TermsCode = "3D", NumberOfDays = 3, NumberOfMonths = 0 },
                new() { TermsCode = "45D", NumberOfDays = 45, NumberOfMonths = 0 },
                new() { TermsCode = "45PDC", NumberOfDays = 45, NumberOfMonths = 0 },
                new() { TermsCode = "60D", NumberOfDays = 60, NumberOfMonths = 0 },
                new() { TermsCode = "60PDC", NumberOfDays = 60, NumberOfMonths = 0 },
                new() { TermsCode = "7D", NumberOfDays = 7, NumberOfMonths = 0 },
                new() { TermsCode = "7PDC", NumberOfDays = 7, NumberOfMonths = 0 },
                new() { TermsCode = "90D", NumberOfDays = 90, NumberOfMonths = 0 },
                new() { TermsCode = "COD", NumberOfDays = 0, NumberOfMonths = 0 },
                new() { TermsCode = "M15", NumberOfDays = 15, NumberOfMonths = 1 },
                new() { TermsCode = "M29", NumberOfDays = 29, NumberOfMonths = 1 },
                new() { TermsCode = "M30", NumberOfDays = 0, NumberOfMonths = 2 },
                new() { TermsCode = "M30+1", NumberOfDays = 1, NumberOfMonths = 2 },
                new() { TermsCode = "PREPAID", NumberOfDays = 0, NumberOfMonths = 0 }
            };

            foreach (var term in termsToSeed)
            {
                if (!await context.Terms.AnyAsync(t => t.TermsCode == term.TermsCode))
                {
                    term.CreatedBy = "SYSTEM";
                    term.CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(8), DateTimeKind.Unspecified);
                    context.Terms.Add(term);
                }
            }

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
