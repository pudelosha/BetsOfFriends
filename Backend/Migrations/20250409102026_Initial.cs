using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShortName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LongName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageId);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    LocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ISOCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.LocationId);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredefinedTournaments",
                columns: table => new
                {
                    TournamentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalTournamentId = table.Column<int>(type: "int", nullable: true),
                    ExternalSeasonId = table.Column<int>(type: "int", nullable: true),
                    Season = table.Column<int>(type: "int", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TournamentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Update = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedTournaments", x => x.TournamentId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportMessages",
                columns: table => new
                {
                    SupportMessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportMessages", x => x.SupportMessageId);
                    table.ForeignKey(
                        name: "FK_SupportMessages_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MemberSince = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    LanguageId = table.Column<int>(type: "int", nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HintsVisible = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedRegulations = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedMarketingConsent = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveEmailMatchClosed = table.Column<bool>(type: "bit", nullable: false),
                    ReceivePushMatchClosed = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveEmailDailyUpdates = table.Column<bool>(type: "bit", nullable: false),
                    ReceivePushDailyUpdates = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveEmailTournamentInvitation = table.Column<bool>(type: "bit", nullable: false),
                    ReceivePushTournamentInvitation = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveEmailPendingBets = table.Column<bool>(type: "bit", nullable: false),
                    ReceivePushPendingBets = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveEmailNewGames = table.Column<bool>(type: "bit", nullable: false),
                    ReceivePushNewGames = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveEmailSpecialOffers = table.Column<bool>(type: "bit", nullable: false),
                    ReceivePushSpecialOffers = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PredefinedMatchStages",
                columns: table => new
                {
                    StageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedMatchStages", x => x.StageId);
                    table.ForeignKey(
                        name: "FK_PredefinedMatchStages_PredefinedTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "PredefinedTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PredefinedTeams",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalTeamId = table.Column<int>(type: "int", nullable: true),
                    TeamName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PredefinedTournamentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedTeams", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_PredefinedTeams_PredefinedTournaments_PredefinedTournamentId",
                        column: x => x.PredefinedTournamentId,
                        principalTable: "PredefinedTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomTournaments",
                columns: table => new
                {
                    TournamentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PredefinedTournamentId = table.Column<int>(type: "int", nullable: true),
                    Season = table.Column<int>(type: "int", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PublicName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllowExactResultBonus = table.Column<bool>(type: "bit", nullable: false),
                    ExactResultBonusCalculation = table.Column<int>(type: "int", nullable: false),
                    ExactResultBonus = table.Column<int>(type: "int", nullable: true),
                    AllowWhoQualifiesBets = table.Column<bool>(type: "bit", nullable: false),
                    AllowBetsWithBooster = table.Column<bool>(type: "bit", nullable: false),
                    MaxBetBooster = table.Column<int>(type: "int", nullable: false),
                    TotalBoosterPool = table.Column<int>(type: "int", nullable: true),
                    AllowNonSubmittedBetsPenalty = table.Column<bool>(type: "bit", nullable: false),
                    NonSubmittedBetPenalty = table.Column<int>(type: "int", nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Update = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTournaments", x => x.TournamentId);
                    table.ForeignKey(
                        name: "FK_CustomTournaments_PredefinedTournaments_PredefinedTournamentId",
                        column: x => x.PredefinedTournamentId,
                        principalTable: "PredefinedTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTournaments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SentEmail = table.Column<bool>(type: "bit", nullable: false),
                    SentPush = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRecipients_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationRecipients_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PredefinedMatches",
                columns: table => new
                {
                    MatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    HomeTeamId = table.Column<int>(type: "int", nullable: false),
                    AwayTeamId = table.Column<int>(type: "int", nullable: false),
                    MatchStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HomeScoreLive = table.Column<int>(type: "int", nullable: true),
                    AwayScoreLive = table.Column<int>(type: "int", nullable: true),
                    LiveStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalMatchId = table.Column<int>(type: "int", nullable: true),
                    HomeScore = table.Column<int>(type: "int", nullable: true),
                    AwayScore = table.Column<int>(type: "int", nullable: true),
                    Qualified = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    HomeWinOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DrawOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AwayWinOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HomeQualifies = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AwayQualifies = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedMatches", x => x.MatchId);
                    table.ForeignKey(
                        name: "FK_PredefinedMatches_PredefinedMatchStages_StageId",
                        column: x => x.StageId,
                        principalTable: "PredefinedMatchStages",
                        principalColumn: "StageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PredefinedMatches_PredefinedTeams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "PredefinedTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredefinedMatches_PredefinedTeams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "PredefinedTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredefinedMatches_PredefinedTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "PredefinedTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomMatchStages",
                columns: table => new
                {
                    StageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PredefinedStageId = table.Column<int>(type: "int", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMatchStages", x => x.StageId);
                    table.ForeignKey(
                        name: "FK_CustomMatchStages_CustomTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "CustomTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomMatchStages_PredefinedMatchStages_PredefinedStageId",
                        column: x => x.PredefinedStageId,
                        principalTable: "PredefinedMatchStages",
                        principalColumn: "StageId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomTeams",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PredefinedTeamId = table.Column<int>(type: "int", nullable: true),
                    TeamName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTeams", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_CustomTeams_CustomTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "CustomTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomTeams_PredefinedTeams_PredefinedTeamId",
                        column: x => x.PredefinedTeamId,
                        principalTable: "PredefinedTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomTournamentUserAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UserAdminName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTournamentUserAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_CustomTournamentUserAssignments_CustomTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "CustomTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomTournamentUserAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomMatches",
                columns: table => new
                {
                    MatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    PredefinedMatchId = table.Column<int>(type: "int", nullable: true),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    HomeTeamId = table.Column<int>(type: "int", nullable: false),
                    AwayTeamId = table.Column<int>(type: "int", nullable: false),
                    MatchStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HomeScoreLive = table.Column<int>(type: "int", nullable: true),
                    AwayScoreLive = table.Column<int>(type: "int", nullable: true),
                    LiveStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalMatchId = table.Column<int>(type: "int", nullable: true),
                    HomeScore = table.Column<int>(type: "int", nullable: true),
                    AwayScore = table.Column<int>(type: "int", nullable: true),
                    Qualified = table.Column<int>(type: "int", nullable: true),
                    HomeWinOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DrawOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AwayWinOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HomeQualifies = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AwayQualifies = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Notifications1Sent = table.Column<bool>(type: "bit", nullable: false),
                    Notifications24Sent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMatches", x => x.MatchId);
                    table.ForeignKey(
                        name: "FK_CustomMatches_CustomMatchStages_StageId",
                        column: x => x.StageId,
                        principalTable: "CustomMatchStages",
                        principalColumn: "StageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomMatches_CustomTeams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "CustomTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomMatches_CustomTeams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "CustomTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomMatches_CustomTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "CustomTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomMatches_PredefinedMatches_PredefinedMatchId",
                        column: x => x.PredefinedMatchId,
                        principalTable: "PredefinedMatches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Bets",
                columns: table => new
                {
                    BetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BonusAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HomeGoals = table.Column<int>(type: "int", nullable: true),
                    AwayGoals = table.Column<int>(type: "int", nullable: true),
                    Qualified = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    Submitted = table.Column<bool>(type: "bit", nullable: false),
                    BasePayout = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    QualificationPayout = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExactScorePayout = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bets", x => x.BetId);
                    table.ForeignKey(
                        name: "FK_Bets_CustomMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "CustomMatches",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageId", "LongName", "ShortName" },
                values: new object[,]
                {
                    { 1, "English", "en" },
                    { 2, "Polski", "pl" },
                    { 3, "Deutsch", "de" },
                    { 4, "Français", "fr" },
                    { 5, "Español", "es" },
                    { 6, "Italiano", "it" },
                    { 7, "Português", "pt" },
                    { 8, "Dutch", "nl" },
                    { 9, "Swedish", "se" },
                    { 10, "Norge", "no" },
                    { 11, "Dansk", "dk" },
                    { 12, "Czesky/Slovacky", "cz" },
                    { 13, "Hrvatski", "hr" },
                    { 14, "Русский", "ru" },
                    { 15, "Українська", "uk" },
                    { 16, "Türkçe", "tr" },
                    { 17, "العربية", "ar" },
                    { 18, "中文", "zh" },
                    { 19, "हिन्दी", "hi" }
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "LocationId", "ISOCode", "Name" },
                values: new object[,]
                {
                    { 1, "AF", "Afghanistan" },
                    { 2, "AL", "Albania" },
                    { 3, "DZ", "Algeria" },
                    { 4, "AS", "American Samoa" },
                    { 5, "AD", "Andorra" },
                    { 6, "AO", "Angola" },
                    { 7, "AI", "Anguilla" },
                    { 8, "AQ", "Antarctica" },
                    { 9, "AG", "Antigua and Barbuda" },
                    { 10, "AR", "Argentina" },
                    { 11, "AM", "Armenia" },
                    { 12, "AW", "Aruba" },
                    { 13, "AU", "Australia" },
                    { 14, "AT", "Austria" },
                    { 15, "AZ", "Azerbaijan" },
                    { 16, "BS", "Bahamas" },
                    { 17, "BH", "Bahrain" },
                    { 18, "BD", "Bangladesh" },
                    { 19, "BB", "Barbados" },
                    { 20, "BY", "Belarus" },
                    { 21, "BE", "Belgium" },
                    { 22, "BZ", "Belize" },
                    { 23, "BJ", "Benin" },
                    { 24, "BM", "Bermuda" },
                    { 25, "BT", "Bhutan" },
                    { 26, "BO", "Bolivia, Plurinational State of" },
                    { 27, "BQ", "Bonaire, Sint Eustatius and Saba" },
                    { 28, "BA", "Bosnia and Herzegovina" },
                    { 29, "BW", "Botswana" },
                    { 30, "BV", "Bouvet Island" },
                    { 31, "BR", "Brazil" },
                    { 32, "IO", "British Indian Ocean Territory" },
                    { 33, "BN", "Brunei Darussalam" },
                    { 34, "BG", "Bulgaria" },
                    { 35, "BF", "Burkina Faso" },
                    { 36, "BI", "Burundi" },
                    { 37, "CV", "Cabo Verde" },
                    { 38, "KH", "Cambodia" },
                    { 39, "CM", "Cameroon" },
                    { 40, "CA", "Canada" },
                    { 41, "KY", "Cayman Islands" },
                    { 42, "CF", "Central African Republic" },
                    { 43, "TD", "Chad" },
                    { 44, "CL", "Chile" },
                    { 45, "CN", "China" },
                    { 46, "CX", "Christmas Island" },
                    { 47, "CC", "Cocos (Keeling) Islands" },
                    { 48, "CO", "Colombia" },
                    { 49, "KM", "Comoros" },
                    { 50, "CG", "Congo" },
                    { 51, "CD", "Congo, The Democratic Republic of the" },
                    { 52, "CK", "Cook Islands" },
                    { 53, "CR", "Costa Rica" },
                    { 54, "HR", "Croatia" },
                    { 55, "CU", "Cuba" },
                    { 56, "CW", "Curaçao" },
                    { 57, "CY", "Cyprus" },
                    { 58, "CZ", "Czechia" },
                    { 59, "CI", "Côte d'Ivoire" },
                    { 60, "DK", "Denmark" },
                    { 61, "DJ", "Djibouti" },
                    { 62, "DM", "Dominica" },
                    { 63, "DO", "Dominican Republic" },
                    { 64, "EC", "Ecuador" },
                    { 65, "EG", "Egypt" },
                    { 66, "SV", "El Salvador" },
                    { 67, "GQ", "Equatorial Guinea" },
                    { 68, "ER", "Eritrea" },
                    { 69, "EE", "Estonia" },
                    { 70, "SZ", "Eswatini" },
                    { 71, "ET", "Ethiopia" },
                    { 72, "FK", "Falkland Islands (Malvinas)" },
                    { 73, "FO", "Faroe Islands" },
                    { 74, "FJ", "Fiji" },
                    { 75, "FI", "Finland" },
                    { 76, "FR", "France" },
                    { 77, "GF", "French Guiana" },
                    { 78, "PF", "French Polynesia" },
                    { 79, "TF", "French Southern Territories" },
                    { 80, "GA", "Gabon" },
                    { 81, "GM", "Gambia" },
                    { 82, "GE", "Georgia" },
                    { 83, "DE", "Germany" },
                    { 84, "GH", "Ghana" },
                    { 85, "GI", "Gibraltar" },
                    { 86, "GR", "Greece" },
                    { 87, "GL", "Greenland" },
                    { 88, "GD", "Grenada" },
                    { 89, "GP", "Guadeloupe" },
                    { 90, "GU", "Guam" },
                    { 91, "GT", "Guatemala" },
                    { 92, "GG", "Guernsey" },
                    { 93, "GN", "Guinea" },
                    { 94, "GW", "Guinea-Bissau" },
                    { 95, "GY", "Guyana" },
                    { 96, "HT", "Haiti" },
                    { 97, "HM", "Heard Island and McDonald Islands" },
                    { 98, "VA", "Holy See (Vatican City State)" },
                    { 99, "HN", "Honduras" },
                    { 100, "HK", "Hong Kong" },
                    { 101, "HU", "Hungary" },
                    { 102, "IS", "Iceland" },
                    { 103, "IN", "India" },
                    { 104, "ID", "Indonesia" },
                    { 105, "IR", "Iran, Islamic Republic of" },
                    { 106, "IQ", "Iraq" },
                    { 107, "IE", "Ireland" },
                    { 108, "IM", "Isle of Man" },
                    { 109, "IL", "Israel" },
                    { 110, "IT", "Italy" },
                    { 111, "JM", "Jamaica" },
                    { 112, "JP", "Japan" },
                    { 113, "JE", "Jersey" },
                    { 114, "JO", "Jordan" },
                    { 115, "KZ", "Kazakhstan" },
                    { 116, "KE", "Kenya" },
                    { 117, "KI", "Kiribati" },
                    { 118, "KP", "Korea, Democratic People's Republic of" },
                    { 119, "KR", "Korea, Republic of" },
                    { 120, "KW", "Kuwait" },
                    { 121, "KG", "Kyrgyzstan" },
                    { 122, "LA", "Lao People's Democratic Republic" },
                    { 123, "LV", "Latvia" },
                    { 124, "LB", "Lebanon" },
                    { 125, "LS", "Lesotho" },
                    { 126, "LR", "Liberia" },
                    { 127, "LY", "Libya" },
                    { 128, "LI", "Liechtenstein" },
                    { 129, "LT", "Lithuania" },
                    { 130, "LU", "Luxembourg" },
                    { 131, "MO", "Macao" },
                    { 132, "MG", "Madagascar" },
                    { 133, "MW", "Malawi" },
                    { 134, "MY", "Malaysia" },
                    { 135, "MV", "Maldives" },
                    { 136, "ML", "Mali" },
                    { 137, "MT", "Malta" },
                    { 138, "MH", "Marshall Islands" },
                    { 139, "MQ", "Martinique" },
                    { 140, "MR", "Mauritania" },
                    { 141, "MU", "Mauritius" },
                    { 142, "YT", "Mayotte" },
                    { 143, "MX", "Mexico" },
                    { 144, "FM", "Micronesia, Federated States of" },
                    { 145, "MD", "Moldova, Republic of" },
                    { 146, "MC", "Monaco" },
                    { 147, "MN", "Mongolia" },
                    { 148, "ME", "Montenegro" },
                    { 149, "MS", "Montserrat" },
                    { 150, "MA", "Morocco" },
                    { 151, "MZ", "Mozambique" },
                    { 152, "MM", "Myanmar" },
                    { 153, "NA", "Namibia" },
                    { 154, "NR", "Nauru" },
                    { 155, "NP", "Nepal" },
                    { 156, "NL", "Netherlands" },
                    { 157, "NC", "New Caledonia" },
                    { 158, "NZ", "New Zealand" },
                    { 159, "NI", "Nicaragua" },
                    { 160, "NE", "Niger" },
                    { 161, "NG", "Nigeria" },
                    { 162, "NU", "Niue" },
                    { 163, "NF", "Norfolk Island" },
                    { 164, "MK", "North Macedonia" },
                    { 165, "MP", "Northern Mariana Islands" },
                    { 166, "NO", "Norway" },
                    { 167, "OM", "Oman" },
                    { 168, "PK", "Pakistan" },
                    { 169, "PW", "Palau" },
                    { 170, "PS", "Palestine, State of" },
                    { 171, "PA", "Panama" },
                    { 172, "PG", "Papua New Guinea" },
                    { 173, "PY", "Paraguay" },
                    { 174, "PE", "Peru" },
                    { 175, "PH", "Philippines" },
                    { 176, "PN", "Pitcairn" },
                    { 177, "PL", "Poland" },
                    { 178, "PT", "Portugal" },
                    { 179, "PR", "Puerto Rico" },
                    { 180, "QA", "Qatar" },
                    { 181, "RO", "Romania" },
                    { 182, "RU", "Russian Federation" },
                    { 183, "RW", "Rwanda" },
                    { 184, "RE", "Réunion" },
                    { 185, "BL", "Saint Barthélemy" },
                    { 186, "SH", "Saint Helena, Ascension and Tristan da Cunha" },
                    { 187, "KN", "Saint Kitts and Nevis" },
                    { 188, "LC", "Saint Lucia" },
                    { 189, "MF", "Saint Martin (French part)" },
                    { 190, "PM", "Saint Pierre and Miquelon" },
                    { 191, "VC", "Saint Vincent and the Grenadines" },
                    { 192, "WS", "Samoa" },
                    { 193, "SM", "San Marino" },
                    { 194, "ST", "Sao Tome and Principe" },
                    { 195, "SA", "Saudi Arabia" },
                    { 196, "SN", "Senegal" },
                    { 197, "RS", "Serbia" },
                    { 198, "SC", "Seychelles" },
                    { 199, "SL", "Sierra Leone" },
                    { 200, "SG", "Singapore" },
                    { 201, "SX", "Sint Maarten (Dutch part)" },
                    { 202, "SK", "Slovakia" },
                    { 203, "SI", "Slovenia" },
                    { 204, "SB", "Solomon Islands" },
                    { 205, "SO", "Somalia" },
                    { 206, "ZA", "South Africa" },
                    { 207, "GS", "South Georgia and the South Sandwich Islands" },
                    { 208, "SS", "South Sudan" },
                    { 209, "ES", "Spain" },
                    { 210, "LK", "Sri Lanka" },
                    { 211, "SD", "Sudan" },
                    { 212, "SR", "Suriname" },
                    { 213, "SJ", "Svalbard and Jan Mayen" },
                    { 214, "SE", "Sweden" },
                    { 215, "CH", "Switzerland" },
                    { 216, "SY", "Syrian Arab Republic" },
                    { 217, "TW", "Taiwan, Province of China" },
                    { 218, "TJ", "Tajikistan" },
                    { 219, "TZ", "Tanzania, United Republic of" },
                    { 220, "TH", "Thailand" },
                    { 221, "TL", "Timor-Leste" },
                    { 222, "TG", "Togo" },
                    { 223, "TK", "Tokelau" },
                    { 224, "TO", "Tonga" },
                    { 225, "TT", "Trinidad and Tobago" },
                    { 226, "TN", "Tunisia" },
                    { 227, "TR", "Turkey" },
                    { 228, "TM", "Turkmenistan" },
                    { 229, "TC", "Turks and Caicos Islands" },
                    { 230, "TV", "Tuvalu" },
                    { 231, "UG", "Uganda" },
                    { 232, "UA", "Ukraine" },
                    { 233, "AE", "United Arab Emirates" },
                    { 234, "GB", "United Kingdom" },
                    { 235, "US", "United States" },
                    { 236, "UM", "United States Minor Outlying Islands" },
                    { 237, "UY", "Uruguay" },
                    { 238, "UZ", "Uzbekistan" },
                    { 239, "VU", "Vanuatu" },
                    { 240, "VE", "Venezuela, Bolivarian Republic of" },
                    { 241, "VN", "Viet Nam" },
                    { 242, "VG", "Virgin Islands, British" },
                    { 243, "VI", "Virgin Islands, U.S." },
                    { 244, "WF", "Wallis and Futuna" },
                    { 245, "EH", "Western Sahara" },
                    { 246, "YE", "Yemen" },
                    { 247, "ZM", "Zambia" },
                    { 248, "ZW", "Zimbabwe" },
                    { 249, "AX", "Åland Islands" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1", null, "User", "USER" },
                    { "2", null, "Admin", "ADMIN" },
                    { "3", null, "SuperAdmin", "SUPERADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_MatchId",
                table: "Bets",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bets_UserId",
                table: "Bets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_AwayTeamId",
                table: "CustomMatches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_HomeTeamId",
                table: "CustomMatches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_PredefinedMatchId",
                table: "CustomMatches",
                column: "PredefinedMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_StageId",
                table: "CustomMatches",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_TournamentId_HomeTeamId_AwayTeamId_MatchStart",
                table: "CustomMatches",
                columns: new[] { "TournamentId", "HomeTeamId", "AwayTeamId", "MatchStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatchStages_PredefinedStageId",
                table: "CustomMatchStages",
                column: "PredefinedStageId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatchStages_TournamentId",
                table: "CustomMatchStages",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTeams_PredefinedTeamId",
                table: "CustomTeams",
                column: "PredefinedTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTeams_TournamentId",
                table: "CustomTeams",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournaments_CreatedByUserId",
                table: "CustomTournaments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournaments_PredefinedTournamentId",
                table: "CustomTournaments",
                column: "PredefinedTournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentUserAssignments_TournamentId",
                table: "CustomTournamentUserAssignments",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentUserAssignments_UserId_TournamentId",
                table: "CustomTournamentUserAssignments",
                columns: new[] { "UserId", "TournamentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_NotificationId",
                table: "NotificationRecipients",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_UserId",
                table: "NotificationRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedMatches_AwayTeamId",
                table: "PredefinedMatches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedMatches_HomeTeamId",
                table: "PredefinedMatches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedMatches_StageId",
                table: "PredefinedMatches",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedMatches_TournamentId",
                table: "PredefinedMatches",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedMatchStages_TournamentId",
                table: "PredefinedMatchStages",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedTeams_PredefinedTournamentId",
                table: "PredefinedTeams",
                column: "PredefinedTournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_LanguageId",
                table: "SupportMessages",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LanguageId",
                table: "Users",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LocationId",
                table: "Users",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bets");

            migrationBuilder.DropTable(
                name: "CustomTournamentUserAssignments");

            migrationBuilder.DropTable(
                name: "NotificationRecipients");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "SupportMessages");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "CustomMatches");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "CustomMatchStages");

            migrationBuilder.DropTable(
                name: "CustomTeams");

            migrationBuilder.DropTable(
                name: "PredefinedMatches");

            migrationBuilder.DropTable(
                name: "CustomTournaments");

            migrationBuilder.DropTable(
                name: "PredefinedMatchStages");

            migrationBuilder.DropTable(
                name: "PredefinedTeams");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PredefinedTournaments");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
