using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_settings",
                columns: table => new
                {
                    game_settings_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    number_of_mines = table.Column<int>(type: "integer", nullable: false),
                    start_time_seconds = table.Column<int>(type: "integer", nullable: true),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    win_condition = table.Column<string>(type: "text", nullable: false),
                    has_powerups = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("game_settings_pkey", x => x.game_settings_id);
                    table.CheckConstraint("CK_game_settings_height", "height >= 9 AND height <= 50");
                    table.CheckConstraint("CK_game_settings_mines_fit", "number_of_mines < (width * height)");
                    table.CheckConstraint("CK_game_settings_number_of_mines", "number_of_mines >= 10 AND number_of_mines <= 500");
                    table.CheckConstraint("CK_game_settings_start_time_seconds", "start_time_seconds IS NULL OR (start_time_seconds >= 30 AND start_time_seconds <= 1200)");
                    table.CheckConstraint("CK_game_settings_team_size", "team_size >= 1 AND team_size <= 3");
                    table.CheckConstraint("CK_game_settings_width", "width >= 9 AND width <= 50 ");
                    table.CheckConstraint("CK_game_settings_win_condition", "win_condition IN ('Race','TimeRush')");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    users_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    datecreated = table.Column<DateOnly>(type: "date", nullable: false),
                    elo = table.Column<short>(type: "smallint", nullable: true, defaultValue: (short)0),
                    user_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValueSql: "'NotSet'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.users_id);
                    table.CheckConstraint("CK_users_elo", "elo >=0 AND elo <=  32767");
                    table.CheckConstraint("CK_users_email", "email LIKE '%@%.%'");
                    table.CheckConstraint("CK_users_user_role", "user_role IN ('NotSet','User','Admin')");
                });

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    games_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying", nullable: false),
                    game_settings_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("games_pkey", x => x.games_id);
                    table.ForeignKey(
                        name: "FK_games_game_settings_game_settings_id",
                        column: x => x.game_settings_id,
                        principalTable: "game_settings",
                        principalColumn: "game_settings_id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "game_players",
                columns: table => new
                {
                    game_players_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    game_id = table.Column<int>(type: "integer", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    team_color = table.Column<string>(type: "character varying", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("game_players_pkey", x => x.game_players_id);
                    table.CheckConstraint("CK_game_players_score", "score >= 0");
                    table.CheckConstraint("CK_game_players_team_color", "team_color IN ('Red','Blue')");
                    table.ForeignKey(
                        name: "gamePlayers_gameId_fkey",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "games_id");
                    table.ForeignKey(
                        name: "gamePlayers_playerId_fkey",
                        column: x => x.player_id,
                        principalTable: "users",
                        principalColumn: "users_id");
                });

            migrationBuilder.CreateTable(
                name: "moves",
                columns: table => new
                {
                    moves_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    game_id = table.Column<int>(type: "integer", nullable: false),
                    move_log = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("moves_pkey", x => x.moves_id);
                    table.ForeignKey(
                        name: "moves_gameid_fkey",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "games_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_players_game_id",
                table: "game_players",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_players_player_id",
                table: "game_players",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_settings_template_lookup",
                table: "game_settings",
                columns: new[] { "win_condition", "width", "height", "number_of_mines" });

            migrationBuilder.CreateIndex(
                name: "IX_games_game_settings_id",
                table: "games",
                column: "game_settings_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moves_game_id",
                table: "moves",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_stats_user_id",
                table: "user_stats",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_username_key",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_players");

            migrationBuilder.DropTable(
                name: "moves");

            migrationBuilder.DropTable(
                name: "user_stats");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "game_settings");
        }
    }
}
