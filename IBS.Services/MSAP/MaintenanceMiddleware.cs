using IBS.DataAccess.Data;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IBS.Services
{
    public class MaintenanceMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // Get the DbContext (or use your repository/unit of work)
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check Maintenance Mode (update table/field as necessary)
                var isMaintenanceMode = await dbContext.AppSettings
                    .Where(s => s.SettingKey == AppSettingKey.MaintenanceMode)
                    .Select(s => s.Value == "true")
                    .FirstOrDefaultAsync();

                if (isMaintenanceMode && !context.Request.Path.StartsWithSegments("/User/Home/Maintenance"))
                {
                    context.Response.Redirect("/User/Home/Maintenance");
                    return;
                }
            }

            await next(context);
        }
    }
}
