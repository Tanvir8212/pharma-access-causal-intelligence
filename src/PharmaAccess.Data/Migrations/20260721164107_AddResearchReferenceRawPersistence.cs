using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchReferenceRawPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResearchReferenceRaw",
                schema: "raw",
                columns: table => new
                {
                    ResearchReferenceRawId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    RawValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchReferenceRaw", x => x.ResearchReferenceRawId);
                    table.ForeignKey(
                        name: "FK_ResearchReferenceRaw_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchReferenceRaw_SourceFileId_SourceRowNumber",
                schema: "raw",
                table: "ResearchReferenceRaw",
                columns: new[] { "SourceFileId", "SourceRowNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchReferenceRaw",
                schema: "raw");
        }
    }
}
