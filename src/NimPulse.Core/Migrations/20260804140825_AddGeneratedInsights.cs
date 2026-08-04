using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NimPulse.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneratedInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedInsights", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedInsights_UserId_WeekStart",
                table: "GeneratedInsights",
                columns: new[] { "UserId", "WeekStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedInsights");
        }
    }
}
