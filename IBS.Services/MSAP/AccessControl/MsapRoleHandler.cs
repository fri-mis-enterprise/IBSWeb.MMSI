using System.Security.Claims;
using IBS.Utility.MSAP.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace IBS.Services.MSAP.AccessControl
{
    public class MsapRoleHandler : AuthorizationHandler<ClaimsAuthorizationRequirement>
    {
        private readonly IRoleService _roleService;

        public MsapRoleHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, ClaimsAuthorizationRequirement requirement)
        {
            if (requirement.ClaimType != MsapRoles.ClaimType)
            {
                return;
            }

            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? role = context.User.Identity?.IsAuthenticated == true && userId is not null
                ? await _roleService.GetUserRoleAsync(userId)
                : null;
            if (role is not null && requirement.AllowedValues!.Contains(role))
            {
                context.Succeed(requirement);
            }
            else
            {
                // Reject stale cookie claims after a role change or account deactivation.
                context.Fail();
            }
        }
    }
}
