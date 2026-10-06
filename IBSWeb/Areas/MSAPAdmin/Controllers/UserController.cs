using IBS.DTOs.MSAP;
using IBS.Services.MSAP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IBSWeb.MSAP.Areas.MSAPAdmin.Controllers
{
    [Area("MSAPAdmin")]
    [Authorize(Roles = "Admin")]
    public class UserController(
        IUserService userService)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        #region API CALLS

        [HttpGet]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var userList = await userService.GetAllUsersAsync(cancellationToken);
            return Json(new { data = userList });
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { success = false, message = "Invalid user id" });
            }

            var result = await userService.GetUserByIdAsync(id, cancellationToken);
            return Json(result.IsSuccess ? new { success = true, data = result.Data } : new { success = false, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert([FromBody] UserUpsertDto model, CancellationToken cancellationToken)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid request payload" });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Missing required fields" });
            }

            var currentUsername = User.FindFirstValue(ClaimTypes.Name) ?? "System";
            var result = await userService.UpsertUserAsync(model, currentUsername, cancellationToken);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { success = false, message = "Invalid user id" });
            }

            var currentUsername = User.FindFirstValue(ClaimTypes.Name) ?? "System";
            var result = await userService.ToggleUserStatusAsync(id, currentUsername, cancellationToken);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request payload" });
            }

            var currentUsername = User.FindFirstValue(ClaimTypes.Name) ?? "System";
            var result = await userService.ResetPasswordAsync(model, currentUsername, cancellationToken);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        #endregion

        #region VIEW DATA

        [HttpGet]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var roles = await userService.GetRolesAsync(cancellationToken);
            return Json(roles);
        }

        #endregion
    }
}
