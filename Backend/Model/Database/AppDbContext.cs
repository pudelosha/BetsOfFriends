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
            ConfigureGameRelationships(builder);
            ConfigureTournamentRelationships(builder);
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
        private void ConfigureGameRelationships(ModelBuilder builder)
        {
            // Many-to-One: Game -> Tournament
            builder.Entity<Game>()
                .HasOne(g => g.Tournament)
                .WithMany(t => t.Games)
                .HasForeignKey(g => g.TournamentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascade cycles

            // Many-to-One: Game -> Home Team
            builder.Entity<Game>()
                .HasOne(g => g.HomeTeam)
                .WithMany()
                .HasForeignKey(g => g.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascade cycles

            // Many-to-One: Game -> Away Team
            builder.Entity<Game>()
                .HasOne(g => g.AwayTeam)
                .WithMany()
                .HasForeignKey(g => g.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascade cycles
        }

        private void ConfigureUserTournamentRelationship(ModelBuilder builder)
        {
            builder.Entity<UserTournament>()
                .HasKey(ut => ut.Id); // Uses `Id` instead of composite `{ UserId, TournamentId }`

            builder.Entity<UserTournament>()
                .HasIndex(ut => new { ut.UserId, ut.TournamentId }) // Unique Index (not PK) for fast lookups
                .IsUnique();

            builder.Entity<UserTournament>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTournaments)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascading delete cycles

            builder.Entity<UserTournament>()
                .HasOne(ut => ut.Tournament)
                .WithMany(t => t.Participants)
                .HasForeignKey(ut => ut.TournamentId)
                .OnDelete(DeleteBehavior.Cascade); // Deletes related UserTournaments when Tournament is deleted

            builder.Entity<UserTournament>()
                .Property(ut => ut.Role)
                .HasDefaultValue(UserTournamentRole.Guest);
        }

        private void ConfigureTournamentRelationships(ModelBuilder builder)
        {
            // Many-to-One: Tournament -> CreatedByUser
            builder.Entity<Tournament>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascading delete cycles

            // Many-to-One: Tournament -> Teams (Cascade Delete Allowed)
            builder.Entity<Tournament>()
                .HasMany(t => t.Teams)
                .WithOne(team => team.Tournament)
                .HasForeignKey(team => team.TournamentId)
                .OnDelete(DeleteBehavior.Cascade); // If Tournament is deleted, delete its Teams

            // Many-to-One: Tournament -> UserTournaments (Cascade Delete Allowed)
            builder.Entity<Tournament>()
                .HasMany(t => t.Participants)
                .WithOne(ut => ut.Tournament)
                .HasForeignKey(ut => ut.TournamentId)
                .OnDelete(DeleteBehavior.Cascade); // If Tournament is deleted, remove participants
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
