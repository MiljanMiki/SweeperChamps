using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class DodadUserStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "gameSettings_gameId_fkey",
                table: "game_settings");

            //EF Core automatski pravi indekse za FKs kada se radi code-first. Posto sam ja prvo uradio scaffold index ne postoji
            //(jer ga nisam napravio x) ).EF coreimplicitno misli da ovaj index postoji, pa pokusava da ga izbrise, pa baca gresku
            //AKo se ovo zakomentarise radi. Reda radi je zakomentarisano i u Down() metodi
            //migrationBuilder.DropIndex(
            //    name: "IX_game_settings_game_id",
            //    table: "game_settings");

            migrationBuilder.DropColumn(
                name: "time_format",
                table: "game_settings");

            migrationBuilder.RenameColumn(
                name: "game_id",
                table: "game_settings",
                newName: "team_size");

            migrationBuilder.AlterColumn<string>(
                name: "user_role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValueSql: "'NotSet'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValueSql: "'User'::character varying");

            migrationBuilder.AddColumn<int>(
                name: "game_settings_id",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "start_time_seconds",
                table: "game_settings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "has_powerups",
                table: "game_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "win_condition",
                table: "game_settings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "user_stats",
                columns: table => new
                {
                    game_setting_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    is_ranked = table.Column<bool>(type: "boolean", nullable: false),
                    games_played = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    wins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    losses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    playtime = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_stats", x => new { x.game_setting_id, x.user_id, x.is_ranked });
                    table.CheckConstraint("CK_user_stats_games_played", "games_played >= 0");
                    table.CheckConstraint("CK_user_stats_losses", "losses >= 0");
                    table.CheckConstraint("CK_user_stats_playtime", "playtime >= 0");
                    table.CheckConstraint("CK_user_stats_valid_outcomes", "(wins + losses) <= games_played");
                    table.CheckConstraint("CK_user_stats_wins", "wins >= 0");
                    table.ForeignKey(
                        name: "FK_user_stats_game_settings_game_setting_id",
                        column: x => x.game_setting_id,
                        principalTable: "game_settings",
                        principalColumn: "game_settings_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_stats_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "users_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_elo",
                table: "users",
                sql: "elo >=0 AND elo <=  32767");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_email",
                table: "users",
                sql: "email LIKE '%@%.%'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_user_role",
                table: "users",
                sql: "user_role IN ('NotSet','User','Admin')");

            migrationBuilder.CreateIndex(
                name: "IX_games_game_settings_id",
                table: "games",
                column: "game_settings_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_settings_height",
                table: "game_settings",
                sql: "height >= 9 AND height <= 50");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_settings_mines_fit",
                table: "game_settings",
                sql: "number_of_mines < (width * height)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_settings_number_of_mines",
                table: "game_settings",
                sql: "number_of_mines >= 10 AND number_of_mines <= 500");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_settings_start_time_seconds",
                table: "game_settings",
                sql: "start_time_seconds IS NULL OR (start_time_seconds >= 30 AND start_time_seconds <= 1200)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_settings_team_size",
                table: "game_settings",
                sql: "team_size >= 1 AND team_size <= 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_settings_width",
                table: "game_settings",
                sql: "width >= 9 AND width <= 50 ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_settings_win_condition",
                table: "game_settings",
                sql: "win_condition IN ('Race','TimeRush')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_players_score",
                table: "game_players",
                sql: "score >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_players_team_color",
                table: "game_players",
                sql: "team_color IN ('Red','Blue')");

            migrationBuilder.CreateIndex(
                name: "IX_user_stats_user_id",
                table: "user_stats",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_games_game_settings_game_settings_id",
                table: "games",
                column: "game_settings_id",
                principalTable: "game_settings",
                principalColumn: "game_settings_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_games_game_settings_game_settings_id",
                table: "games");

            migrationBuilder.DropTable(
                name: "user_stats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_elo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_email",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_user_role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_games_game_settings_id",
                table: "games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_settings_height",
                table: "game_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_settings_mines_fit",
                table: "game_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_settings_number_of_mines",
                table: "game_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_settings_start_time_seconds",
                table: "game_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_settings_team_size",
                table: "game_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_settings_width",
                table: "game_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_settings_win_condition",
                table: "game_settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_players_score",
                table: "game_players");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_players_team_color",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "game_settings_id",
                table: "games");

            migrationBuilder.DropColumn(
                name: "has_powerups",
                table: "game_settings");

            migrationBuilder.DropColumn(
                name: "win_condition",
                table: "game_settings");

            migrationBuilder.RenameColumn(
                name: "team_size",
                table: "game_settings",
                newName: "game_id");

            migrationBuilder.AlterColumn<string>(
                name: "user_role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValueSql: "'User'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValueSql: "'NotSet'::character varying");

            migrationBuilder.AlterColumn<int>(
                name: "start_time_seconds",
                table: "game_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_format",
                table: "game_settings",
                type: "character varying",
                nullable: false,
                defaultValue: "");

            //migrationBuilder.CreateIndex(
            //    name: "IX_game_settings_game_id",
            //    table: "game_settings",
            //    column: "game_id");

            migrationBuilder.AddForeignKey(
                name: "gameSettings_gameId_fkey",
                table: "game_settings",
                column: "game_id",
                principalTable: "games",
                principalColumn: "games_id");
        }
    }
}
