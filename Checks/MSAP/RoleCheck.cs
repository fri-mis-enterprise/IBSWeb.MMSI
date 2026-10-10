using System.Security.Claims;
using IBS.DataAccess.MSAP.Repository.IRepository;
using IBS.Models;
using IBS.Models.MSAP.Enums;
using IBS.Models.MSAP.MasterFile;
using IBS.Services.MSAP;
using IBS.Services.MSAP.AccessControl;
using IBS.Utility.MSAP.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checks.MSAP
{
    internal static class RoleCheck
    {
        public static async Task RunAsync(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IUnitOfWork work)
        {
            ApplicationUser user = (await users.FindByIdAsync("shared-check"))!;
            Check((await users.UpdateAsync(user)).Succeeded, "Cannot normalize the shared test account.");
            Check((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded, "Cannot create the Filpride test role.");
            Check((await users.AddToRoleAsync(user, "Admin")).Succeeded, "Cannot assign the Filpride test role.");
            Check((await users.AddClaimAsync(user, new Claim("Company", "Filpride"))).Succeeded, "Cannot prepare the shared company claim.");
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Role, "Admin")], "check"));
            var permission = new UserAccess { UserId = user.Id, UserName = user.UserName, CanCreateJobOrder = true };
            await work.UserAccess.AddAsync(permission);
            await work.SaveAsync();

            Check(await HasRoleAsync(users, principal, MsapRoles.Admin), "Filpride Admin did not default to MSAP Admin.");
            Check(!await HasRoleAsync(users, principal, MsapRoles.SuperAdmin), "Filpride Admin inherited MSAP SuperAdmin.");
            Check(await HasProcedureAsync(users, work, user.Id, ProcedureEnum.CreateBilling), "Inherited MSAP Admin lacks procedure access.");
            Check((await users.RemoveFromRoleAsync(user, "Admin")).Succeeded, "Cannot prepare ordinary shared user check.");
            Check(await HasRoleAsync(users, principal, MsapRoles.User)
                && !await HasRoleAsync(users, principal, MsapRoles.Admin), "Stored role changes were ignored in favor of cookie roles.");
            Check(await HasProcedureAsync(users, work, user.Id, ProcedureEnum.CreateJobOrder)
                && !await HasProcedureAsync(users, work, user.Id, ProcedureEnum.CreateBilling),
                "The default MSAP User does not retain its procedure permissions.");
            Check((await users.AddToRoleAsync(user, "Admin")).Succeeded, "Cannot restore Filpride Admin for override check.");
            Check((await users.AddClaimAsync(user, new Claim(MsapRoles.ClaimType, MsapRoles.User))).Succeeded,
                "Cannot assign MSAP User.");
            Check(await HasRoleAsync(users, principal, MsapRoles.User), "Shared login cannot use its MSAP role.");
            Check(!await HasRoleAsync(users, principal, MsapRoles.Admin), "MSAP User inherited Filpride Admin.");
            Check(await HasProcedureAsync(users, work, user.Id, ProcedureEnum.CreateJobOrder)
                && !await HasProcedureAsync(users, work, user.Id, ProcedureEnum.CreateBilling),
                "MSAP User procedure permissions were not enforced.");

            Check((await users.ReplaceClaimAsync(user, new Claim(MsapRoles.ClaimType, MsapRoles.User),
                new Claim(MsapRoles.ClaimType, MsapRoles.Admin))).Succeeded, "Cannot assign MSAP Admin for authorization check.");
            Check(await users.IsInRoleAsync(user, "Admin"), "MSAP role editing removed the Filpride role.");
            Check((await users.GetClaimsAsync(user)).Any(claim => claim.Type == "Company" && claim.Value == "Filpride"),
                "MSAP role editing removed an unrelated shared claim.");
            Check((await users.GetClaimsAsync(user)).Count(claim => claim.Type == MsapRoles.ClaimType) == 1,
                "MSAP role editing left multiple role claims.");
            Check(await HasRoleAsync(users, principal, MsapRoles.Admin)
                && !await HasRoleAsync(users, principal, MsapRoles.SuperAdmin), "MSAP Admin boundary failed.");
            Check(await HasProcedureAsync(users, work, user.Id, ProcedureEnum.CreateBilling), "MSAP Admin lacks procedure access.");
            Check((await users.ReplaceClaimAsync(user, new Claim(MsapRoles.ClaimType, MsapRoles.Admin),
                new Claim(MsapRoles.ClaimType, MsapRoles.SuperAdmin))).Succeeded, "Cannot bootstrap MSAP SuperAdmin.");
            Check(await HasRoleAsync(users, principal, MsapRoles.SuperAdmin)
                && await HasRoleAsync(users, principal, MsapRoles.Admin, MsapRoles.SuperAdmin),
                "MSAP SuperAdmin cannot access both administration areas.");
            user.IsActive = false;
            Check((await users.UpdateAsync(user)).Succeeded, "Cannot deactivate the test account.");
            principal.Identities.First().AddClaim(new Claim(MsapRoles.ClaimType, MsapRoles.SuperAdmin));
            Check(!await HasRoleAsync(users, principal, MsapRoles.SuperAdmin)
                && !await HasProcedureAsync(users, work, user.Id, ProcedureEnum.CreateBilling),
                "An inactive account retained MSAP access through its cookie or permissions.");
            user.IsActive = true;
            Check((await users.UpdateAsync(user)).Succeeded, "Cannot reactivate the test account.");
            Check((await users.ReplaceClaimAsync(user, new Claim(MsapRoles.ClaimType, MsapRoles.SuperAdmin),
                new Claim(MsapRoles.ClaimType, MsapRoles.User))).Succeeded, "Cannot demote MSAP SuperAdmin for stale cookie check.");
            Check(!await HasRoleAsync(users, principal, MsapRoles.SuperAdmin), "A stale cookie retained SuperAdmin after demotion.");

            Check((await users.AddClaimAsync(user, new Claim(MsapRoles.ClaimType, MsapRoles.Admin))).Succeeded,
                "Cannot prepare duplicate role check.");
            Check(!await HasRoleAsync(users, principal, MsapRoles.User, MsapRoles.Admin), "Ambiguous MSAP roles granted access.");
            Check((await users.RemoveClaimAsync(user, new Claim(MsapRoles.ClaimType, MsapRoles.Admin))).Succeeded,
                "Cannot clean up duplicate role check.");
            string?[] allowed = (await new RoleService(users).GetAllRolesAsync(default)).Select(role => role.Name).ToArray();
            Check(allowed.Length == 3 && allowed.All(MsapRoles.IsValid), "MSAP role list contains other roles.");
            await work.UserAccess.RemoveAsync(permission);
            await work.SaveAsync();
            Check((await users.RemoveFromRoleAsync(user, "Admin")).Succeeded, "Cannot clean up Filpride test membership.");
            Check((await roles.DeleteAsync((await roles.FindByNameAsync("Admin"))!)).Succeeded, "Cannot clean up Filpride test role.");
            Console.WriteLine("PASS: MSAP Admin defaults, explicit role overrides, procedure permissions, shared role preservation, inactive accounts and stale cookies.");
        }

        private static async Task<bool> HasRoleAsync(UserManager<ApplicationUser> users, ClaimsPrincipal principal, params string[] roles)
        {
            var requirement = new ClaimsAuthorizationRequirement(MsapRoles.ClaimType, roles);
            var context = new AuthorizationHandlerContext([requirement], principal, null);
            // Run the default claim handler too: persisted cookie claims must not bypass the live role check.
            await requirement.HandleAsync(context);
            await new MsapRoleHandler(new RoleService(users)).HandleAsync(context);
            return context.HasSucceeded;
        }

        private static Task<bool> HasProcedureAsync(UserManager<ApplicationUser> users, IUnitOfWork work, string userId, ProcedureEnum procedure)
        {
            return new UserAccessService(work, users, new RoleService(users), NullLogger<UserAccessService>.Instance)
                .CheckAccess(userId, procedure);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
