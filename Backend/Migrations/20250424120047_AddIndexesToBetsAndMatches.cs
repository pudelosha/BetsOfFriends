using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesToBetsAndMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomMatchStages_TournamentId",
                table: "CustomMatchStages");

            migrationBuilder.DropIndex(
                name: "IX_Bets_MatchId",
                table: "Bets");

            migrationBuilder.DropIndex(
                name: "IX_Bets_UserId",
                table: "Bets");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatchStages_TournamentId_StageName",
                table: "CustomMatchStages",
                columns: new[] { "TournamentId", "StageName" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_TournamentId_StageId_Status",
                table: "CustomMatches",
                columns: new[] { "TournamentId", "StageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_TournamentId_Status_MatchStart",
                table: "CustomMatches",
                columns: new[] { "TournamentId", "Status", "MatchStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_MatchId_Calculated",
                table: "Bets",
                columns: new[] { "MatchId", "Calculated" });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_MatchId_Status",
                table: "Bets",
                columns: new[] { "MatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_MatchId_UserId",
                table: "Bets",
                columns: new[] { "MatchId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bets_UserId_Status",
                table: "Bets",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomMatchStages_TournamentId_StageName",
                table: "CustomMatchStages");

            migrationBuilder.DropIndex(
                name: "IX_CustomMatches_TournamentId_StageId_Status",
                table: "CustomMatches");

            migrationBuilder.DropIndex(
                name: "IX_CustomMatches_TournamentId_Status_MatchStart",
                table: "CustomMatches");

            migrationBuilder.DropIndex(
                name: "IX_Bets_MatchId_Calculated",
                table: "Bets");

            migrationBuilder.DropIndex(
                name: "IX_Bets_MatchId_Status",
                table: "Bets");

            migrationBuilder.DropIndex(
                name: "IX_Bets_MatchId_UserId",
                table: "Bets");

            migrationBuilder.DropIndex(
                name: "IX_Bets_UserId_Status",
                table: "Bets");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatchStages_TournamentId",
                table: "CustomMatchStages",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Bets_MatchId",
                table: "Bets",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bets_UserId",
                table: "Bets",
                column: "UserId");
        }
    }
}
