using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchImportPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResearchImportRun",
                schema: "research",
                columns: table => new
                {
                    ResearchImportRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    ResearchProtocolId = table.Column<long>(type: "bigint", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceManifestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchImportRun", x => x.ResearchImportRunId);
                    table.ForeignKey(
                        name: "FK_ResearchImportRun_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchImportRun_ResearchProtocol_ResearchProtocolId",
                        column: x => x.ResearchProtocolId,
                        principalSchema: "research",
                        principalTable: "ResearchProtocol",
                        principalColumn: "ResearchProtocolId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchImportAuditEvent",
                schema: "research",
                columns: table => new
                {
                    ResearchImportAuditEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchImportRunId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DetailJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchImportAuditEvent", x => x.ResearchImportAuditEventId);
                    table.ForeignKey(
                        name: "FK_ResearchImportAuditEvent_ResearchImportRun_ResearchImportRunId",
                        column: x => x.ResearchImportRunId,
                        principalSchema: "research",
                        principalTable: "ResearchImportRun",
                        principalColumn: "ResearchImportRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchImportCheckpoint",
                schema: "research",
                columns: table => new
                {
                    ResearchImportCheckpointId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchImportRunId = table.Column<long>(type: "bigint", nullable: false),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastCompletedRow = table.Column<long>(type: "bigint", nullable: false),
                    LastCompletedBatch = table.Column<int>(type: "int", nullable: false),
                    AggregateHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchImportCheckpoint", x => x.ResearchImportCheckpointId);
                    table.ForeignKey(
                        name: "FK_ResearchImportCheckpoint_ResearchImportRun_ResearchImportRunId",
                        column: x => x.ResearchImportRunId,
                        principalSchema: "research",
                        principalTable: "ResearchImportRun",
                        principalColumn: "ResearchImportRunId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchImportCheckpoint_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchMappingReview",
                schema: "research",
                columns: table => new
                {
                    ResearchMappingReviewId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchImportRunId = table.Column<long>(type: "bigint", nullable: false),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    MappingType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceValueHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchMappingReview", x => x.ResearchMappingReviewId);
                    table.ForeignKey(
                        name: "FK_ResearchMappingReview_ResearchImportRun_ResearchImportRunId",
                        column: x => x.ResearchImportRunId,
                        principalSchema: "research",
                        principalTable: "ResearchImportRun",
                        principalColumn: "ResearchImportRunId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchMappingReview_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchRejectedRow",
                schema: "research",
                columns: table => new
                {
                    ResearchRejectedRowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchImportRunId = table.Column<long>(type: "bigint", nullable: false),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchRejectedRow", x => x.ResearchRejectedRowId);
                    table.ForeignKey(
                        name: "FK_ResearchRejectedRow_ResearchImportRun_ResearchImportRunId",
                        column: x => x.ResearchImportRunId,
                        principalSchema: "research",
                        principalTable: "ResearchImportRun",
                        principalColumn: "ResearchImportRunId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchRejectedRow_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchImportAuditEvent_ResearchImportRunId_CreatedAtUtc",
                schema: "research",
                table: "ResearchImportAuditEvent",
                columns: new[] { "ResearchImportRunId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchImportCheckpoint_ResearchImportRunId_SourceFileId",
                schema: "research",
                table: "ResearchImportCheckpoint",
                columns: new[] { "ResearchImportRunId", "SourceFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchImportCheckpoint_SourceFileId",
                schema: "research",
                table: "ResearchImportCheckpoint",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchImportRun_CorrelationId",
                schema: "research",
                table: "ResearchImportRun",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchImportRun_DatasetVersionId",
                schema: "research",
                table: "ResearchImportRun",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchImportRun_ResearchProtocolId",
                schema: "research",
                table: "ResearchImportRun",
                column: "ResearchProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchMappingReview_ResearchImportRunId_SourceFileId_SourceRowNumber_MappingType",
                schema: "research",
                table: "ResearchMappingReview",
                columns: new[] { "ResearchImportRunId", "SourceFileId", "SourceRowNumber", "MappingType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchMappingReview_SourceFileId",
                schema: "research",
                table: "ResearchMappingReview",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchRejectedRow_ResearchImportRunId_SourceFileId_SourceRowNumber",
                schema: "research",
                table: "ResearchRejectedRow",
                columns: new[] { "ResearchImportRunId", "SourceFileId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchRejectedRow_SourceFileId",
                schema: "research",
                table: "ResearchRejectedRow",
                column: "SourceFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchImportAuditEvent",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchImportCheckpoint",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchMappingReview",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchRejectedRow",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchImportRun",
                schema: "research");
        }
    }
}
