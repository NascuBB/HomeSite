using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSite.Migrations
{
    /// <inheritdoc />
    public partial class ShortLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShortLogs",
                table: "UserAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortLogs",
                table: "UserAccounts");
        }
    }
}
