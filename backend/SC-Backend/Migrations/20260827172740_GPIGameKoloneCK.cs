using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class GPIGameKoloneCK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duration_seconds",
                table: "games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_ranked",
                table: "games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "winning_team",
                table: "games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "accuracy",
                table: "game_players",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<short>(
                name: "elo_change",
                table: "game_players",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "outcome",
                table: "game_players",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_valid_duration_seconds",
                table: "games",
                sql: "duration_seconds IS NULL OR end_time IS NULL OR duration_seconds <= EXTRACT(EPOCH FROM(end_time - start_time))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_valid_duration_seconds",
                table: "games");

            migrationBuilder.DropColumn(
                name: "duration_seconds",
                table: "games");

            migrationBuilder.DropColumn(
                name: "is_ranked",
                table: "games");

            migrationBuilder.DropColumn(
                name: "winning_team",
                table: "games");

            migrationBuilder.DropColumn(
                name: "accuracy",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "elo_change",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "outcome",
                table: "game_players");
        }
    }
}
