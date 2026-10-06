using System.Text.Json;
using IBS.Models.MSAP;
using IBS.Services.MSAP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.MSAP.Areas.MSAPSuperAdmin.Controllers
{
    [Area("MSAPSuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DataController(
        SuperAdminService superAdminService,
        ILogger<DataController> logger)
        : Controller
    {
        private static readonly JsonSerializerOptions JsonCamelCase = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public IActionResult Index(string table)
        {
            if (superAdminService.SupportedTables.Contains(table))
            {
                ViewBag.TableName = table;
                ViewBag.DisplayName = superAdminService.DisplayName(table);
                ViewBag.ColumnsJson = JsonSerializer.Serialize(superAdminService.GetColumns(table), JsonCamelCase);
                ViewBag.FieldsJson =
                    JsonSerializer.Serialize(superAdminService.GetEditableFields(table), JsonCamelCase);
                ViewBag.IdColumn = superAdminService.IdColumn(table);
                ViewBag.ReferenceColumn = superAdminService.ReferenceColumn(table);

                return View();
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetData(string table, [FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                // Extract sort column and direction from DataTables request
                var sortColumn = parameters.Order?.Count > 0 && parameters.Columns?.Count > 0
                    ? parameters.Columns[parameters.Order[0].Column].Data
                    : null;
                var sortDir = parameters.Order?.Count > 0
                    ? parameters.Order[0].Dir
                    : "asc";

                var (data, total) = await superAdminService.GetDataAsync(
                    table, parameters.Start, parameters.Length,
                    parameters.Search?.Value,
                    sortColumn, sortDir,
                    cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = total,
                    recordsFiltered = total,
                    data
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetData failed for {Table}", table);
                return Json(new { draw = parameters.Draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecord(string table, int id, CancellationToken cancellationToken)
        {
            var record = await superAdminService.GetRecordAsync(table, id, cancellationToken);
            return record == null ? Json(new { success = false, message = "Record not found." }) : Json(new { success = true, data = record });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string table, int id,
            [FromForm] Dictionary<string, string> changes,
            [FromForm] string remarks,
            CancellationToken cancellationToken)
        {
            var username = User.Identity?.Name ?? "SuperAdmin";
            var result = await superAdminService.SaveAsync(table, id, changes, remarks, username, cancellationToken);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetLookup(string key, CancellationToken cancellationToken)
        {
            var items = await superAdminService.GetLookupAsync(key, cancellationToken);
            return Json(items);
        }
    }

}
