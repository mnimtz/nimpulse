using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NimPulse.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiGatewaySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DefaultProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ClaudeModel = table.Column<string>(type: "TEXT", nullable: false),
                    ClaudeApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    AzureOpenAiDeploymentName = table.Column<string>(type: "TEXT", nullable: false),
                    AzureOpenAiEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    AzureOpenAiApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    OpenAiModel = table.Column<string>(type: "TEXT", nullable: false),
                    OpenAiApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiGatewaySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CategoryValue = table.Column<int>(type: "INTEGER", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthSamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SyncWindowDays = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthSamples_UserId_ExternalId",
                table: "HealthSamples",
                columns: new[] { "UserId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthSamples_UserId_Type_StartDate",
                table: "HealthSamples",
                columns: new[] { "UserId", "Type", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiGatewaySettings");

            migrationBuilder.DropTable(
                name: "HealthSamples");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
