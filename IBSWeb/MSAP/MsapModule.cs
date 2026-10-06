using IBS.DataAccess.MSAP.Data;
using IBS.DataAccess.MSAP.Repository;
using IBS.DataAccess.MSAP.Repository.IRepository;
using IBS.Services.MSAP;
using IBS.Services.MSAP.AccessControl;
using IBS.Utility.MSAP;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace IBSWeb.MSAP
{
    public static class MsapModule
    {
        public static IServiceCollection AddMsapModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(_ => new MsapDbContext(new DbContextOptionsBuilder<MsapDbContext>()
                .UseNpgsql(configuration.GetConnectionString("MMSIConnection"),
                    postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "msap"))
                .Options));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserAccessService, UserAccessService>();
            services.AddScoped<IAccessControlService, AccessControlService>();
            services.Configure<GCSConfigOptions>(configuration.GetSection("MSAP"));
            services.AddSingleton<ICloudStorageService>(provider =>
                provider.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                    ? ActivatorUtilities.CreateInstance<LocalFileStorageService>(provider)
                    : ActivatorUtilities.CreateInstance<CloudStorageService>(provider));
            services.AddScoped<JobOrderService>();
            services.AddScoped<DispatchTicketService>();
            services.AddScoped<BillingService>();
            services.AddScoped<CollectionService>();
            services.AddScoped<IMaritimeServiceService, MaritimeServiceService>();
            services.AddScoped<IPortService, PortService>();
            services.AddScoped<IPrincipalService, PrincipalService>();
            services.AddScoped<ITariffRateService, TariffRateService>();
            services.AddScoped<ITerminalService, TerminalService>();
            services.AddScoped<ITugMasterService, TugMasterService>();
            services.AddScoped<ITugboatOwnerService, TugboatOwnerService>();
            services.AddScoped<ITugboatService, TugboatService>();
            services.AddScoped<IVesselService, VesselService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IAuditTrailService, AuditTrailService>();
            services.AddScoped<IVesselScheduleService, VesselScheduleService>();
            services.AddScoped<SuperAdminService>();
            services.AddControllersWithViews(options => options.Filters.Add<MsapJsonResultFilter>());
            services.Configure<RazorViewEngineOptions>(options => options.ViewLocationExpanders.Add(new MsapViewLocationExpander()));
            return services;
        }

        public static async Task UseMsapModuleAsync(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                var storagePath = app.Configuration["MSAP:LocalStoragePath"] ?? "App_Data/MSAP/LocalStorage";
                var absolutePath = Path.GetFullPath(storagePath, app.Environment.ContentRootPath);
                Directory.CreateDirectory(absolutePath);
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(absolutePath),
                    RequestPath = "/msap-storage"
                });
            }

            if (app.Configuration.GetValue<bool>("MSAP:ApplyMigrations"))
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MsapDbContext>();
                await context.Database.MigrateAsync();
                await DbSeeder.SeedAsync(scope.ServiceProvider);
            }
        }
    }

    internal class MsapViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> locations)
        {
            if (context.AreaName is "MSAP" or "MSAPAdmin" or "MSAPSuperAdmin")
            {
                return locations.Where(location => !location.StartsWith("/Views/", StringComparison.Ordinal))
                    .Append("/Areas/MSAP/Views/Shared/{0}.cshtml");
            }

            return locations;
        }
    }
}
