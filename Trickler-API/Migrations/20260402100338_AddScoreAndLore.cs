using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trickler_API.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreAndLore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "score",
                table: "trickles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "reward_text",
                table: "trickles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "score",
                table: "trickles");

            migrationBuilder.DropColumn(
                name: "reward_text",
                table: "trickles");
        }
    }
}
