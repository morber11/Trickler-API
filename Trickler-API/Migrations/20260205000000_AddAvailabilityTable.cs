using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Trickler_API.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "availabilities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    type = table.Column<int>(type: "integer", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: true),
                    until_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dates = table.Column<string>(type: "jsonb", nullable: true),
                    days_of_week = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availabilities", x => x.id);
                });

            migrationBuilder.AddColumn<int>(
                name: "availability_id",
                table: "trickles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_trickles_availability_id",
                table: "trickles",
                column: "availability_id");

            migrationBuilder.AddForeignKey(
                name: "fk_trickles_availabilities_availability_id",
                table: "trickles",
                column: "availability_id",
                principalTable: "availabilities",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_trickles_availabilities_availability_id",
                table: "trickles");

            migrationBuilder.DropIndex(
                name: "ix_trickles_availability_id",
                table: "trickles");

            migrationBuilder.DropColumn(
                name: "availability_id",
                table: "trickles");

            migrationBuilder.DropTable(
                name: "availabilities");
        }
    }
}
