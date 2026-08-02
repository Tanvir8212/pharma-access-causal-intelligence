using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentModelGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChampionChallengerComparison",
                schema: "ml",
                columns: table => new
                {
                    ComparisonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChampionVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChallengerVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChampionArtifactPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChallengerArtifactPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChampionArtifactSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChallengerArtifactSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FeatureSchemaHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EvaluationCohortHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DatasetHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DatasetFreezeIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReproducibilityHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChampionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChallengerJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetricDifferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubgroupResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockingReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromotionEligible = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionChallengerComparison", x => x.ComparisonId);
                });

            migrationBuilder.CreateTable(
                name: "ChampionState",
                schema: "ml",
                columns: table => new
                {
                    GovernanceChampionStateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChampionVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PreviousChampionVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionState", x => x.GovernanceChampionStateId);
                    table.CheckConstraint("CK_ChampionState_Singleton", "[GovernanceChampionStateId] = 1");
                });

            migrationBuilder.CreateTable(
                name: "DriftReport",
                schema: "ml",
                columns: table => new
                {
                    DriftReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChampionVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EvaluationWindow = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    LabelsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    SubgroupWarningsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    GovernanceNotice = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriftReport", x => x.DriftReportId);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceDecision",
                schema: "audit",
                columns: table => new
                {
                    GovernanceDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComparisonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ChampionBefore = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChampionAfter = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChallengerVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApproverIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActionTimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceDecision", x => x.GovernanceDecisionId);
                    table.ForeignKey(
                        name: "FK_GovernanceDecision_ChampionChallengerComparison_ComparisonId",
                        column: x => x.ComparisonId,
                        principalSchema: "ml",
                        principalTable: "ChampionChallengerComparison",
                        principalColumn: "ComparisonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriftFinding",
                schema: "ml",
                columns: table => new
                {
                    DriftFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriftReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Statistic = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReferenceValue = table.Column<double>(type: "float", nullable: false),
                    CurrentValue = table.Column<double>(type: "float", nullable: false),
                    Change = table.Column<double>(type: "float", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriftFinding", x => x.DriftFindingId);
                    table.ForeignKey(
                        name: "FK_DriftFinding_DriftReport_DriftReportId",
                        column: x => x.DriftReportId,
                        principalSchema: "ml",
                        principalTable: "DriftReport",
                        principalColumn: "DriftReportId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChampionHistory",
                schema: "audit",
                columns: table => new
                {
                    ChampionHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PreviousChampionVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    GovernanceDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupersededAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionHistory", x => x.ChampionHistoryId);
                    table.ForeignKey(
                        name: "FK_ChampionHistory_GovernanceDecision_GovernanceDecisionId",
                        column: x => x.GovernanceDecisionId,
                        principalSchema: "audit",
                        principalTable: "GovernanceDecision",
                        principalColumn: "GovernanceDecisionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceAuditRecord",
                schema: "audit",
                columns: table => new
                {
                    GovernanceAuditRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernanceDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceAuditRecord", x => x.GovernanceAuditRecordId);
                    table.ForeignKey(
                        name: "FK_GovernanceAuditRecord_GovernanceDecision_GovernanceDecisionId",
                        column: x => x.GovernanceDecisionId,
                        principalSchema: "audit",
                        principalTable: "GovernanceDecision",
                        principalColumn: "GovernanceDecisionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChampionChallengerComparison_ChampionVersion_ChallengerVersion_CreatedAtUtc",
                schema: "ml",
                table: "ChampionChallengerComparison",
                columns: new[] { "ChampionVersion", "ChallengerVersion", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChampionHistory_GovernanceDecisionId",
                schema: "audit",
                table: "ChampionHistory",
                column: "GovernanceDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionHistory_IsCurrent",
                schema: "audit",
                table: "ChampionHistory",
                column: "IsCurrent",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionHistory_ModelVersion_ApprovedAtUtc",
                schema: "audit",
                table: "ChampionHistory",
                columns: new[] { "ModelVersion", "ApprovedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DriftFinding_DriftReportId_Scope_Name_Statistic",
                schema: "ml",
                table: "DriftFinding",
                columns: new[] { "DriftReportId", "Scope", "Name", "Statistic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriftReport_ChampionVersion_CreatedAtUtc",
                schema: "ml",
                table: "DriftReport",
                columns: new[] { "ChampionVersion", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceAuditRecord_GovernanceDecisionId",
                schema: "audit",
                table: "GovernanceAuditRecord",
                column: "GovernanceDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceDecision_ComparisonId",
                schema: "audit",
                table: "GovernanceDecision",
                column: "ComparisonId",
                unique: true,
                filter: "[ComparisonId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChampionHistory",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ChampionState",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "DriftFinding",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "GovernanceAuditRecord",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "DriftReport",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "GovernanceDecision",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ChampionChallengerComparison",
                schema: "ml");
        }
    }
}
