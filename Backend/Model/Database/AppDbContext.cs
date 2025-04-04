using Backend.Model.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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

        // Other
        public DbSet<Location> Locations { get; set; }
        public DbSet<Language> Languages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            RenameIdentityTables(builder);
            ConfigurePredefinedTournamentRelationships(builder);
            ConfigureCustomTournamentRelationships(builder);
            ConfigureUserTournamentRelationship(builder);
            ConfigureMatchRelationships(builder);
            ConfigureBetRelationships(builder);
            ConfigureNotificationRelationships(builder);
            ConfigurePredefinedReferencesInCustomEntities(builder);
            ConfigureUserRelationship(builder);
            SeedRoles(builder);
            SeedCountries(builder);
            SeedLanguages(builder);
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

        private void ConfigureCustomTournamentRelationships(ModelBuilder builder)
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
                .HasDefaultValue(UserTournamentRole.Player);
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

        private void ConfigurePredefinedReferencesInCustomEntities(ModelBuilder builder)
        {
            builder.Entity<CustomTournament>()
                .HasOne(t => t.PredefinedSource)
                .WithMany()
                .HasForeignKey(t => t.PredefinedTournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomTeam>()
                .HasOne(t => t.PredefinedSource)
                .WithMany()
                .HasForeignKey(t => t.PredefinedTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<CustomMatchStage>()
                .HasOne(s => s.PredefinedSource)
                .WithMany()
                .HasForeignKey(s => s.PredefinedStageId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<CustomMatch>()
                .HasOne(m => m.PredefinedSource)
                .WithMany()
                .HasForeignKey(m => m.PredefinedMatchId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private void ConfigureUserRelationship(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Location)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Language)
                .WithMany(l => l.Users)
                .HasForeignKey(u => u.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "User", NormalizedName = "USER" },
                new IdentityRole { Id = "2", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "3", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" }
            );
        }

        private void SeedLanguages(ModelBuilder builder)
        {
            builder.Entity<Language>().HasData(
                new Language { LanguageId = 1, ShortName = "en", LongName = "English" },
                new Language { LanguageId = 2, ShortName = "pl", LongName = "Polski" },
                new Language { LanguageId = 3, ShortName = "de", LongName = "Deutsch" },
                new Language { LanguageId = 4, ShortName = "fr", LongName = "Français" },
                new Language { LanguageId = 5, ShortName = "es", LongName = "Español" },
                new Language { LanguageId = 6, ShortName = "it", LongName = "Italiano" },
                new Language { LanguageId = 7, ShortName = "pt", LongName = "Português" },
                new Language { LanguageId = 8, ShortName = "nl", LongName = "Dutch" },
                new Language { LanguageId = 9, ShortName = "se", LongName = "Swedish" },
                new Language { LanguageId = 10, ShortName = "no", LongName = "Norge" },
                new Language { LanguageId = 11, ShortName = "dk", LongName = "Dansk" },
                new Language { LanguageId = 12, ShortName = "cz", LongName = "Czesky/Slovacky" },
                new Language { LanguageId = 13, ShortName = "hr", LongName = "Hrvatski" },
                new Language { LanguageId = 14, ShortName = "ru", LongName = "Русский" },
                new Language { LanguageId = 15, ShortName = "uk", LongName = "Українська" },
                new Language { LanguageId = 16, ShortName = "tr", LongName = "Türkçe" },
                new Language { LanguageId = 17, ShortName = "ar", LongName = "العربية" },
                new Language { LanguageId = 18, ShortName = "zh", LongName = "中文" },
                new Language { LanguageId = 19, ShortName = "hi", LongName = "हिन्दी" }
            );
        }

        private void SeedCountries(ModelBuilder builder)
        {
            builder.Entity<Location>().HasData(
                new Location { LocationId = 1, Name = "Afghanistan", ISOCode = "AF" },
                new Location { LocationId = 2, Name = "Albania", ISOCode = "AL" },
                new Location { LocationId = 3, Name = "Algeria", ISOCode = "DZ" },
                new Location { LocationId = 4, Name = "American Samoa", ISOCode = "AS" },
                new Location { LocationId = 5, Name = "Andorra", ISOCode = "AD" },
                new Location { LocationId = 6, Name = "Angola", ISOCode = "AO" },
                new Location { LocationId = 7, Name = "Anguilla", ISOCode = "AI" },
                new Location { LocationId = 8, Name = "Antarctica", ISOCode = "AQ" },
                new Location { LocationId = 9, Name = "Antigua and Barbuda", ISOCode = "AG" },
                new Location { LocationId = 10, Name = "Argentina", ISOCode = "AR" },
                new Location { LocationId = 11, Name = "Armenia", ISOCode = "AM" },
                new Location { LocationId = 12, Name = "Aruba", ISOCode = "AW" },
                new Location { LocationId = 13, Name = "Australia", ISOCode = "AU" },
                new Location { LocationId = 14, Name = "Austria", ISOCode = "AT" },
                new Location { LocationId = 15, Name = "Azerbaijan", ISOCode = "AZ" },
                new Location { LocationId = 16, Name = "Bahamas", ISOCode = "BS" },
                new Location { LocationId = 17, Name = "Bahrain", ISOCode = "BH" },
                new Location { LocationId = 18, Name = "Bangladesh", ISOCode = "BD" },
                new Location { LocationId = 19, Name = "Barbados", ISOCode = "BB" },
                new Location { LocationId = 20, Name = "Belarus", ISOCode = "BY" },
                new Location { LocationId = 21, Name = "Belgium", ISOCode = "BE" },
                new Location { LocationId = 22, Name = "Belize", ISOCode = "BZ" },
                new Location { LocationId = 23, Name = "Benin", ISOCode = "BJ" },
                new Location { LocationId = 24, Name = "Bermuda", ISOCode = "BM" },
                new Location { LocationId = 25, Name = "Bhutan", ISOCode = "BT" },
                new Location { LocationId = 26, Name = "Bolivia, Plurinational State of", ISOCode = "BO" },
                new Location { LocationId = 27, Name = "Bonaire, Sint Eustatius and Saba", ISOCode = "BQ" },
                new Location { LocationId = 28, Name = "Bosnia and Herzegovina", ISOCode = "BA" },
                new Location { LocationId = 29, Name = "Botswana", ISOCode = "BW" },
                new Location { LocationId = 30, Name = "Bouvet Island", ISOCode = "BV" },
                new Location { LocationId = 31, Name = "Brazil", ISOCode = "BR" },
                new Location { LocationId = 32, Name = "British Indian Ocean Territory", ISOCode = "IO" },
                new Location { LocationId = 33, Name = "Brunei Darussalam", ISOCode = "BN" },
                new Location { LocationId = 34, Name = "Bulgaria", ISOCode = "BG" },
                new Location { LocationId = 35, Name = "Burkina Faso", ISOCode = "BF" },
                new Location { LocationId = 36, Name = "Burundi", ISOCode = "BI" },
                new Location { LocationId = 37, Name = "Cabo Verde", ISOCode = "CV" },
                new Location { LocationId = 38, Name = "Cambodia", ISOCode = "KH" },
                new Location { LocationId = 39, Name = "Cameroon", ISOCode = "CM" },
                new Location { LocationId = 40, Name = "Canada", ISOCode = "CA" },
                new Location { LocationId = 41, Name = "Cayman Islands", ISOCode = "KY" },
                new Location { LocationId = 42, Name = "Central African Republic", ISOCode = "CF" },
                new Location { LocationId = 43, Name = "Chad", ISOCode = "TD" },
                new Location { LocationId = 44, Name = "Chile", ISOCode = "CL" },
                new Location { LocationId = 45, Name = "China", ISOCode = "CN" },
                new Location { LocationId = 46, Name = "Christmas Island", ISOCode = "CX" },
                new Location { LocationId = 47, Name = "Cocos (Keeling) Islands", ISOCode = "CC" },
                new Location { LocationId = 48, Name = "Colombia", ISOCode = "CO" },
                new Location { LocationId = 49, Name = "Comoros", ISOCode = "KM" },
                new Location { LocationId = 50, Name = "Congo", ISOCode = "CG" },
                new Location { LocationId = 51, Name = "Congo, The Democratic Republic of the", ISOCode = "CD" },
                new Location { LocationId = 52, Name = "Cook Islands", ISOCode = "CK" },
                new Location { LocationId = 53, Name = "Costa Rica", ISOCode = "CR" },
                new Location { LocationId = 54, Name = "Croatia", ISOCode = "HR" },
                new Location { LocationId = 55, Name = "Cuba", ISOCode = "CU" },
                new Location { LocationId = 56, Name = "Curaçao", ISOCode = "CW" },
                new Location { LocationId = 57, Name = "Cyprus", ISOCode = "CY" },
                new Location { LocationId = 58, Name = "Czechia", ISOCode = "CZ" },
                new Location { LocationId = 59, Name = "Côte d'Ivoire", ISOCode = "CI" },
                new Location { LocationId = 60, Name = "Denmark", ISOCode = "DK" },
                new Location { LocationId = 61, Name = "Djibouti", ISOCode = "DJ" },
                new Location { LocationId = 62, Name = "Dominica", ISOCode = "DM" },
                new Location { LocationId = 63, Name = "Dominican Republic", ISOCode = "DO" },
                new Location { LocationId = 64, Name = "Ecuador", ISOCode = "EC" },
                new Location { LocationId = 65, Name = "Egypt", ISOCode = "EG" },
                new Location { LocationId = 66, Name = "El Salvador", ISOCode = "SV" },
                new Location { LocationId = 67, Name = "Equatorial Guinea", ISOCode = "GQ" },
                new Location { LocationId = 68, Name = "Eritrea", ISOCode = "ER" },
                new Location { LocationId = 69, Name = "Estonia", ISOCode = "EE" },
                new Location { LocationId = 70, Name = "Eswatini", ISOCode = "SZ" },
                new Location { LocationId = 71, Name = "Ethiopia", ISOCode = "ET" },
                new Location { LocationId = 72, Name = "Falkland Islands (Malvinas)", ISOCode = "FK" },
                new Location { LocationId = 73, Name = "Faroe Islands", ISOCode = "FO" },
                new Location { LocationId = 74, Name = "Fiji", ISOCode = "FJ" },
                new Location { LocationId = 75, Name = "Finland", ISOCode = "FI" },
                new Location { LocationId = 76, Name = "France", ISOCode = "FR" },
                new Location { LocationId = 77, Name = "French Guiana", ISOCode = "GF" },
                new Location { LocationId = 78, Name = "French Polynesia", ISOCode = "PF" },
                new Location { LocationId = 79, Name = "French Southern Territories", ISOCode = "TF" },
                new Location { LocationId = 80, Name = "Gabon", ISOCode = "GA" },
                new Location { LocationId = 81, Name = "Gambia", ISOCode = "GM" },
                new Location { LocationId = 82, Name = "Georgia", ISOCode = "GE" },
                new Location { LocationId = 83, Name = "Germany", ISOCode = "DE" },
                new Location { LocationId = 84, Name = "Ghana", ISOCode = "GH" },
                new Location { LocationId = 85, Name = "Gibraltar", ISOCode = "GI" },
                new Location { LocationId = 86, Name = "Greece", ISOCode = "GR" },
                new Location { LocationId = 87, Name = "Greenland", ISOCode = "GL" },
                new Location { LocationId = 88, Name = "Grenada", ISOCode = "GD" },
                new Location { LocationId = 89, Name = "Guadeloupe", ISOCode = "GP" },
                new Location { LocationId = 90, Name = "Guam", ISOCode = "GU" },
                new Location { LocationId = 91, Name = "Guatemala", ISOCode = "GT" },
                new Location { LocationId = 92, Name = "Guernsey", ISOCode = "GG" },
                new Location { LocationId = 93, Name = "Guinea", ISOCode = "GN" },
                new Location { LocationId = 94, Name = "Guinea-Bissau", ISOCode = "GW" },
                new Location { LocationId = 95, Name = "Guyana", ISOCode = "GY" },
                new Location { LocationId = 96, Name = "Haiti", ISOCode = "HT" },
                new Location { LocationId = 97, Name = "Heard Island and McDonald Islands", ISOCode = "HM" },
                new Location { LocationId = 98, Name = "Holy See (Vatican City State)", ISOCode = "VA" },
                new Location { LocationId = 99, Name = "Honduras", ISOCode = "HN" },
                new Location { LocationId = 100, Name = "Hong Kong", ISOCode = "HK" },
                new Location { LocationId = 101, Name = "Hungary", ISOCode = "HU" },
                new Location { LocationId = 102, Name = "Iceland", ISOCode = "IS" },
                new Location { LocationId = 103, Name = "India", ISOCode = "IN" },
                new Location { LocationId = 104, Name = "Indonesia", ISOCode = "ID" },
                new Location { LocationId = 105, Name = "Iran, Islamic Republic of", ISOCode = "IR" },
                new Location { LocationId = 106, Name = "Iraq", ISOCode = "IQ" },
                new Location { LocationId = 107, Name = "Ireland", ISOCode = "IE" },
                new Location { LocationId = 108, Name = "Isle of Man", ISOCode = "IM" },
                new Location { LocationId = 109, Name = "Israel", ISOCode = "IL" },
                new Location { LocationId = 110, Name = "Italy", ISOCode = "IT" },
                new Location { LocationId = 111, Name = "Jamaica", ISOCode = "JM" },
                new Location { LocationId = 112, Name = "Japan", ISOCode = "JP" },
                new Location { LocationId = 113, Name = "Jersey", ISOCode = "JE" },
                new Location { LocationId = 114, Name = "Jordan", ISOCode = "JO" },
                new Location { LocationId = 115, Name = "Kazakhstan", ISOCode = "KZ" },
                new Location { LocationId = 116, Name = "Kenya", ISOCode = "KE" },
                new Location { LocationId = 117, Name = "Kiribati", ISOCode = "KI" },
                new Location { LocationId = 118, Name = "Korea, Democratic People's Republic of", ISOCode = "KP" },
                new Location { LocationId = 119, Name = "Korea, Republic of", ISOCode = "KR" },
                new Location { LocationId = 120, Name = "Kuwait", ISOCode = "KW" },
                new Location { LocationId = 121, Name = "Kyrgyzstan", ISOCode = "KG" },
                new Location { LocationId = 122, Name = "Lao People's Democratic Republic", ISOCode = "LA" },
                new Location { LocationId = 123, Name = "Latvia", ISOCode = "LV" },
                new Location { LocationId = 124, Name = "Lebanon", ISOCode = "LB" },
                new Location { LocationId = 125, Name = "Lesotho", ISOCode = "LS" },
                new Location { LocationId = 126, Name = "Liberia", ISOCode = "LR" },
                new Location { LocationId = 127, Name = "Libya", ISOCode = "LY" },
                new Location { LocationId = 128, Name = "Liechtenstein", ISOCode = "LI" },
                new Location { LocationId = 129, Name = "Lithuania", ISOCode = "LT" },
                new Location { LocationId = 130, Name = "Luxembourg", ISOCode = "LU" },
                new Location { LocationId = 131, Name = "Macao", ISOCode = "MO" },
                new Location { LocationId = 132, Name = "Madagascar", ISOCode = "MG" },
                new Location { LocationId = 133, Name = "Malawi", ISOCode = "MW" },
                new Location { LocationId = 134, Name = "Malaysia", ISOCode = "MY" },
                new Location { LocationId = 135, Name = "Maldives", ISOCode = "MV" },
                new Location { LocationId = 136, Name = "Mali", ISOCode = "ML" },
                new Location { LocationId = 137, Name = "Malta", ISOCode = "MT" },
                new Location { LocationId = 138, Name = "Marshall Islands", ISOCode = "MH" },
                new Location { LocationId = 139, Name = "Martinique", ISOCode = "MQ" },
                new Location { LocationId = 140, Name = "Mauritania", ISOCode = "MR" },
                new Location { LocationId = 141, Name = "Mauritius", ISOCode = "MU" },
                new Location { LocationId = 142, Name = "Mayotte", ISOCode = "YT" },
                new Location { LocationId = 143, Name = "Mexico", ISOCode = "MX" },
                new Location { LocationId = 144, Name = "Micronesia, Federated States of", ISOCode = "FM" },
                new Location { LocationId = 145, Name = "Moldova, Republic of", ISOCode = "MD" },
                new Location { LocationId = 146, Name = "Monaco", ISOCode = "MC" },
                new Location { LocationId = 147, Name = "Mongolia", ISOCode = "MN" },
                new Location { LocationId = 148, Name = "Montenegro", ISOCode = "ME" },
                new Location { LocationId = 149, Name = "Montserrat", ISOCode = "MS" },
                new Location { LocationId = 150, Name = "Morocco", ISOCode = "MA" },
                new Location { LocationId = 151, Name = "Mozambique", ISOCode = "MZ" },
                new Location { LocationId = 152, Name = "Myanmar", ISOCode = "MM" },
                new Location { LocationId = 153, Name = "Namibia", ISOCode = "NA" },
                new Location { LocationId = 154, Name = "Nauru", ISOCode = "NR" },
                new Location { LocationId = 155, Name = "Nepal", ISOCode = "NP" },
                new Location { LocationId = 156, Name = "Netherlands", ISOCode = "NL" },
                new Location { LocationId = 157, Name = "New Caledonia", ISOCode = "NC" },
                new Location { LocationId = 158, Name = "New Zealand", ISOCode = "NZ" },
                new Location { LocationId = 159, Name = "Nicaragua", ISOCode = "NI" },
                new Location { LocationId = 160, Name = "Niger", ISOCode = "NE" },
                new Location { LocationId = 161, Name = "Nigeria", ISOCode = "NG" },
                new Location { LocationId = 162, Name = "Niue", ISOCode = "NU" },
                new Location { LocationId = 163, Name = "Norfolk Island", ISOCode = "NF" },
                new Location { LocationId = 164, Name = "North Macedonia", ISOCode = "MK" },
                new Location { LocationId = 165, Name = "Northern Mariana Islands", ISOCode = "MP" },
                new Location { LocationId = 166, Name = "Norway", ISOCode = "NO" },
                new Location { LocationId = 167, Name = "Oman", ISOCode = "OM" },
                new Location { LocationId = 168, Name = "Pakistan", ISOCode = "PK" },
                new Location { LocationId = 169, Name = "Palau", ISOCode = "PW" },
                new Location { LocationId = 170, Name = "Palestine, State of", ISOCode = "PS" },
                new Location { LocationId = 171, Name = "Panama", ISOCode = "PA" },
                new Location { LocationId = 172, Name = "Papua New Guinea", ISOCode = "PG" },
                new Location { LocationId = 173, Name = "Paraguay", ISOCode = "PY" },
                new Location { LocationId = 174, Name = "Peru", ISOCode = "PE" },
                new Location { LocationId = 175, Name = "Philippines", ISOCode = "PH" },
                new Location { LocationId = 176, Name = "Pitcairn", ISOCode = "PN" },
                new Location { LocationId = 177, Name = "Poland", ISOCode = "PL" },
                new Location { LocationId = 178, Name = "Portugal", ISOCode = "PT" },
                new Location { LocationId = 179, Name = "Puerto Rico", ISOCode = "PR" },
                new Location { LocationId = 180, Name = "Qatar", ISOCode = "QA" },
                new Location { LocationId = 181, Name = "Romania", ISOCode = "RO" },
                new Location { LocationId = 182, Name = "Russian Federation", ISOCode = "RU" },
                new Location { LocationId = 183, Name = "Rwanda", ISOCode = "RW" },
                new Location { LocationId = 184, Name = "Réunion", ISOCode = "RE" },
                new Location { LocationId = 185, Name = "Saint Barthélemy", ISOCode = "BL" },
                new Location { LocationId = 186, Name = "Saint Helena, Ascension and Tristan da Cunha", ISOCode = "SH" },
                new Location { LocationId = 187, Name = "Saint Kitts and Nevis", ISOCode = "KN" },
                new Location { LocationId = 188, Name = "Saint Lucia", ISOCode = "LC" },
                new Location { LocationId = 189, Name = "Saint Martin (French part)", ISOCode = "MF" },
                new Location { LocationId = 190, Name = "Saint Pierre and Miquelon", ISOCode = "PM" },
                new Location { LocationId = 191, Name = "Saint Vincent and the Grenadines", ISOCode = "VC" },
                new Location { LocationId = 192, Name = "Samoa", ISOCode = "WS" },
                new Location { LocationId = 193, Name = "San Marino", ISOCode = "SM" },
                new Location { LocationId = 194, Name = "Sao Tome and Principe", ISOCode = "ST" },
                new Location { LocationId = 195, Name = "Saudi Arabia", ISOCode = "SA" },
                new Location { LocationId = 196, Name = "Senegal", ISOCode = "SN" },
                new Location { LocationId = 197, Name = "Serbia", ISOCode = "RS" },
                new Location { LocationId = 198, Name = "Seychelles", ISOCode = "SC" },
                new Location { LocationId = 199, Name = "Sierra Leone", ISOCode = "SL" },
                new Location { LocationId = 200, Name = "Singapore", ISOCode = "SG" },
                new Location { LocationId = 201, Name = "Sint Maarten (Dutch part)", ISOCode = "SX" },
                new Location { LocationId = 202, Name = "Slovakia", ISOCode = "SK" },
                new Location { LocationId = 203, Name = "Slovenia", ISOCode = "SI" },
                new Location { LocationId = 204, Name = "Solomon Islands", ISOCode = "SB" },
                new Location { LocationId = 205, Name = "Somalia", ISOCode = "SO" },
                new Location { LocationId = 206, Name = "South Africa", ISOCode = "ZA" },
                new Location { LocationId = 207, Name = "South Georgia and the South Sandwich Islands", ISOCode = "GS" },
                new Location { LocationId = 208, Name = "South Sudan", ISOCode = "SS" },
                new Location { LocationId = 209, Name = "Spain", ISOCode = "ES" },
                new Location { LocationId = 210, Name = "Sri Lanka", ISOCode = "LK" },
                new Location { LocationId = 211, Name = "Sudan", ISOCode = "SD" },
                new Location { LocationId = 212, Name = "Suriname", ISOCode = "SR" },
                new Location { LocationId = 213, Name = "Svalbard and Jan Mayen", ISOCode = "SJ" },
                new Location { LocationId = 214, Name = "Sweden", ISOCode = "SE" },
                new Location { LocationId = 215, Name = "Switzerland", ISOCode = "CH" },
                new Location { LocationId = 216, Name = "Syrian Arab Republic", ISOCode = "SY" },
                new Location { LocationId = 217, Name = "Taiwan, Province of China", ISOCode = "TW" },
                new Location { LocationId = 218, Name = "Tajikistan", ISOCode = "TJ" },
                new Location { LocationId = 219, Name = "Tanzania, United Republic of", ISOCode = "TZ" },
                new Location { LocationId = 220, Name = "Thailand", ISOCode = "TH" },
                new Location { LocationId = 221, Name = "Timor-Leste", ISOCode = "TL" },
                new Location { LocationId = 222, Name = "Togo", ISOCode = "TG" },
                new Location { LocationId = 223, Name = "Tokelau", ISOCode = "TK" },
                new Location { LocationId = 224, Name = "Tonga", ISOCode = "TO" },
                new Location { LocationId = 225, Name = "Trinidad and Tobago", ISOCode = "TT" },
                new Location { LocationId = 226, Name = "Tunisia", ISOCode = "TN" },
                new Location { LocationId = 227, Name = "Turkey", ISOCode = "TR" },
                new Location { LocationId = 228, Name = "Turkmenistan", ISOCode = "TM" },
                new Location { LocationId = 229, Name = "Turks and Caicos Islands", ISOCode = "TC" },
                new Location { LocationId = 230, Name = "Tuvalu", ISOCode = "TV" },
                new Location { LocationId = 231, Name = "Uganda", ISOCode = "UG" },
                new Location { LocationId = 232, Name = "Ukraine", ISOCode = "UA" },
                new Location { LocationId = 233, Name = "United Arab Emirates", ISOCode = "AE" },
                new Location { LocationId = 234, Name = "United Kingdom", ISOCode = "GB" },
                new Location { LocationId = 235, Name = "United States", ISOCode = "US" },
                new Location { LocationId = 236, Name = "United States Minor Outlying Islands", ISOCode = "UM" },
                new Location { LocationId = 237, Name = "Uruguay", ISOCode = "UY" },
                new Location { LocationId = 238, Name = "Uzbekistan", ISOCode = "UZ" },
                new Location { LocationId = 239, Name = "Vanuatu", ISOCode = "VU" },
                new Location { LocationId = 240, Name = "Venezuela, Bolivarian Republic of", ISOCode = "VE" },
                new Location { LocationId = 241, Name = "Viet Nam", ISOCode = "VN" },
                new Location { LocationId = 242, Name = "Virgin Islands, British", ISOCode = "VG" },
                new Location { LocationId = 243, Name = "Virgin Islands, U.S.", ISOCode = "VI" },
                new Location { LocationId = 244, Name = "Wallis and Futuna", ISOCode = "WF" },
                new Location { LocationId = 245, Name = "Western Sahara", ISOCode = "EH" },
                new Location { LocationId = 246, Name = "Yemen", ISOCode = "YE" },
                new Location { LocationId = 247, Name = "Zambia", ISOCode = "ZM" },
                new Location { LocationId = 248, Name = "Zimbabwe", ISOCode = "ZW" },
                new Location { LocationId = 249, Name = "Åland Islands", ISOCode = "AX" }
            );
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;
                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                    entity.UpdatedAt = null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = now;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
