using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSite.Migrations.ServerMigrations
{
    /// <inheritdoc />
    public partial class AddCurseforgePack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "curseforgepack",
                table: "servers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "curseforgepack",
                table: "servers");
        }
    }
}
