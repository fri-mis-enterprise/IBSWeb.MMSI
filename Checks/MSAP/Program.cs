using System.Text.Json;
using System.Text.RegularExpressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.MSAP.Data;
using IBS.Models;
using IBS.Models.MSAP;
using IBS.Services.MSAP;
using IBSWeb.MSAP;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var webRoot = Path.GetFullPath("IBSWeb");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(MsapModule).Assembly.GetName().Name,
    ContentRootPath = webRoot,
    EnvironmentName = "Development"
});
builder.Logging.SetMinimumLevel(LogLevel.Error);
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:MMSIConnection"] = "Host=localhost;Database=msap_check;Username=postgres"
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("MMSIConnection")));
builder.Services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddScoped<IBS.DataAccess.Repository.IRepository.IUnitOfWork, IBS.DataAccess.Repository.UnitOfWork>();
builder.Services.AddMsapModule(builder.Configuration);
await using var app = builder.Build();
app.UseRouting();
#pragma warning disable ASP0014 // Populate routing without starting a server.
app.UseEndpoints(endpoints => endpoints.MapControllerRoute("default", "{area=User}/{controller=Home}/{action=Index}/{id?}"));
#pragma warning restore ASP0014
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var module = services.GetRequiredService<MsapDbContext>();
var original = services.GetRequiredService<ApplicationDbContext>();
void Check(bool passed, string message)
{
    if (!passed)
    {
        throw new InvalidOperationException(message);
    }
}

var originalTables = original.Model.GetRelationalModel().Tables.Select(t => (Schema: t.Schema ?? "public", t.Name)).ToHashSet();
var moduleTables = module.Model.GetRelationalModel().Tables;
Check(moduleTables.Where(t => t.Schema == "msap").All(t => !t.Name.StartsWith("msap_", StringComparison.Ordinal)), "An MSAP table still uses the old prefix.");
Check(moduleTables.Where(t => t.Schema == "msap").All(t => !originalTables.Contains((t.Schema!, t.Name))), "MSAP owns a base table.");
Check(module.Model.FindEntityType(typeof(AuditTrail))?.GetSchema() == "msap", "MSAP audits are not isolated.");
Check(original.Model.FindEntityType(typeof(AuditTrail)) == null, "Base context discovered MSAP audits.");
Check(module.Model.GetEntityTypes().Where(e => e.ClrType.Namespace?.StartsWith("IBS.Models.MSAP", StringComparison.Ordinal) == true)
    .All(e => e.GetSchema() == "msap"), "An MSAP entity maps outside its schema.");
var sql = module.GetService<IMigrator>().GenerateScript();
Check(!sql.Contains("CREATE TABLE public.", StringComparison.Ordinal) && !sql.Contains("ALTER TABLE public.", StringComparison.Ordinal), "MSAP migration writes shared tables.");
Check(sql.Contains("msap.audit_trails", StringComparison.Ordinal), "Audit migration is missing.");
Check(services.GetRequiredService<IBS.DataAccess.Repository.IRepository.IUnitOfWork>().GetType() == typeof(IBS.DataAccess.Repository.UnitOfWork), "Base unit of work was replaced.");
Check(services.GetRequiredService<IBS.DataAccess.MSAP.Repository.IRepository.IUnitOfWork>().GetType() == typeof(IBS.DataAccess.MSAP.Repository.UnitOfWork), "MSAP unit of work is missing.");

var actions = services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToList();
var moduleActions = actions.Where(a => a.ControllerTypeInfo.Namespace?.StartsWith("IBSWeb.MSAP.", StringComparison.Ordinal) == true).ToList();
Check(moduleActions.Select(a => a.ControllerTypeInfo).Distinct().Count() == 31, "An imported controller is missing.");
Check(moduleActions.All(a => a.RouteValues["area"] is "MSAP" or "MSAPAdmin" or "MSAPSuperAdmin"), "An MSAP controller collides with a base area.");
foreach (var controller in moduleActions.Select(a => a.ControllerTypeInfo.AsType()).Distinct())
{
    ActivatorUtilities.CreateInstance(services, controller);
}
foreach (var group in actions.Where(a => a.AttributeRouteInfo == null).GroupBy(a => (a.RouteValues["area"], a.ControllerName)))
{
    Check(group.Select(a => a.ControllerTypeInfo).Distinct().Count() == 1, $"Controller route collision: {group.Key}");
}
var links = services.GetRequiredService<LinkGenerator>();
Check(links.GetPathByAction("Index", "Home", new { area = "MSAP" })?.StartsWith("/MSAP", StringComparison.Ordinal) == true, "MSAP dashboard link is wrong.");
Check(links.GetPathByAction("GetTimeline", "AuditTrail", new { area = "MSAP", id = 1 }) == "/MSAP/AuditTrail/GetTimeline/1", "MSAP audit link is wrong.");
Check(links.GetPathByAction("Index", "Home", new { area = "User" }) != null, "Base dashboard route is missing.");
var engine = services.GetRequiredService<IRazorViewEngine>();
foreach (var area in new[] { "MSAP", "MSAPAdmin", "MSAPSuperAdmin" })
{
    foreach (var view in Directory.EnumerateFiles(Path.Combine(webRoot, "Areas", area), "*.cshtml", SearchOption.AllDirectories))
    {
        foreach (Match asset in Regex.Matches(File.ReadAllText(view), "(?:src|href)=\"([^\"]+)\""))
        {
            var url = asset.Groups[1].Value;
            if (url.StartsWith("~/msap/", StringComparison.Ordinal) || url.StartsWith("/msap/", StringComparison.Ordinal))
            {
                var path = url.TrimStart('~', '/').Split('?', '#')[0];
                Check(File.Exists(Path.Combine(webRoot, "wwwroot", path)), $"Missing MSAP asset in {view}: {url}");
            }
            if (url.StartsWith("https://", StringComparison.Ordinal) || url.StartsWith("//", StringComparison.Ordinal))
            {
                Check(!url.Contains("/msap/", StringComparison.Ordinal), $"MSAP prefix inserted into CDN URL: {url}");
            }
        }
    }
}
Console.WriteLine("PASS: MSAP view assets exist and CDN URLs have no module prefix.");
foreach (var area in new[] { "MSAP", "MSAPAdmin", "MSAPSuperAdmin" })
{
    var descriptor = moduleActions.First(a => a.RouteValues["area"] == area);
    var http = new DefaultHttpContext { RequestServices = services };
    var route = new RouteData();
    route.Values["area"] = area;
    route.Values["controller"] = descriptor.ControllerName;
    var actionContext = new ActionContext(http, route, descriptor);
    Check(engine.FindView(actionContext, "_Layout", false).View?.Path == "/Areas/MSAP/Views/Shared/_Layout.cshtml", "MSAP picked the base layout.");
}
var moduleAction = moduleActions.First();
var result = new JsonResult(new { Amount = 1.235m });
var filterContext = new ResultExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), moduleAction), [], result, new object());
new MsapJsonResultFilter().OnResultExecuting(filterContext);
Check(JsonSerializer.Serialize(result.Value, (JsonSerializerOptions)result.SerializerSettings!).Contains("1.24", StringComparison.Ordinal), "Module decimal formatting changed.");
var baseAction = actions.First(a => a.RouteValues["area"] == "User");
var baseResult = new JsonResult(new { Amount = 1.235m });
new MsapJsonResultFilter().OnResultExecuting(new ResultExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), baseAction), [], baseResult, new object()));
Check(baseResult.SerializerSettings == null, "MSAP changed base JSON options.");
Console.WriteLine($"PASS: {moduleActions.Select(a => a.ControllerTypeInfo).Distinct().Count()} controllers, routes, DI, view isolation, schema/migrations, audit ownership and scoped JSON.");
var storage = services.GetRequiredService<ICloudStorageService>();
try
{
    await storage.GetSignedUrlAsync("../outside.txt");
    throw new InvalidOperationException("MSAP storage allows escaping its directory.");
}
catch (ArgumentException)
{
}
var fileName = $"checks/{Guid.NewGuid():N}.txt";
try
{
    using var content = new MemoryStream("msap"u8.ToArray());
    await storage.UploadFileAsync(new FormFile(content, 0, content.Length, "check", "check.txt"), fileName);
    Check((await storage.GetSignedUrlAsync(fileName)).StartsWith("/msap-storage/", StringComparison.Ordinal), "MSAP attachments use the base URL.");
    await using var download = await storage.DownloadFileAsync(fileName);
    using var reader = new StreamReader(download);
    Check(await reader.ReadToEndAsync() == "msap", "MSAP attachment round-trip failed.");
}
finally
{
    await storage.DeleteFileAsync(fileName);
}
if (args.Contains("--database", StringComparer.Ordinal))
{
    await Checks.MSAP.DatabaseCheck.RunAsync(storage, services.GetRequiredService<ITempDataDictionaryFactory>());
}
