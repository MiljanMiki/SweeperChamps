using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class EmailAndDateCKConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_games_game_settings_id",
                table: "games");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_email",
                table: "users",
                sql: "email LIKE '%@%'");

            migrationBuilder.CreateIndex(
                name: "IX_games_game_settings_id",
                table: "games",
                column: "game_settings_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_valid_end_time",
                table: "games",
                sql: "end_time IS NULL OR (end_time > start_time)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_games_game_settings_id",
                table: "games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_valid_end_time",
                table: "games");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_email",
                table: "users",
                sql: "email LIKE '%@%.%'");

            migrationBuilder.CreateIndex(
                name: "IX_games_game_settings_id",
                table: "games",
                column: "game_settings_id",
                unique: true);
        }
    }
}
