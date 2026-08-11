using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class GameSettingsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_games_game_settings_id",
                table: "games");

            migrationBuilder.CreateIndex(
                name: "IX_games_game_settings_id",
                table: "games",
                column: "game_settings_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_settings_template_lookup",
                table: "game_settings",
                columns: new[] { "win_condition", "width", "height", "number_of_mines" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_games_game_settings_id",
                table: "games");

            migrationBuilder.DropIndex(
                name: "IX_game_settings_template_lookup",
                table: "game_settings");

            migrationBuilder.CreateIndex(
                name: "IX_games_game_settings_id",
                table: "games",
                column: "game_settings_id");
        }
    }
}
