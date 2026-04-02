using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trickler_API.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedAnswerColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_answer",
                table: "answers",
                type: "text",
                nullable: true);

            // Backfill existing rows with a normalized form of the answer
            migrationBuilder.Sql("UPDATE answers SET normalized_answer = lower(btrim(answer));");

            // Make the column non-nullable now that existing rows have values
            migrationBuilder.AlterColumn<string>(
                name: "normalized_answer",
                table: "answers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_answers_trickler_id_normalized_answer",
                table: "answers",
                columns: ["trickler_id", "normalized_answer"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_answers_trickler_id_normalized_answer",
                table: "answers");

            migrationBuilder.DropColumn(
                name: "normalized_answer",
                table: "answers");
        }
    }
}
