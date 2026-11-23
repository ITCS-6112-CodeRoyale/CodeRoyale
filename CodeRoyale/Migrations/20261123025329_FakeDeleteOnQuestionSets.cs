using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRoyale.Migrations
{
    /// <inheritdoc />
    public partial class FakeDeleteOnQuestionSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "question_sets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "question_sets");
        }
    }
}
