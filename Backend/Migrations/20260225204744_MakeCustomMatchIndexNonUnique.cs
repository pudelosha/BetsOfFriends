using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class MakeCustomMatchIndexNonUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomMatches_TournamentId_HomeTeamId_AwayTeamId_MatchStart",
                table: "CustomMatches");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_TournamentId_HomeTeamId_AwayTeamId_MatchStart",
                table: "CustomMatches",
                columns: new[] { "TournamentId", "HomeTeamId", "AwayTeamId", "MatchStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomMatches_TournamentId_HomeTeamId_AwayTeamId_MatchStart",
                table: "CustomMatches");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMatches_TournamentId_HomeTeamId_AwayTeamId_MatchStart",
                table: "CustomMatches",
                columns: new[] { "TournamentId", "HomeTeamId", "AwayTeamId", "MatchStart" },
                unique: true);
        }
    }
}
