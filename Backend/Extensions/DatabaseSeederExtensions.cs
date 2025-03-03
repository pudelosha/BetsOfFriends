using Backend.Model.Database;
using Backend.Model.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Extensions
{
    public static class DatabaseSeederExtensions
    {
        /// <summary>
        /// Applies pending migrations and ensures SuperAdmin exists.
        /// </summary>
        public static async Task ApplyMigrationsAndSeedSuperAdmin(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var dbContext = services.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync(); // Apply pending migrations
                await SeedSuperAdmin(services); // Ensure SuperAdmin user is created
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database migration: {ex.Message}");
            }
        }

        private static async Task SeedSuperAdmin(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string superAdminEmail = "pudel1985@gmail.com";
            string superAdminPassword = "Dupa123!";

            // Ensure roles exist
            string[] roles = { "User", "Admin", "SuperAdmin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Check if SuperAdmin user exists
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdmin == null)
            {
                var newSuperAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newSuperAdmin, superAdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newSuperAdmin, "SuperAdmin");
                    Console.WriteLine("SuperAdmin account created successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to create SuperAdmin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine("SuperAdmin already exists.");
            }
        }
    }
}
