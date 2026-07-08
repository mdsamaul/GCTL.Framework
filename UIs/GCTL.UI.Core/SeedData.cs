using GCTL.Core.Common;
using GCTL.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace GCTL.UI.Core;

/// <summary>
/// Seeds default roles and one admin account so you can sign in immediately
/// after first run, without needing the Sign Up page.
///
/// CHANGE THE DEFAULT PASSWORD BELOW before deploying anywhere real.
/// </summary>
public static class SeedData
{
    public const string AdminRole = "Admin";
    public const string TeamLeaderRole = "TeamLeader";
    public const string UserRole = "User";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in new[] { AdminRole, TeamLeaderRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName, Description = roleName });
            }
        }

        const string adminEmail = "admin@gctl.local";
        const string adminPassword = "Admin@123"; // CHANGE THIS before real use

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                AccessCode = AccessCodes.Admin,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }
        }
    }
}
