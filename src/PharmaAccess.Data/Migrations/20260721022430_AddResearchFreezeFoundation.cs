using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchFreezeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "research");

            migrationBuilder.CreateTable(
                name: "ResearchCohortDefinition",
                schema: "research",
                columns: table => new
                {
                    ResearchCohortDefinitionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PopulationDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinitionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchCohortDefinition", x => x.ResearchCohortDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "ResearchProtocol",
                schema: "research",
                columns: table => new
                {
                    ResearchProtocolId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProtocolCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtocolVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResearchQuestion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryPredictionTask = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryCausalQuestion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryPredictiveMetric = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryPredictiveMetrics = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryCausalEstimand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryEffectScale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryEstimator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryEstimators = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PopulationDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservationWindow = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InclusionRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExclusionRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StateEligibilityPolicyVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StateEntryPolicyVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarketWeightPolicyVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    PredictiveSplitDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    DagVersionId = table.Column<long>(type: "bigint", nullable: false),
                    AdjustmentSetVersionId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentDefinitionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    OutcomeDefinitionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    MissingDataPolicy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CensoringPolicy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MultiplicityPolicy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SensitivityAnalysisPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubgroupAnalysisPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoppingRules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DefinitionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchProtocol", x => x.ResearchProtocolId);
                });

            migrationBuilder.CreateTable(
                name: "ResearchSourceRegistration",
                schema: "research",
                columns: table => new
                {
                    SourceRegistryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceAuthority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatasetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OfficialSourceReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoverageStart = table.Column<DateOnly>(type: "date", nullable: true),
                    CoverageEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    LicenseOrUsageNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedSchemaVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: true),
                    Encoding = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Delimiter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSynthetic = table.Column<bool>(type: "bit", nullable: false),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ValidationMessages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchSourceRegistration", x => x.SourceRegistryId);
                });

            migrationBuilder.CreateTable(
                name: "ResearchExclusionRule",
                schema: "research",
                columns: table => new
                {
                    ResearchExclusionRuleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchCohortDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsBlocking = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchExclusionRule", x => x.ResearchExclusionRuleId);
                    table.ForeignKey(
                        name: "FK_ResearchExclusionRule_ResearchCohortDefinition_ResearchCohortDefinitionId",
                        column: x => x.ResearchCohortDefinitionId,
                        principalSchema: "research",
                        principalTable: "ResearchCohortDefinition",
                        principalColumn: "ResearchCohortDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchAnalysisPlan",
                schema: "research",
                columns: table => new
                {
                    ResearchAnalysisPlanId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchProtocolId = table.Column<long>(type: "bigint", nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VersionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinitionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TestPartitionLockedForSelection = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchAnalysisPlan", x => x.ResearchAnalysisPlanId);
                    table.ForeignKey(
                        name: "FK_ResearchAnalysisPlan_ResearchProtocol_ResearchProtocolId",
                        column: x => x.ResearchProtocolId,
                        principalSchema: "research",
                        principalTable: "ResearchProtocol",
                        principalColumn: "ResearchProtocolId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchDataFreeze",
                schema: "research",
                columns: table => new
                {
                    ResearchDataFreezeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FreezeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FreezeVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResearchProtocolId = table.Column<long>(type: "bigint", nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    SourceManifestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DatasetHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CohortHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FeatureSchemaHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PredictiveSplitHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CausalProtocolHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PythonEnvironmentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DotNetEnvironmentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GitCommitHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSynthetic = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BlockingFindingCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FrozenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FrozenBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchDataFreeze", x => x.ResearchDataFreezeId);
                    table.ForeignKey(
                        name: "FK_ResearchDataFreeze_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchDataFreeze_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchDataFreeze_ResearchProtocol_ResearchProtocolId",
                        column: x => x.ResearchProtocolId,
                        principalSchema: "research",
                        principalTable: "ResearchProtocol",
                        principalColumn: "ResearchProtocolId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchProtocolApproval",
                schema: "research",
                columns: table => new
                {
                    ResearchProtocolApprovalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchProtocolId = table.Column<long>(type: "bigint", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchProtocolApproval", x => x.ResearchProtocolApprovalId);
                    table.ForeignKey(
                        name: "FK_ResearchProtocolApproval_ResearchProtocol_ResearchProtocolId",
                        column: x => x.ResearchProtocolId,
                        principalSchema: "research",
                        principalTable: "ResearchProtocol",
                        principalColumn: "ResearchProtocolId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchFreezeArtifact",
                schema: "research",
                columns: table => new
                {
                    ResearchFreezeArtifactId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchDataFreezeId = table.Column<long>(type: "bigint", nullable: false),
                    ArtifactType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchFreezeArtifact", x => x.ResearchFreezeArtifactId);
                    table.ForeignKey(
                        name: "FK_ResearchFreezeArtifact_ResearchDataFreeze_ResearchDataFreezeId",
                        column: x => x.ResearchDataFreezeId,
                        principalSchema: "research",
                        principalTable: "ResearchDataFreeze",
                        principalColumn: "ResearchDataFreezeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchFreezeFinding",
                schema: "research",
                columns: table => new
                {
                    ResearchFreezeFindingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchDataFreezeId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchFreezeFinding", x => x.ResearchFreezeFindingId);
                    table.ForeignKey(
                        name: "FK_ResearchFreezeFinding_ResearchDataFreeze_ResearchDataFreezeId",
                        column: x => x.ResearchDataFreezeId,
                        principalSchema: "research",
                        principalTable: "ResearchDataFreeze",
                        principalColumn: "ResearchDataFreezeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchFreezeSource",
                schema: "research",
                columns: table => new
                {
                    ResearchFreezeSourceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchDataFreezeId = table.Column<long>(type: "bigint", nullable: false),
                    ResearchSourceRegistrationId = table.Column<long>(type: "bigint", nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SchemaProfileVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchFreezeSource", x => x.ResearchFreezeSourceId);
                    table.ForeignKey(
                        name: "FK_ResearchFreezeSource_ResearchDataFreeze_ResearchDataFreezeId",
                        column: x => x.ResearchDataFreezeId,
                        principalSchema: "research",
                        principalTable: "ResearchDataFreeze",
                        principalColumn: "ResearchDataFreezeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchFreezeSource_ResearchSourceRegistration_ResearchSourceRegistrationId",
                        column: x => x.ResearchSourceRegistrationId,
                        principalSchema: "research",
                        principalTable: "ResearchSourceRegistration",
                        principalColumn: "SourceRegistryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchAnalysisPlan_ResearchProtocolId_PlanType_VersionCode",
                schema: "research",
                table: "ResearchAnalysisPlan",
                columns: new[] { "ResearchProtocolId", "PlanType", "VersionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCohortDefinition_DefinitionHash",
                schema: "research",
                table: "ResearchCohortDefinition",
                column: "DefinitionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchCohortDefinition_VersionCode",
                schema: "research",
                table: "ResearchCohortDefinition",
                column: "VersionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDataFreeze_DatasetVersionId",
                schema: "research",
                table: "ResearchDataFreeze",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDataFreeze_FeatureSetVersionId",
                schema: "research",
                table: "ResearchDataFreeze",
                column: "FeatureSetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDataFreeze_FreezeCode_FreezeVersion",
                schema: "research",
                table: "ResearchDataFreeze",
                columns: new[] { "FreezeCode", "FreezeVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDataFreeze_ResearchProtocolId",
                schema: "research",
                table: "ResearchDataFreeze",
                column: "ResearchProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchExclusionRule_ResearchCohortDefinitionId_RuleCode",
                schema: "research",
                table: "ResearchExclusionRule",
                columns: new[] { "ResearchCohortDefinitionId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchFreezeArtifact_ResearchDataFreezeId_ArtifactType",
                schema: "research",
                table: "ResearchFreezeArtifact",
                columns: new[] { "ResearchDataFreezeId", "ArtifactType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchFreezeFinding_ResearchDataFreezeId_Severity_Code",
                schema: "research",
                table: "ResearchFreezeFinding",
                columns: new[] { "ResearchDataFreezeId", "Severity", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchFreezeSource_ResearchDataFreezeId_ResearchSourceRegistrationId",
                schema: "research",
                table: "ResearchFreezeSource",
                columns: new[] { "ResearchDataFreezeId", "ResearchSourceRegistrationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchFreezeSource_ResearchSourceRegistrationId",
                schema: "research",
                table: "ResearchFreezeSource",
                column: "ResearchSourceRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchProtocol_DefinitionHash",
                schema: "research",
                table: "ResearchProtocol",
                column: "DefinitionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchProtocol_ProtocolCode_ProtocolVersion",
                schema: "research",
                table: "ResearchProtocol",
                columns: new[] { "ProtocolCode", "ProtocolVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchProtocolApproval_ResearchProtocolId_DecidedAtUtc",
                schema: "research",
                table: "ResearchProtocolApproval",
                columns: new[] { "ResearchProtocolId", "DecidedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchSourceRegistration_Sha256",
                schema: "research",
                table: "ResearchSourceRegistration",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchSourceRegistration_SourceCode",
                schema: "research",
                table: "ResearchSourceRegistration",
                column: "SourceCode");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchSourceRegistration_SourceCode_Sha256",
                schema: "research",
                table: "ResearchSourceRegistration",
                columns: new[] { "SourceCode", "Sha256" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchAnalysisPlan",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchExclusionRule",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchFreezeArtifact",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchFreezeFinding",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchFreezeSource",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchProtocolApproval",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchCohortDefinition",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchDataFreeze",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchSourceRegistration",
                schema: "research");

            migrationBuilder.DropTable(
                name: "ResearchProtocol",
                schema: "research");
        }
    }
}
