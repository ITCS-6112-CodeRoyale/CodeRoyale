using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRoyale.Migrations
{
    /// <inheritdoc />
    public partial class CreateGameModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CorrectRegex",
                table: "questions_blank_answers",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "questions",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionSetId = table.Column<int>(type: "int", nullable: false),
                    QuestionSetRevision = table.Column<int>(type: "int", nullable: false),
                    StartedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedTime = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_games_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_games_question_sets_QuestionSetId_QuestionSetRevision",
                        columns: x => new { x.QuestionSetId, x.QuestionSetRevision },
                        principalTable: "question_sets",
                        principalColumns: new[] { "Id", "Revision" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "game_players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JoinedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LeftTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Score = table.Column<double>(type: "double", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_game_players_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_game_players_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "games_question_results",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    AnswerTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Response = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false),
                    GamePlayerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games_question_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_games_question_results_game_players_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "game_players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_games_question_results_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_game_players_GameId",
                table: "game_players",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_game_players_UserId",
                table: "game_players",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_games_QuestionSetId_QuestionSetRevision",
                table: "games",
                columns: new[] { "QuestionSetId", "QuestionSetRevision" });

            migrationBuilder.CreateIndex(
                name: "IX_games_UserId",
                table: "games",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_games_question_results_GamePlayerId",
                table: "games_question_results",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_games_question_results_QuestionId",
                table: "games_question_results",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "games_question_results");

            migrationBuilder.DropTable(
                name: "game_players");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "questions");

            migrationBuilder.AlterColumn<string>(
                name: "CorrectRegex",
                table: "questions_blank_answers",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1024)",
                oldMaxLength: 1024)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
