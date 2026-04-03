using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trickler_API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPrivateToUserDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                table: "user_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_private",
                table: "user_details");
        }
    }
}
