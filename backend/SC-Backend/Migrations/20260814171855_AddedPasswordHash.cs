using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "user_role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'NotSet'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValueSql: "'NotSet'::character varying");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "datecreated",
                table: "users",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE",
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "users");

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
                oldDefaultValueSql: "'NotSet'::character varying");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "datecreated",
                table: "users",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldDefaultValueSql: "CURRENT_DATE");
        }
    }
}
