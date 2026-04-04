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
            //migrationBuilder.CreateTable(
            //    name: "games",
            //    columns: table => new
            //    {
            //        games_id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
            //        end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
            //        status = table.Column<string>(type: "character varying", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("games_pkey", x => x.games_id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "users",
            //    columns: table => new
            //    {
            //        users_id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            //        email = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
            //        datecreated = table.Column<DateOnly>(type: "date", nullable: false),
            //        elo = table.Column<short>(type: "smallint", nullable: true, defaultValue: (short)0),
            //        user_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValueSql: "'User'::character varying")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("users_pkey", x => x.users_id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "game_settings",
            //    columns: table => new
            //    {
            //        game_settings_id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        width = table.Column<int>(type: "integer", nullable: false),
            //        height = table.Column<int>(type: "integer", nullable: false),
            //        number_of_mines = table.Column<int>(type: "integer", nullable: false),
            //        start_time_seconds = table.Column<int>(type: "integer", nullable: false),
            //        time_format = table.Column<string>(type: "character varying", nullable: false),
            //        game_id = table.Column<int>(type: "integer", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("game_settings_pkey", x => x.game_settings_id);
            //        table.ForeignKey(
            //            name: "gameSettings_gameId_fkey",
            //            column: x => x.game_id,
            //            principalTable: "games",
            //            principalColumn: "games_id");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "moves",
            //    columns: table => new
            //    {
            //        moves_id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        game_id = table.Column<int>(type: "integer", nullable: false),
            //        move_log = table.Column<string>(type: "jsonb", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("moves_pkey", x => x.moves_id);
            //        table.ForeignKey(
            //            name: "moves_gameid_fkey",
            //            column: x => x.game_id,
            //            principalTable: "games",
            //            principalColumn: "games_id");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "game_players",
            //    columns: table => new
            //    {
            //        game_players_id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        game_id = table.Column<int>(type: "integer", nullable: false),
            //        player_id = table.Column<int>(type: "integer", nullable: false),
            //        team_color = table.Column<string>(type: "character varying", nullable: false),
            //        score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("game_players_pkey", x => x.game_players_id);
            //        table.ForeignKey(
            //            name: "gamePlayers_gameId_fkey",
            //            column: x => x.game_id,
            //            principalTable: "games",
            //            principalColumn: "games_id");
            //        table.ForeignKey(
            //            name: "gamePlayers_playerId_fkey",
            //            column: x => x.player_id,
            //            principalTable: "users",
            //            principalColumn: "users_id");
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_game_players_game_id",
            //    table: "game_players",
            //    column: "game_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_game_players_player_id",
            //    table: "game_players",
            //    column: "player_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_game_settings_game_id",
            //    table: "game_settings",
            //    column: "game_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_moves_game_id",
            //    table: "moves",
            //    column: "game_id");

            //migrationBuilder.CreateIndex(
            //    name: "users_email_key",
            //    table: "users",
            //    column: "email",
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "users_username_key",
            //    table: "users",
            //    column: "username",
            //    unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "game_players");

            //migrationBuilder.DropTable(
            //    name: "game_settings");

            //migrationBuilder.DropTable(
            //    name: "moves");

            //migrationBuilder.DropTable(
            //    name: "users");

            //migrationBuilder.DropTable(
            //    name: "games");
        }
    }
}
