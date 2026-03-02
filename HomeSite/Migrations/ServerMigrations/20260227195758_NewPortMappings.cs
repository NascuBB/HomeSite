using System.Collections.Generic;
using HomeSite.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSite.Migrations.ServerMigrations
{
    /// <inheritdoc />
    public partial class NewPortMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "publicport",
                table: "servers");

            migrationBuilder.DropColumn(
                name: "rconport",
                table: "servers");

            migrationBuilder.AddColumn<List<PortMapping>>(
                name: "portmappings",
                table: "servers",
                type: "jsonb",
                defaultValue: "[{\"Port\": 8080, \"Path\": null}]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "portmappings",
                table: "servers");

            migrationBuilder.AddColumn<int>(
                name: "publicport",
                table: "servers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "rconport",
                table: "servers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
