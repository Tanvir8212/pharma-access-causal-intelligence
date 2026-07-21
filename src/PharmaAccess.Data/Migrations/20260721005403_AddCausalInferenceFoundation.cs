using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCausalInferenceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "causal");

            migrationBuilder.CreateTable(
                name: "CausalAdjustmentSet",
                schema: "causal",
                columns: table => new
                {
                    CausalAdjustmentSetId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    VariablesJson = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false),
                    DefinitionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CausalAdjustmentSet", x => x.CausalAdjustmentSetId);
                });

            migrationBuilder.CreateTable(
                name: "CausalDagDefinition",
                schema: "causal",
                columns: table => new
                {
                    CausalDagDefinitionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false),
                    DefinitionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CausalDagDefinition", x => x.CausalDagDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentDefinition",
                schema: "causal",
                columns: table => new
                {
                    TreatmentDefinitionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TreatmentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExposureType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Threshold = table.Column<double>(type: "float(53)", nullable: false),
                    MinimumPeerCount = table.Column<int>(type: "int", nullable: false),
                    LagQuarters = table.Column<int>(type: "int", nullable: false),
                    ReferenceVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false),
                    DefinitionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentDefinition", x => x.TreatmentDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "CausalStudy",
                schema: "causal",
                columns: table => new
                {
                    CausalStudyId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudyCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StudyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ResearchQuestion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    CausalDagDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    CausalAdjustmentSetId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    OutcomeDefinitionVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Estimand = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetPopulation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ObservationStartQuarterId = table.Column<int>(type: "int", nullable: false),
                    ObservationEndQuarterId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AssumptionStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeCommitHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CausalStudy", x => x.CausalStudyId);
                    table.ForeignKey(
                        name: "FK_CausalStudy_CalendarQuarter_ObservationEndQuarterId",
                        column: x => x.ObservationEndQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalStudy_CalendarQuarter_ObservationStartQuarterId",
                        column: x => x.ObservationStartQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalStudy_CausalAdjustmentSet_CausalAdjustmentSetId",
                        column: x => x.CausalAdjustmentSetId,
                        principalSchema: "causal",
                        principalTable: "CausalAdjustmentSet",
                        principalColumn: "CausalAdjustmentSetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalStudy_CausalDagDefinition_CausalDagDefinitionId",
                        column: x => x.CausalDagDefinitionId,
                        principalSchema: "causal",
                        principalTable: "CausalDagDefinition",
                        principalColumn: "CausalDagDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalStudy_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalStudy_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalStudy_TreatmentDefinition_TreatmentDefinitionId",
                        column: x => x.TreatmentDefinitionId,
                        principalSchema: "causal",
                        principalTable: "TreatmentDefinition",
                        principalColumn: "TreatmentDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CausalAnalysisRow",
                schema: "causal",
                columns: table => new
                {
                    CausalAnalysisRowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CausalStudyId = table.Column<long>(type: "bigint", nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    ObservationQuarterId = table.Column<int>(type: "int", nullable: false),
                    OutcomeQuarterId = table.Column<int>(type: "int", nullable: false),
                    TreatmentValue = table.Column<double>(type: "float(53)", nullable: false),
                    BinaryTreatment = table.Column<bool>(type: "bit", nullable: false),
                    OutcomeValue = table.Column<bool>(type: "bit", nullable: false),
                    IsCensored = table.Column<bool>(type: "bit", nullable: false),
                    EligibilityStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PropensityScore = table.Column<double>(type: "float(53)", nullable: true),
                    OutcomeRegressionTreated = table.Column<double>(type: "float(53)", nullable: true),
                    OutcomeRegressionUntreated = table.Column<double>(type: "float(53)", nullable: true),
                    InfluenceFunctionValue = table.Column<double>(type: "float(53)", nullable: true),
                    Weight = table.Column<double>(type: "float(53)", nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CausalAnalysisRow", x => x.CausalAnalysisRowId);
                    table.ForeignKey(
                        name: "FK_CausalAnalysisRow_CalendarQuarter_ObservationQuarterId",
                        column: x => x.ObservationQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalAnalysisRow_CalendarQuarter_OutcomeQuarterId",
                        column: x => x.OutcomeQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalAnalysisRow_CausalStudy_CausalStudyId",
                        column: x => x.CausalStudyId,
                        principalSchema: "causal",
                        principalTable: "CausalStudy",
                        principalColumn: "CausalStudyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalAnalysisRow_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CausalAnalysisRow_State_StateId",
                        column: x => x.StateId,
                        principalSchema: "core",
                        principalTable: "State",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CausalDiagnostic",
                schema: "causal",
                columns: table => new
                {
                    CausalDiagnosticId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CausalStudyId = table.Column<long>(type: "bigint", nullable: false),
                    DiagnosticType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VariableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MetricName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MetricValue = table.Column<double>(type: "float(53)", nullable: true),
                    Threshold = table.Column<double>(type: "float(53)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CausalDiagnostic", x => x.CausalDiagnosticId);
                    table.ForeignKey(
                        name: "FK_CausalDiagnostic_CausalStudy_CausalStudyId",
                        column: x => x.CausalStudyId,
                        principalSchema: "causal",
                        principalTable: "CausalStudy",
                        principalColumn: "CausalStudyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CausalEstimate",
                schema: "causal",
                columns: table => new
                {
                    CausalEstimateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CausalStudyId = table.Column<long>(type: "bigint", nullable: false),
                    EstimatorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Estimand = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EffectScale = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Estimate = table.Column<double>(type: "float(53)", nullable: false),
                    StandardError = table.Column<double>(type: "float(53)", nullable: true),
                    ConfidenceIntervalLower = table.Column<double>(type: "float(53)", nullable: true),
                    ConfidenceIntervalUpper = table.Column<double>(type: "float(53)", nullable: true),
                    PValue = table.Column<double>(type: "float(53)", nullable: true),
                    BootstrapRepetitions = table.Column<int>(type: "int", nullable: true),
                    EffectiveSampleSize = table.Column<double>(type: "float(53)", nullable: true),
                    TreatedCount = table.Column<int>(type: "int", nullable: false),
                    ControlCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Interpretation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Limitations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CausalEstimate", x => x.CausalEstimateId);
                    table.ForeignKey(
                        name: "FK_CausalEstimate_CausalStudy_CausalStudyId",
                        column: x => x.CausalStudyId,
                        principalSchema: "causal",
                        principalTable: "CausalStudy",
                        principalColumn: "CausalStudyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CounterfactualScenario",
                schema: "causal",
                columns: table => new
                {
                    CounterfactualScenarioId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CausalStudyId = table.Column<long>(type: "bigint", nullable: false),
                    ScenarioCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScenarioName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InterventionDefinitionJson = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false),
                    BaselinePolicy = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterfactualScenario", x => x.CounterfactualScenarioId);
                    table.ForeignKey(
                        name: "FK_CounterfactualScenario_CausalStudy_CausalStudyId",
                        column: x => x.CausalStudyId,
                        principalSchema: "causal",
                        principalTable: "CausalStudy",
                        principalColumn: "CausalStudyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CounterfactualResult",
                schema: "causal",
                columns: table => new
                {
                    CounterfactualResultId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CounterfactualScenarioId = table.Column<long>(type: "bigint", nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: true),
                    StateId = table.Column<int>(type: "int", nullable: true),
                    ObservationQuarterId = table.Column<int>(type: "int", nullable: true),
                    BaselinePredictedOutcome = table.Column<double>(type: "float(53)", nullable: false),
                    InterventionPredictedOutcome = table.Column<double>(type: "float(53)", nullable: false),
                    EstimatedDifference = table.Column<double>(type: "float(53)", nullable: false),
                    UncertaintyLower = table.Column<double>(type: "float(53)", nullable: true),
                    UncertaintyUpper = table.Column<double>(type: "float(53)", nullable: true),
                    SupportStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WarningMessagesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterfactualResult", x => x.CounterfactualResultId);
                    table.ForeignKey(
                        name: "FK_CounterfactualResult_CalendarQuarter_ObservationQuarterId",
                        column: x => x.ObservationQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CounterfactualResult_CounterfactualScenario_CounterfactualScenarioId",
                        column: x => x.CounterfactualScenarioId,
                        principalSchema: "causal",
                        principalTable: "CounterfactualScenario",
                        principalColumn: "CounterfactualScenarioId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CounterfactualResult_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CounterfactualResult_State_StateId",
                        column: x => x.StateId,
                        principalSchema: "core",
                        principalTable: "State",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CausalAdjustmentSet_DefinitionHash",
                schema: "causal",
                table: "CausalAdjustmentSet",
                column: "DefinitionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CausalAdjustmentSet_VersionCode",
                schema: "causal",
                table: "CausalAdjustmentSet",
                column: "VersionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CausalAnalysisRow_CausalStudyId_BinaryTreatment",
                schema: "causal",
                table: "CausalAnalysisRow",
                columns: new[] { "CausalStudyId", "BinaryTreatment" });

            migrationBuilder.CreateIndex(
                name: "IX_CausalAnalysisRow_CausalStudyId_DrugId_StateId_ObservationQuarterId",
                schema: "causal",
                table: "CausalAnalysisRow",
                columns: new[] { "CausalStudyId", "DrugId", "StateId", "ObservationQuarterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CausalAnalysisRow_DrugId",
                schema: "causal",
                table: "CausalAnalysisRow",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalAnalysisRow_ObservationQuarterId",
                schema: "causal",
                table: "CausalAnalysisRow",
                column: "ObservationQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalAnalysisRow_OutcomeQuarterId",
                schema: "causal",
                table: "CausalAnalysisRow",
                column: "OutcomeQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalAnalysisRow_StateId",
                schema: "causal",
                table: "CausalAnalysisRow",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalDagDefinition_DefinitionHash",
                schema: "causal",
                table: "CausalDagDefinition",
                column: "DefinitionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CausalDagDefinition_VersionCode",
                schema: "causal",
                table: "CausalDagDefinition",
                column: "VersionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CausalDiagnostic_CausalStudyId_DiagnosticType_VariableName_MetricName",
                schema: "causal",
                table: "CausalDiagnostic",
                columns: new[] { "CausalStudyId", "DiagnosticType", "VariableName", "MetricName" });

            migrationBuilder.CreateIndex(
                name: "IX_CausalEstimate_CausalStudyId_EstimatorName_Estimand_EffectScale",
                schema: "causal",
                table: "CausalEstimate",
                columns: new[] { "CausalStudyId", "EstimatorName", "Estimand", "EffectScale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_CausalAdjustmentSetId",
                schema: "causal",
                table: "CausalStudy",
                column: "CausalAdjustmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_CausalDagDefinitionId",
                schema: "causal",
                table: "CausalStudy",
                column: "CausalDagDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_DatasetVersionId",
                schema: "causal",
                table: "CausalStudy",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_FeatureSetVersionId",
                schema: "causal",
                table: "CausalStudy",
                column: "FeatureSetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_ObservationEndQuarterId",
                schema: "causal",
                table: "CausalStudy",
                column: "ObservationEndQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_ObservationStartQuarterId",
                schema: "causal",
                table: "CausalStudy",
                column: "ObservationStartQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_StudyCode",
                schema: "causal",
                table: "CausalStudy",
                column: "StudyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CausalStudy_TreatmentDefinitionId",
                schema: "causal",
                table: "CausalStudy",
                column: "TreatmentDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CounterfactualResult_CounterfactualScenarioId_DrugId_StateId_ObservationQuarterId",
                schema: "causal",
                table: "CounterfactualResult",
                columns: new[] { "CounterfactualScenarioId", "DrugId", "StateId", "ObservationQuarterId" });

            migrationBuilder.CreateIndex(
                name: "IX_CounterfactualResult_DrugId",
                schema: "causal",
                table: "CounterfactualResult",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_CounterfactualResult_ObservationQuarterId",
                schema: "causal",
                table: "CounterfactualResult",
                column: "ObservationQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_CounterfactualResult_StateId",
                schema: "causal",
                table: "CounterfactualResult",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_CounterfactualScenario_CausalStudyId_ScenarioCode",
                schema: "causal",
                table: "CounterfactualScenario",
                columns: new[] { "CausalStudyId", "ScenarioCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentDefinition_DefinitionHash",
                schema: "causal",
                table: "TreatmentDefinition",
                column: "DefinitionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentDefinition_VersionCode",
                schema: "causal",
                table: "TreatmentDefinition",
                column: "VersionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CausalAnalysisRow",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "CausalDiagnostic",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "CausalEstimate",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "CounterfactualResult",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "CounterfactualScenario",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "CausalStudy",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "CausalAdjustmentSet",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "CausalDagDefinition",
                schema: "causal");

            migrationBuilder.DropTable(
                name: "TreatmentDefinition",
                schema: "causal");
        }
    }
}
