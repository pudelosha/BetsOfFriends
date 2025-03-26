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
        public DbSet<Country> Countries { get; set; }

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
                .HasOne(u => u.Country)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CountryId)
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

        private void SeedCountries(ModelBuilder builder)
        {
            builder.Entity<Country>().HasData(
                new Country { CountryId = 1, Name = "Afghanistan", ISOCode = "AF" },
                new Country { CountryId = 2, Name = "Albania", ISOCode = "AL" },
                new Country { CountryId = 3, Name = "Algeria", ISOCode = "DZ" },
                new Country { CountryId = 4, Name = "American Samoa", ISOCode = "AS" },
                new Country { CountryId = 5, Name = "Andorra", ISOCode = "AD" },
                new Country { CountryId = 6, Name = "Angola", ISOCode = "AO" },
                new Country { CountryId = 7, Name = "Anguilla", ISOCode = "AI" },
                new Country { CountryId = 8, Name = "Antarctica", ISOCode = "AQ" },
                new Country { CountryId = 9, Name = "Antigua and Barbuda", ISOCode = "AG" },
                new Country { CountryId = 10, Name = "Argentina", ISOCode = "AR" },
                new Country { CountryId = 11, Name = "Armenia", ISOCode = "AM" },
                new Country { CountryId = 12, Name = "Aruba", ISOCode = "AW" },
                new Country { CountryId = 13, Name = "Australia", ISOCode = "AU" },
                new Country { CountryId = 14, Name = "Austria", ISOCode = "AT" },
                new Country { CountryId = 15, Name = "Azerbaijan", ISOCode = "AZ" },
                new Country { CountryId = 16, Name = "Bahamas", ISOCode = "BS" },
                new Country { CountryId = 17, Name = "Bahrain", ISOCode = "BH" },
                new Country { CountryId = 18, Name = "Bangladesh", ISOCode = "BD" },
                new Country { CountryId = 19, Name = "Barbados", ISOCode = "BB" },
                new Country { CountryId = 20, Name = "Belarus", ISOCode = "BY" },
                new Country { CountryId = 21, Name = "Belgium", ISOCode = "BE" },
                new Country { CountryId = 22, Name = "Belize", ISOCode = "BZ" },
                new Country { CountryId = 23, Name = "Benin", ISOCode = "BJ" },
                new Country { CountryId = 24, Name = "Bermuda", ISOCode = "BM" },
                new Country { CountryId = 25, Name = "Bhutan", ISOCode = "BT" },
                new Country { CountryId = 26, Name = "Bolivia, Plurinational State of", ISOCode = "BO" },
                new Country { CountryId = 27, Name = "Bonaire, Sint Eustatius and Saba", ISOCode = "BQ" },
                new Country { CountryId = 28, Name = "Bosnia and Herzegovina", ISOCode = "BA" },
                new Country { CountryId = 29, Name = "Botswana", ISOCode = "BW" },
                new Country { CountryId = 30, Name = "Bouvet Island", ISOCode = "BV" },
                new Country { CountryId = 31, Name = "Brazil", ISOCode = "BR" },
                new Country { CountryId = 32, Name = "British Indian Ocean Territory", ISOCode = "IO" },
                new Country { CountryId = 33, Name = "Brunei Darussalam", ISOCode = "BN" },
                new Country { CountryId = 34, Name = "Bulgaria", ISOCode = "BG" },
                new Country { CountryId = 35, Name = "Burkina Faso", ISOCode = "BF" },
                new Country { CountryId = 36, Name = "Burundi", ISOCode = "BI" },
                new Country { CountryId = 37, Name = "Cabo Verde", ISOCode = "CV" },
                new Country { CountryId = 38, Name = "Cambodia", ISOCode = "KH" },
                new Country { CountryId = 39, Name = "Cameroon", ISOCode = "CM" },
                new Country { CountryId = 40, Name = "Canada", ISOCode = "CA" },
                new Country { CountryId = 41, Name = "Cayman Islands", ISOCode = "KY" },
                new Country { CountryId = 42, Name = "Central African Republic", ISOCode = "CF" },
                new Country { CountryId = 43, Name = "Chad", ISOCode = "TD" },
                new Country { CountryId = 44, Name = "Chile", ISOCode = "CL" },
                new Country { CountryId = 45, Name = "China", ISOCode = "CN" },
                new Country { CountryId = 46, Name = "Christmas Island", ISOCode = "CX" },
                new Country { CountryId = 47, Name = "Cocos (Keeling) Islands", ISOCode = "CC" },
                new Country { CountryId = 48, Name = "Colombia", ISOCode = "CO" },
                new Country { CountryId = 49, Name = "Comoros", ISOCode = "KM" },
                new Country { CountryId = 50, Name = "Congo", ISOCode = "CG" },
                new Country { CountryId = 51, Name = "Congo, The Democratic Republic of the", ISOCode = "CD" },
                new Country { CountryId = 52, Name = "Cook Islands", ISOCode = "CK" },
                new Country { CountryId = 53, Name = "Costa Rica", ISOCode = "CR" },
                new Country { CountryId = 54, Name = "Croatia", ISOCode = "HR" },
                new Country { CountryId = 55, Name = "Cuba", ISOCode = "CU" },
                new Country { CountryId = 56, Name = "Curaçao", ISOCode = "CW" },
                new Country { CountryId = 57, Name = "Cyprus", ISOCode = "CY" },
                new Country { CountryId = 58, Name = "Czechia", ISOCode = "CZ" },
                new Country { CountryId = 59, Name = "Côte d'Ivoire", ISOCode = "CI" },
                new Country { CountryId = 60, Name = "Denmark", ISOCode = "DK" },
                new Country { CountryId = 61, Name = "Djibouti", ISOCode = "DJ" },
                new Country { CountryId = 62, Name = "Dominica", ISOCode = "DM" },
                new Country { CountryId = 63, Name = "Dominican Republic", ISOCode = "DO" },
                new Country { CountryId = 64, Name = "Ecuador", ISOCode = "EC" },
                new Country { CountryId = 65, Name = "Egypt", ISOCode = "EG" },
                new Country { CountryId = 66, Name = "El Salvador", ISOCode = "SV" },
                new Country { CountryId = 67, Name = "Equatorial Guinea", ISOCode = "GQ" },
                new Country { CountryId = 68, Name = "Eritrea", ISOCode = "ER" },
                new Country { CountryId = 69, Name = "Estonia", ISOCode = "EE" },
                new Country { CountryId = 70, Name = "Eswatini", ISOCode = "SZ" },
                new Country { CountryId = 71, Name = "Ethiopia", ISOCode = "ET" },
                new Country { CountryId = 72, Name = "Falkland Islands (Malvinas)", ISOCode = "FK" },
                new Country { CountryId = 73, Name = "Faroe Islands", ISOCode = "FO" },
                new Country { CountryId = 74, Name = "Fiji", ISOCode = "FJ" },
                new Country { CountryId = 75, Name = "Finland", ISOCode = "FI" },
                new Country { CountryId = 76, Name = "France", ISOCode = "FR" },
                new Country { CountryId = 77, Name = "French Guiana", ISOCode = "GF" },
                new Country { CountryId = 78, Name = "French Polynesia", ISOCode = "PF" },
                new Country { CountryId = 79, Name = "French Southern Territories", ISOCode = "TF" },
                new Country { CountryId = 80, Name = "Gabon", ISOCode = "GA" },
                new Country { CountryId = 81, Name = "Gambia", ISOCode = "GM" },
                new Country { CountryId = 82, Name = "Georgia", ISOCode = "GE" },
                new Country { CountryId = 83, Name = "Germany", ISOCode = "DE" },
                new Country { CountryId = 84, Name = "Ghana", ISOCode = "GH" },
                new Country { CountryId = 85, Name = "Gibraltar", ISOCode = "GI" },
                new Country { CountryId = 86, Name = "Greece", ISOCode = "GR" },
                new Country { CountryId = 87, Name = "Greenland", ISOCode = "GL" },
                new Country { CountryId = 88, Name = "Grenada", ISOCode = "GD" },
                new Country { CountryId = 89, Name = "Guadeloupe", ISOCode = "GP" },
                new Country { CountryId = 90, Name = "Guam", ISOCode = "GU" },
                new Country { CountryId = 91, Name = "Guatemala", ISOCode = "GT" },
                new Country { CountryId = 92, Name = "Guernsey", ISOCode = "GG" },
                new Country { CountryId = 93, Name = "Guinea", ISOCode = "GN" },
                new Country { CountryId = 94, Name = "Guinea-Bissau", ISOCode = "GW" },
                new Country { CountryId = 95, Name = "Guyana", ISOCode = "GY" },
                new Country { CountryId = 96, Name = "Haiti", ISOCode = "HT" },
                new Country { CountryId = 97, Name = "Heard Island and McDonald Islands", ISOCode = "HM" },
                new Country { CountryId = 98, Name = "Holy See (Vatican City State)", ISOCode = "VA" },
                new Country { CountryId = 99, Name = "Honduras", ISOCode = "HN" },
                new Country { CountryId = 100, Name = "Hong Kong", ISOCode = "HK" },
                new Country { CountryId = 101, Name = "Hungary", ISOCode = "HU" },
                new Country { CountryId = 102, Name = "Iceland", ISOCode = "IS" },
                new Country { CountryId = 103, Name = "India", ISOCode = "IN" },
                new Country { CountryId = 104, Name = "Indonesia", ISOCode = "ID" },
                new Country { CountryId = 105, Name = "Iran, Islamic Republic of", ISOCode = "IR" },
                new Country { CountryId = 106, Name = "Iraq", ISOCode = "IQ" },
                new Country { CountryId = 107, Name = "Ireland", ISOCode = "IE" },
                new Country { CountryId = 108, Name = "Isle of Man", ISOCode = "IM" },
                new Country { CountryId = 109, Name = "Israel", ISOCode = "IL" },
                new Country { CountryId = 110, Name = "Italy", ISOCode = "IT" },
                new Country { CountryId = 111, Name = "Jamaica", ISOCode = "JM" },
                new Country { CountryId = 112, Name = "Japan", ISOCode = "JP" },
                new Country { CountryId = 113, Name = "Jersey", ISOCode = "JE" },
                new Country { CountryId = 114, Name = "Jordan", ISOCode = "JO" },
                new Country { CountryId = 115, Name = "Kazakhstan", ISOCode = "KZ" },
                new Country { CountryId = 116, Name = "Kenya", ISOCode = "KE" },
                new Country { CountryId = 117, Name = "Kiribati", ISOCode = "KI" },
                new Country { CountryId = 118, Name = "Korea, Democratic People's Republic of", ISOCode = "KP" },
                new Country { CountryId = 119, Name = "Korea, Republic of", ISOCode = "KR" },
                new Country { CountryId = 120, Name = "Kuwait", ISOCode = "KW" },
                new Country { CountryId = 121, Name = "Kyrgyzstan", ISOCode = "KG" },
                new Country { CountryId = 122, Name = "Lao People's Democratic Republic", ISOCode = "LA" },
                new Country { CountryId = 123, Name = "Latvia", ISOCode = "LV" },
                new Country { CountryId = 124, Name = "Lebanon", ISOCode = "LB" },
                new Country { CountryId = 125, Name = "Lesotho", ISOCode = "LS" },
                new Country { CountryId = 126, Name = "Liberia", ISOCode = "LR" },
                new Country { CountryId = 127, Name = "Libya", ISOCode = "LY" },
                new Country { CountryId = 128, Name = "Liechtenstein", ISOCode = "LI" },
                new Country { CountryId = 129, Name = "Lithuania", ISOCode = "LT" },
                new Country { CountryId = 130, Name = "Luxembourg", ISOCode = "LU" },
                new Country { CountryId = 131, Name = "Macao", ISOCode = "MO" },
                new Country { CountryId = 132, Name = "Madagascar", ISOCode = "MG" },
                new Country { CountryId = 133, Name = "Malawi", ISOCode = "MW" },
                new Country { CountryId = 134, Name = "Malaysia", ISOCode = "MY" },
                new Country { CountryId = 135, Name = "Maldives", ISOCode = "MV" },
                new Country { CountryId = 136, Name = "Mali", ISOCode = "ML" },
                new Country { CountryId = 137, Name = "Malta", ISOCode = "MT" },
                new Country { CountryId = 138, Name = "Marshall Islands", ISOCode = "MH" },
                new Country { CountryId = 139, Name = "Martinique", ISOCode = "MQ" },
                new Country { CountryId = 140, Name = "Mauritania", ISOCode = "MR" },
                new Country { CountryId = 141, Name = "Mauritius", ISOCode = "MU" },
                new Country { CountryId = 142, Name = "Mayotte", ISOCode = "YT" },
                new Country { CountryId = 143, Name = "Mexico", ISOCode = "MX" },
                new Country { CountryId = 144, Name = "Micronesia, Federated States of", ISOCode = "FM" },
                new Country { CountryId = 145, Name = "Moldova, Republic of", ISOCode = "MD" },
                new Country { CountryId = 146, Name = "Monaco", ISOCode = "MC" },
                new Country { CountryId = 147, Name = "Mongolia", ISOCode = "MN" },
                new Country { CountryId = 148, Name = "Montenegro", ISOCode = "ME" },
                new Country { CountryId = 149, Name = "Montserrat", ISOCode = "MS" },
                new Country { CountryId = 150, Name = "Morocco", ISOCode = "MA" },
                new Country { CountryId = 151, Name = "Mozambique", ISOCode = "MZ" },
                new Country { CountryId = 152, Name = "Myanmar", ISOCode = "MM" },
                new Country { CountryId = 153, Name = "Namibia", ISOCode = "NA" },
                new Country { CountryId = 154, Name = "Nauru", ISOCode = "NR" },
                new Country { CountryId = 155, Name = "Nepal", ISOCode = "NP" },
                new Country { CountryId = 156, Name = "Netherlands", ISOCode = "NL" },
                new Country { CountryId = 157, Name = "New Caledonia", ISOCode = "NC" },
                new Country { CountryId = 158, Name = "New Zealand", ISOCode = "NZ" },
                new Country { CountryId = 159, Name = "Nicaragua", ISOCode = "NI" },
                new Country { CountryId = 160, Name = "Niger", ISOCode = "NE" },
                new Country { CountryId = 161, Name = "Nigeria", ISOCode = "NG" },
                new Country { CountryId = 162, Name = "Niue", ISOCode = "NU" },
                new Country { CountryId = 163, Name = "Norfolk Island", ISOCode = "NF" },
                new Country { CountryId = 164, Name = "North Macedonia", ISOCode = "MK" },
                new Country { CountryId = 165, Name = "Northern Mariana Islands", ISOCode = "MP" },
                new Country { CountryId = 166, Name = "Norway", ISOCode = "NO" },
                new Country { CountryId = 167, Name = "Oman", ISOCode = "OM" },
                new Country { CountryId = 168, Name = "Pakistan", ISOCode = "PK" },
                new Country { CountryId = 169, Name = "Palau", ISOCode = "PW" },
                new Country { CountryId = 170, Name = "Palestine, State of", ISOCode = "PS" },
                new Country { CountryId = 171, Name = "Panama", ISOCode = "PA" },
                new Country { CountryId = 172, Name = "Papua New Guinea", ISOCode = "PG" },
                new Country { CountryId = 173, Name = "Paraguay", ISOCode = "PY" },
                new Country { CountryId = 174, Name = "Peru", ISOCode = "PE" },
                new Country { CountryId = 175, Name = "Philippines", ISOCode = "PH" },
                new Country { CountryId = 176, Name = "Pitcairn", ISOCode = "PN" },
                new Country { CountryId = 177, Name = "Poland", ISOCode = "PL" },
                new Country { CountryId = 178, Name = "Portugal", ISOCode = "PT" },
                new Country { CountryId = 179, Name = "Puerto Rico", ISOCode = "PR" },
                new Country { CountryId = 180, Name = "Qatar", ISOCode = "QA" },
                new Country { CountryId = 181, Name = "Romania", ISOCode = "RO" },
                new Country { CountryId = 182, Name = "Russian Federation", ISOCode = "RU" },
                new Country { CountryId = 183, Name = "Rwanda", ISOCode = "RW" },
                new Country { CountryId = 184, Name = "Réunion", ISOCode = "RE" },
                new Country { CountryId = 185, Name = "Saint Barthélemy", ISOCode = "BL" },
                new Country { CountryId = 186, Name = "Saint Helena, Ascension and Tristan da Cunha", ISOCode = "SH" },
                new Country { CountryId = 187, Name = "Saint Kitts and Nevis", ISOCode = "KN" },
                new Country { CountryId = 188, Name = "Saint Lucia", ISOCode = "LC" },
                new Country { CountryId = 189, Name = "Saint Martin (French part)", ISOCode = "MF" },
                new Country { CountryId = 190, Name = "Saint Pierre and Miquelon", ISOCode = "PM" },
                new Country { CountryId = 191, Name = "Saint Vincent and the Grenadines", ISOCode = "VC" },
                new Country { CountryId = 192, Name = "Samoa", ISOCode = "WS" },
                new Country { CountryId = 193, Name = "San Marino", ISOCode = "SM" },
                new Country { CountryId = 194, Name = "Sao Tome and Principe", ISOCode = "ST" },
                new Country { CountryId = 195, Name = "Saudi Arabia", ISOCode = "SA" },
                new Country { CountryId = 196, Name = "Senegal", ISOCode = "SN" },
                new Country { CountryId = 197, Name = "Serbia", ISOCode = "RS" },
                new Country { CountryId = 198, Name = "Seychelles", ISOCode = "SC" },
                new Country { CountryId = 199, Name = "Sierra Leone", ISOCode = "SL" },
                new Country { CountryId = 200, Name = "Singapore", ISOCode = "SG" },
                new Country { CountryId = 201, Name = "Sint Maarten (Dutch part)", ISOCode = "SX" },
                new Country { CountryId = 202, Name = "Slovakia", ISOCode = "SK" },
                new Country { CountryId = 203, Name = "Slovenia", ISOCode = "SI" },
                new Country { CountryId = 204, Name = "Solomon Islands", ISOCode = "SB" },
                new Country { CountryId = 205, Name = "Somalia", ISOCode = "SO" },
                new Country { CountryId = 206, Name = "South Africa", ISOCode = "ZA" },
                new Country { CountryId = 207, Name = "South Georgia and the South Sandwich Islands", ISOCode = "GS" },
                new Country { CountryId = 208, Name = "South Sudan", ISOCode = "SS" },
                new Country { CountryId = 209, Name = "Spain", ISOCode = "ES" },
                new Country { CountryId = 210, Name = "Sri Lanka", ISOCode = "LK" },
                new Country { CountryId = 211, Name = "Sudan", ISOCode = "SD" },
                new Country { CountryId = 212, Name = "Suriname", ISOCode = "SR" },
                new Country { CountryId = 213, Name = "Svalbard and Jan Mayen", ISOCode = "SJ" },
                new Country { CountryId = 214, Name = "Sweden", ISOCode = "SE" },
                new Country { CountryId = 215, Name = "Switzerland", ISOCode = "CH" },
                new Country { CountryId = 216, Name = "Syrian Arab Republic", ISOCode = "SY" },
                new Country { CountryId = 217, Name = "Taiwan, Province of China", ISOCode = "TW" },
                new Country { CountryId = 218, Name = "Tajikistan", ISOCode = "TJ" },
                new Country { CountryId = 219, Name = "Tanzania, United Republic of", ISOCode = "TZ" },
                new Country { CountryId = 220, Name = "Thailand", ISOCode = "TH" },
                new Country { CountryId = 221, Name = "Timor-Leste", ISOCode = "TL" },
                new Country { CountryId = 222, Name = "Togo", ISOCode = "TG" },
                new Country { CountryId = 223, Name = "Tokelau", ISOCode = "TK" },
                new Country { CountryId = 224, Name = "Tonga", ISOCode = "TO" },
                new Country { CountryId = 225, Name = "Trinidad and Tobago", ISOCode = "TT" },
                new Country { CountryId = 226, Name = "Tunisia", ISOCode = "TN" },
                new Country { CountryId = 227, Name = "Turkey", ISOCode = "TR" },
                new Country { CountryId = 228, Name = "Turkmenistan", ISOCode = "TM" },
                new Country { CountryId = 229, Name = "Turks and Caicos Islands", ISOCode = "TC" },
                new Country { CountryId = 230, Name = "Tuvalu", ISOCode = "TV" },
                new Country { CountryId = 231, Name = "Uganda", ISOCode = "UG" },
                new Country { CountryId = 232, Name = "Ukraine", ISOCode = "UA" },
                new Country { CountryId = 233, Name = "United Arab Emirates", ISOCode = "AE" },
                new Country { CountryId = 234, Name = "United Kingdom", ISOCode = "GB" },
                new Country { CountryId = 235, Name = "United States", ISOCode = "US" },
                new Country { CountryId = 236, Name = "United States Minor Outlying Islands", ISOCode = "UM" },
                new Country { CountryId = 237, Name = "Uruguay", ISOCode = "UY" },
                new Country { CountryId = 238, Name = "Uzbekistan", ISOCode = "UZ" },
                new Country { CountryId = 239, Name = "Vanuatu", ISOCode = "VU" },
                new Country { CountryId = 240, Name = "Venezuela, Bolivarian Republic of", ISOCode = "VE" },
                new Country { CountryId = 241, Name = "Viet Nam", ISOCode = "VN" },
                new Country { CountryId = 242, Name = "Virgin Islands, British", ISOCode = "VG" },
                new Country { CountryId = 243, Name = "Virgin Islands, U.S.", ISOCode = "VI" },
                new Country { CountryId = 244, Name = "Wallis and Futuna", ISOCode = "WF" },
                new Country { CountryId = 245, Name = "Western Sahara", ISOCode = "EH" },
                new Country { CountryId = 246, Name = "Yemen", ISOCode = "YE" },
                new Country { CountryId = 247, Name = "Zambia", ISOCode = "ZM" },
                new Country { CountryId = 248, Name = "Zimbabwe", ISOCode = "ZW" },
                new Country { CountryId = 249, Name = "Åland Islands", ISOCode = "AX" }
            );
        }
    }
}
