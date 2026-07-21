using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModelEvaluationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureImportanceResult",
                schema: "ml",
                columns: table => new
                {
                    FeatureImportanceResultId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    FeatureName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BaselineValue = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    PermutedValue = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    ImportanceDelta = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    StandardDeviation = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    RepetitionCount = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureImportanceResult", x => x.FeatureImportanceResultId);
                    table.ForeignKey(
                        name: "FK_FeatureImportanceResult_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelApproval",
                schema: "ml",
                columns: table => new
                {
                    ModelApprovalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelArtifactId = table.Column<long>(type: "bigint", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApprovalReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetEnvironment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PromoteToChampion = table.Column<bool>(type: "bit", nullable: false),
                    ModelCardVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelApproval", x => x.ModelApprovalId);
                    table.ForeignKey(
                        name: "FK_ModelApproval_ModelArtifact_ModelArtifactId",
                        column: x => x.ModelArtifactId,
                        principalSchema: "ml",
                        principalTable: "ModelArtifact",
                        principalColumn: "ModelArtifactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelCalibration",
                schema: "ml",
                columns: table => new
                {
                    ModelCalibrationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Slope = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: true),
                    Intercept = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: true),
                    ValidationRowCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarningsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelCalibration", x => x.ModelCalibrationId);
                    table.ForeignKey(
                        name: "FK_ModelCalibration_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelErrorAnalysis",
                schema: "ml",
                columns: table => new
                {
                    ModelErrorAnalysisId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    ObservationQuarterId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActualLabel = table.Column<bool>(type: "bit", nullable: false),
                    Probability = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    PredictedLabel = table.Column<bool>(type: "bit", nullable: false),
                    Threshold = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    MissingFeatureCount = table.Column<int>(type: "int", nullable: false),
                    UncertaintyStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    WarningsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelErrorAnalysis", x => x.ModelErrorAnalysisId);
                    table.ForeignKey(
                        name: "FK_ModelErrorAnalysis_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelRegistryEntry",
                schema: "ml",
                columns: table => new
                {
                    ModelRegistryEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelArtifactId = table.Column<long>(type: "bigint", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsChampion = table.Column<bool>(type: "bit", nullable: false),
                    IsSynthetic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelRegistryEntry", x => x.ModelRegistryEntryId);
                    table.ForeignKey(
                        name: "FK_ModelRegistryEntry_ModelArtifact_ModelArtifactId",
                        column: x => x.ModelArtifactId,
                        principalSchema: "ml",
                        principalTable: "ModelArtifact",
                        principalColumn: "ModelArtifactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubgroupMetric",
                schema: "ml",
                columns: table => new
                {
                    SubgroupMetricId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    Partition = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Dimension = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubgroupValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MetricValue = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubgroupMetric", x => x.SubgroupMetricId);
                    table.ForeignKey(
                        name: "FK_SubgroupMetric_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThresholdEvaluation",
                schema: "ml",
                columns: table => new
                {
                    ThresholdEvaluationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    Policy = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Partition = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Threshold = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConstraintsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Precision = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    Recall = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    F1 = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    Specificity = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThresholdEvaluation", x => x.ThresholdEvaluationId);
                    table.ForeignKey(
                        name: "FK_ThresholdEvaluation_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalibrationBin",
                schema: "ml",
                columns: table => new
                {
                    CalibrationBinId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelCalibrationId = table.Column<long>(type: "bigint", nullable: false),
                    Partition = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Binning = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BinNumber = table.Column<int>(type: "int", nullable: false),
                    LowerBound = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    UpperBound = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    MeanProbability = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    ObservedRate = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationBin", x => x.CalibrationBinId);
                    table.ForeignKey(
                        name: "FK_CalibrationBin_ModelCalibration_ModelCalibrationId",
                        column: x => x.ModelCalibrationId,
                        principalSchema: "ml",
                        principalTable: "ModelCalibration",
                        principalColumn: "ModelCalibrationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalibrationMetric",
                schema: "ml",
                columns: table => new
                {
                    CalibrationMetricId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelCalibrationId = table.Column<long>(type: "bigint", nullable: false),
                    Partition = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MetricValue = table.Column<double>(type: "float(20)", precision: 20, scale: 10, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationMetric", x => x.CalibrationMetricId);
                    table.ForeignKey(
                        name: "FK_CalibrationMetric_ModelCalibration_ModelCalibrationId",
                        column: x => x.ModelCalibrationId,
                        principalSchema: "ml",
                        principalTable: "ModelCalibration",
                        principalColumn: "ModelCalibrationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationBin_ModelCalibrationId_Partition_Binning_BinNumber",
                schema: "ml",
                table: "CalibrationBin",
                columns: new[] { "ModelCalibrationId", "Partition", "Binning", "BinNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationMetric_ModelCalibrationId_Partition_MetricName",
                schema: "ml",
                table: "CalibrationMetric",
                columns: new[] { "ModelCalibrationId", "Partition", "MetricName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureImportanceResult_ModelTrainingRunId_MetricName_FeatureName",
                schema: "ml",
                table: "FeatureImportanceResult",
                columns: new[] { "ModelTrainingRunId", "MetricName", "FeatureName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelApproval_ModelArtifactId_CreatedAtUtc",
                schema: "ml",
                table: "ModelApproval",
                columns: new[] { "ModelArtifactId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelCalibration_ModelTrainingRunId_Method",
                schema: "ml",
                table: "ModelCalibration",
                columns: new[] { "ModelTrainingRunId", "Method" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelErrorAnalysis_ModelTrainingRunId_Category",
                schema: "ml",
                table: "ModelErrorAnalysis",
                columns: new[] { "ModelTrainingRunId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelRegistryEntry_ModelArtifactId_Environment",
                schema: "ml",
                table: "ModelRegistryEntry",
                columns: new[] { "ModelArtifactId", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelRegistryEntry_TaskName_Environment_IsChampion",
                schema: "ml",
                table: "ModelRegistryEntry",
                columns: new[] { "TaskName", "Environment", "IsChampion" },
                unique: true,
                filter: "[IsChampion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SubgroupMetric_ModelTrainingRunId_Partition_Dimension_SubgroupValue_MetricName",
                schema: "ml",
                table: "SubgroupMetric",
                columns: new[] { "ModelTrainingRunId", "Partition", "Dimension", "SubgroupValue", "MetricName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThresholdEvaluation_ModelTrainingRunId_Policy_Partition",
                schema: "ml",
                table: "ThresholdEvaluation",
                columns: new[] { "ModelTrainingRunId", "Policy", "Partition" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalibrationBin",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "CalibrationMetric",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "FeatureImportanceResult",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "ModelApproval",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "ModelErrorAnalysis",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "ModelRegistryEntry",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "SubgroupMetric",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "ThresholdEvaluation",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "ModelCalibration",
                schema: "ml");
        }
    }
}
