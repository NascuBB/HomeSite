using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSite.Migrations.SharedRightsMigrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shared",
                columns: table => new
                {
                    userid = table.Column<int>(type: "integer", nullable: false),
                    serverid = table.Column<string>(type: "text", nullable: false),
                    editserverpreferences = table.Column<bool>(type: "boolean", nullable: false),
                    editmods = table.Column<bool>(type: "boolean", nullable: false),
                    startstopserver = table.Column<bool>(type: "boolean", nullable: false),
                    uploadmods = table.Column<bool>(type: "boolean", nullable: false),
                    sendcommands = table.Column<bool>(type: "boolean", nullable: false),
                    addshareds = table.Column<bool>(type: "boolean", nullable: false),
                    seeserverfiles = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared", x => new { x.userid, x.serverid });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shared");
        }
    }
}
