using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Trickler_API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTrickles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "user_trickles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    trickler_id = table.Column<int>(type: "integer", nullable: false),
                    attempts_today = table.Column<int>(type: "integer", nullable: false),
                    attempts_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempt_count_total = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_solved = table.Column<bool>(type: "boolean", nullable: false),
                    solved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reward_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_trickles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_trickles_trickles_trickler_id",
                        column: x => x.trickler_id,
                        principalTable: "trickles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_trickles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_trickles_user_id",
                table: "user_trickles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_trickles_trickler_id",
                table: "user_trickles",
                column: "trickler_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_trickles_user_id_trickler_id",
                table: "user_trickles",
                columns: new[] { "user_id", "trickler_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_trickles_reward_code",
                table: "user_trickles",
                column: "reward_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_trickles");
        }
    }
}