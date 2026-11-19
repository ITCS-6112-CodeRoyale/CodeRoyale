using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRoyale.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexToCodeEntryTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Index",
                table: "questions_code_tests",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "RegexFlags",
                table: "questions_code_tests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SecondsDuration",
                table: "questions",
                type: "double",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Index",
                table: "questions_code_tests");

            migrationBuilder.DropColumn(
                name: "RegexFlags",
                table: "questions_code_tests");

            migrationBuilder.DropColumn(
                name: "SecondsDuration",
                table: "questions");
        }
    }
}
