using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSite.Migrations.ServerMigrations
{
    /// <inheritdoc />
    public partial class AddBedrockPort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "bedrockport",
                table: "servers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bedrockport",
                table: "servers");
        }
    }
}
