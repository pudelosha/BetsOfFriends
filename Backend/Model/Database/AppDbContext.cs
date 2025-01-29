using Backend.Model.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Model.Database
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<UserTournament> UserTournaments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            RenameIdentityTables(builder);
            ConfigureUserTournamentRelationship(builder);
            SeedRoles(builder);
        }

        /// <summary>
        /// Renames ASP.NET Identity tables for a cleaner database schema.
        /// </summary>
        private void RenameIdentityTables(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        }

        /// <summary>
        /// Define relationships between tables
        /// </summary>
        private void ConfigureUserTournamentRelationship(ModelBuilder builder)
        {
            builder.Entity<UserTournament>()
                .HasKey(ut => new { ut.UserId, ut.TournamentId });

            builder.Entity<UserTournament>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTournaments)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserTournament>()
                .HasOne(ut => ut.Tournament)
                .WithMany(t => t.UserTournaments)
                .HasForeignKey(ut => ut.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserTournament>()
                .Property(ut => ut.Role)
                .HasDefaultValue(UserTournamentRole.Guest); // Default role is Guest
        }

        /// <summary>
        /// Seeds default roles ("User", "Admin", "SuperAdmin") into the database.
        /// </summary>
        private void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "User", NormalizedName = "USER" },
                new IdentityRole { Id = "2", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "3", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" }
            );
        }
    }
}
