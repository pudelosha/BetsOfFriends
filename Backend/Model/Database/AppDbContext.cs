using Backend.Model.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Model.Database
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Predefined Tournament Tables
        public DbSet<PredefinedTournament> PredefinedTournaments { get; set; }
        public DbSet<PredefinedTeam> PredefinedTeams { get; set; }
        public DbSet<PredefinedMatch> PredefinedMatches { get; set; }
        public DbSet<PredefinedMatchStage> PredefinedMatchStages { get; set; }

        // Standard Tournament Tables
        public DbSet<CustomTournament> CustomTournaments { get; set; }
        public DbSet<CustomTournamentUserAssignment> CustomTournamentUserAssignments { get; set; }
        public DbSet<CustomTeam> CustomTeams { get; set; }
        public DbSet<CustomMatch> CustomMatches { get; set; }
        public DbSet<CustomMatchStage> CustomMatchStages { get; set; }
        public DbSet<Bet> Bets { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationRecipient> NotificationRecipients { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            RenameIdentityTables(builder);
            ConfigurePredefinedTournamentRelationships(builder);
            ConfigureTournamentRelationships(builder);
            ConfigureUserTournamentRelationship(builder);
            ConfigureMatchRelationships(builder);
            ConfigureBetRelationships(builder);
            ConfigureNotificationRelationships(builder);
            SeedRoles(builder);
        }

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

        private void ConfigurePredefinedTournamentRelationships(ModelBuilder builder)
        {
            // Tournament -> Teams
            builder.Entity<PredefinedTeam>()
                .HasOne(t => t.PredefinedTournament)
                .WithMany(t => t.PredefinedTeams)
                .HasForeignKey(t => t.PredefinedTournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tournament -> Matches
            builder.Entity<PredefinedMatch>()
                .HasOne(m => m.PredefinedTournament)
                .WithMany(t => t.PredefinedMatches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascade delete

            // Tournament -> Stages
            builder.Entity<PredefinedMatchStage>()
                .HasOne(s => s.PredefinedTournament)
                .WithMany(t => t.PredefinedStages)
                .HasForeignKey(s => s.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Match -> Home Team (FK to PredefinedTeam)
            builder.Entity<PredefinedMatch>()
                .HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cycles

            // Match -> Away Team (FK to PredefinedTeam)
            builder.Entity<PredefinedMatch>()
                .HasOne(m => m.AwayTeam)
                .WithMany()
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureTournamentRelationships(ModelBuilder builder)
        {
            // Tournament -> CreatedByUser
            builder.Entity<CustomTournament>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tournament -> Teams
            builder.Entity<CustomTournament>()
                .HasMany(t => t.Teams)
                .WithOne(team => team.Tournament)
                .HasForeignKey(team => team.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tournament -> Participants
            builder.Entity<CustomTournament>()
                .HasMany(t => t.Participants)
                .WithOne(ut => ut.Tournament)
                .HasForeignKey(ut => ut.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tournament -> Stages
            builder.Entity<CustomTournament>()
                .HasMany(t => t.Stages)
                .WithOne(s => s.Tournament)
                .HasForeignKey(s => s.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureUserTournamentRelationship(ModelBuilder builder)
        {
            builder.Entity<CustomTournamentUserAssignment>()
                .HasKey(ut => ut.AssignmentId);

            builder.Entity<CustomTournamentUserAssignment>()
                .HasIndex(ut => new { ut.UserId, ut.TournamentId })
                .IsUnique();

            builder.Entity<CustomTournamentUserAssignment>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.CustomTournamentUserAssignments)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomTournamentUserAssignment>()
                .HasOne(ut => ut.Tournament)
                .WithMany(t => t.Participants)
                .HasForeignKey(ut => ut.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CustomTournamentUserAssignment>()
                .Property(ut => ut.Role)
                .HasDefaultValue(UserTournamentRole.Guest);
        }

        private void ConfigureMatchRelationships(ModelBuilder builder)
        {
            builder.Entity<CustomMatch>()
                .HasOne(g => g.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(g => g.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomMatch>()
                .HasOne(g => g.Stage)
                .WithMany()
                .HasForeignKey(g => g.StageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomMatch>()
                .HasOne(g => g.HomeTeam)
                .WithMany()
                .HasForeignKey(g => g.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomMatch>()
                .HasOne(g => g.AwayTeam)
                .WithMany()
                .HasForeignKey(g => g.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomMatch>()
                .HasIndex(m => new { m.TournamentId, m.HomeTeamId, m.AwayTeamId, m.MatchStart })
                .IsUnique();
        }

        private void ConfigureBetRelationships(ModelBuilder builder)
        {
            builder.Entity<Bet>()
                .HasOne(b => b.Match)
                .WithMany(m => m.Bets)
                .HasForeignKey(b => b.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Bet>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureNotificationRelationships(ModelBuilder builder)
        {
            // A NotificationRecipient links a user to a notification
            builder.Entity<NotificationRecipient>()
                .HasKey(nr => nr.Id);

            builder.Entity<NotificationRecipient>()
                .HasOne(nr => nr.User)
                .WithMany()
                .HasForeignKey(nr => nr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationRecipient>()
                .HasOne(nr => nr.Notification)
                .WithMany()
                .HasForeignKey(nr => nr.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationRecipient>()
                .Property(nr => nr.IsRead)
                .HasDefaultValue(false);
        }

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
