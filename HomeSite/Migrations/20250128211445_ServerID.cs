using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSite.Migrations
{
    /// <inheritdoc />
    public partial class ServerID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServerID",
                table: "UserAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServerID",
                table: "UserAccounts");
        }
    }
}
