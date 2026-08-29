using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace auth.Migrations
{
    /// <inheritdoc />
    public partial class RemovedUserDetailsFromAuthService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Auths");

            migrationBuilder.DropColumn(
                name: "Lastname",
                table: "Auths");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Auths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Auths",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lastname",
                table: "Auths",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Auths",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
