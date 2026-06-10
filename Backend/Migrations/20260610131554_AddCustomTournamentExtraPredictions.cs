using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomTournamentExtraPredictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomTournamentExtraPredictions",
                columns: table => new
                {
                    PredictionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WinnerTeamId = table.Column<int>(type: "int", nullable: true),
                    SecondPlaceTeamId = table.Column<int>(type: "int", nullable: true),
                    ThirdPlaceTeamId = table.Column<int>(type: "int", nullable: true),
                    TopScorerTeamId = table.Column<int>(type: "int", nullable: true),
                    TopScorerName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTournamentExtraPredictions", x => x.PredictionId);
                    table.ForeignKey(
                        name: "FK_CustomTournamentExtraPredictions_CustomTeams_SecondPlaceTeamId",
                        column: x => x.SecondPlaceTeamId,
                        principalTable: "CustomTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTournamentExtraPredictions_CustomTeams_ThirdPlaceTeamId",
                        column: x => x.ThirdPlaceTeamId,
                        principalTable: "CustomTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTournamentExtraPredictions_CustomTeams_TopScorerTeamId",
                        column: x => x.TopScorerTeamId,
                        principalTable: "CustomTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTournamentExtraPredictions_CustomTeams_WinnerTeamId",
                        column: x => x.WinnerTeamId,
                        principalTable: "CustomTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTournamentExtraPredictions_CustomTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "CustomTournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomTournamentExtraPredictions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentExtraPredictions_SecondPlaceTeamId",
                table: "CustomTournamentExtraPredictions",
                column: "SecondPlaceTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentExtraPredictions_ThirdPlaceTeamId",
                table: "CustomTournamentExtraPredictions",
                column: "ThirdPlaceTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentExtraPredictions_TopScorerTeamId",
                table: "CustomTournamentExtraPredictions",
                column: "TopScorerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentExtraPredictions_TournamentId_UserId",
                table: "CustomTournamentExtraPredictions",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentExtraPredictions_UserId",
                table: "CustomTournamentExtraPredictions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTournamentExtraPredictions_WinnerTeamId",
                table: "CustomTournamentExtraPredictions",
                column: "WinnerTeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomTournamentExtraPredictions");
        }
    }
}
