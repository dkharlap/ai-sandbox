using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimpleAccount.Preferences.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "prefs");

            migrationBuilder.CreateTable(
                name: "model_catalog",
                schema: "prefs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    vendor = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_model_catalog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_model_preferences",
                schema: "prefs",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<string>(type: "text", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_model_preferences", x => new { x.user_id, x.model_id });
                    table.ForeignKey(
                        name: "FK_user_model_preferences_model_catalog_model_id",
                        column: x => x.model_id,
                        principalSchema: "prefs",
                        principalTable: "model_catalog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "prefs",
                table: "model_catalog",
                columns: new[] { "id", "display_name", "is_active", "vendor" },
                values: new object[,]
                {
                    { "claude-haiku-4-5", "Claude Haiku 4.5", true, "Anthropic" },
                    { "claude-opus-5", "Claude Opus 5", true, "Anthropic" },
                    { "claude-sonnet-5", "Claude Sonnet 5", true, "Anthropic" },
                    { "gemini-3-pro", "Gemini 3 Pro", true, "Google" },
                    { "gpt-5-codex", "GPT-5 Codex", true, "OpenAI" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_model_preferences_model_id",
                schema: "prefs",
                table: "user_model_preferences",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_model_preferences_user_rank",
                schema: "prefs",
                table: "user_model_preferences",
                columns: new[] { "user_id", "rank" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_model_preferences",
                schema: "prefs");

            migrationBuilder.DropTable(
                name: "model_catalog",
                schema: "prefs");
        }
    }
}
