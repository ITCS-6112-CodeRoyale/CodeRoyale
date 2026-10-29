using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRoyale.Migrations
{
    /// <inheritdoc />
    public partial class CreateQuestionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "language",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LanguageCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_language", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "question_sets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    CreatedFromId = table.Column<int>(type: "int", nullable: true),
                    CreatedFromRevision = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_sets", x => new { x.Id, x.Revision });
                    table.ForeignKey(
                        name: "FK_question_sets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_question_sets_language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_question_sets_question_sets_CreatedFromId_CreatedFromRevision",
                        columns: x => new { x.CreatedFromId, x.CreatedFromRevision },
                        principalTable: "question_sets",
                        principalColumns: new[] { "Id", "Revision" });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Text = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    QuestionSetId = table.Column<int>(type: "int", nullable: true),
                    QuestionSetRevision = table.Column<int>(type: "int", nullable: true),
                    StartingText = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxSelectCount = table.Column<int>(type: "int", nullable: true),
                    MinSelectCount = table.Column<int>(type: "int", nullable: true),
                    RandomizeAnswers = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_question_sets_QuestionSetId_QuestionSetRevision",
                        columns: x => new { x.QuestionSetId, x.QuestionSetRevision },
                        principalTable: "question_sets",
                        principalColumns: new[] { "Id", "Revision" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "question_mcq_answers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Text = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MultipleChoiceQuestionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_mcq_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_mcq_answers_questions_MultipleChoiceQuestionId",
                        column: x => x.MultipleChoiceQuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "questions_blank_answers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CorrectRegex = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegexFlags = table.Column<int>(type: "int", nullable: false),
                    BlankEntryQuestionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions_blank_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_blank_answers_questions_BlankEntryQuestionId",
                        column: x => x.BlankEntryQuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "questions_code_tests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrGroup = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CodeEntryQuestionId = table.Column<int>(type: "int", nullable: true),
                    CodeEntryQuestionId1 = table.Column<int>(type: "int", nullable: true),
                    Pattern = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExcludeComments = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ExcludeLiterals = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    PreTest = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PostTest = table.Column<string>(type: "varchar(8192)", maxLength: 8192, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions_code_tests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_code_tests_questions_CodeEntryQuestionId",
                        column: x => x.CodeEntryQuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_questions_code_tests_questions_CodeEntryQuestionId1",
                        column: x => x.CodeEntryQuestionId1,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_question_mcq_answers_MultipleChoiceQuestionId",
                table: "question_mcq_answers",
                column: "MultipleChoiceQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_question_sets_CreatedFromId_CreatedFromRevision",
                table: "question_sets",
                columns: new[] { "CreatedFromId", "CreatedFromRevision" });

            migrationBuilder.CreateIndex(
                name: "IX_question_sets_LanguageId",
                table: "question_sets",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_question_sets_UserId",
                table: "question_sets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_QuestionSetId_QuestionSetRevision",
                table: "questions",
                columns: new[] { "QuestionSetId", "QuestionSetRevision" });

            migrationBuilder.CreateIndex(
                name: "IX_questions_blank_answers_BlankEntryQuestionId",
                table: "questions_blank_answers",
                column: "BlankEntryQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_code_tests_CodeEntryQuestionId",
                table: "questions_code_tests",
                column: "CodeEntryQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_code_tests_CodeEntryQuestionId1",
                table: "questions_code_tests",
                column: "CodeEntryQuestionId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "question_mcq_answers");

            migrationBuilder.DropTable(
                name: "questions_blank_answers");

            migrationBuilder.DropTable(
                name: "questions_code_tests");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "question_sets");

            migrationBuilder.DropTable(
                name: "language");
        }
    }
}
