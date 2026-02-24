using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class ELORatingInPredefinedTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "betsoffriends_db_admin");

            migrationBuilder.RenameTable(
                name: "TournamentMessages",
                newName: "TournamentMessages",
                newSchema: "betsoffriends_db_admin");

            migrationBuilder.RenameTable(
                name: "PrivateMessages",
                newName: "PrivateMessages",
                newSchema: "betsoffriends_db_admin");

            migrationBuilder.AddColumn<bool>(
                name: "CalculateBetsWithHomeAdvantage",
                table: "PredefinedTournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EloRating",
                table: "PredefinedTeams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeWinOdds",
                table: "PredefinedMatches",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeQualifies",
                table: "PredefinedMatches",
                type: "decimal(6,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DrawOdds",
                table: "PredefinedMatches",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayWinOdds",
                table: "PredefinedMatches",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayQualifies",
                table: "PredefinedMatches",
                type: "decimal(6,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CalculateBetsWithHomeAdvantage",
                table: "CustomTournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EloRating",
                table: "CustomTeams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeWinOdds",
                table: "CustomMatches",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeQualifies",
                table: "CustomMatches",
                type: "decimal(6,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DrawOdds",
                table: "CustomMatches",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayWinOdds",
                table: "CustomMatches",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayQualifies",
                table: "CustomMatches",
                type: "decimal(6,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalculateBetsWithHomeAdvantage",
                table: "PredefinedTournaments");

            migrationBuilder.DropColumn(
                name: "EloRating",
                table: "PredefinedTeams");

            migrationBuilder.DropColumn(
                name: "CalculateBetsWithHomeAdvantage",
                table: "CustomTournaments");

            migrationBuilder.DropColumn(
                name: "EloRating",
                table: "CustomTeams");

            migrationBuilder.RenameTable(
                name: "TournamentMessages",
                schema: "betsoffriends_db_admin",
                newName: "TournamentMessages");

            migrationBuilder.RenameTable(
                name: "PrivateMessages",
                schema: "betsoffriends_db_admin",
                newName: "PrivateMessages");

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeWinOdds",
                table: "PredefinedMatches",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeQualifies",
                table: "PredefinedMatches",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DrawOdds",
                table: "PredefinedMatches",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayWinOdds",
                table: "PredefinedMatches",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayQualifies",
                table: "PredefinedMatches",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeWinOdds",
                table: "CustomMatches",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HomeQualifies",
                table: "CustomMatches",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DrawOdds",
                table: "CustomMatches",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayWinOdds",
                table: "CustomMatches",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AwayQualifies",
                table: "CustomMatches",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldNullable: true);
        }
    }
}
