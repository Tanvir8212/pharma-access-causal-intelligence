using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchDatabaseOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResearchDatabaseOwnership",
                schema: "research",
                columns: table => new
                {
                    ResearchDatabaseOwnershipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RepositoryMarker = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchDatabaseOwnership", x => x.ResearchDatabaseOwnershipId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDatabaseOwnership_ProjectId",
                schema: "research",
                table: "ResearchDatabaseOwnership",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchDatabaseOwnership",
                schema: "research");
        }
    }
}
