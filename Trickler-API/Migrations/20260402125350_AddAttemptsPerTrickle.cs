using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trickler_API.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptsPerTrickle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts_per_trickle",
                table: "trickles",
                type: "integer",
                nullable: false,
                defaultValue: -1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attempts_per_trickle",
                table: "trickles");
        }
    }
}
