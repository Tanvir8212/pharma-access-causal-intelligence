using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictiveMlFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ml");

            migrationBuilder.CreateTable(
                name: "MlExperiment",
                schema: "ml",
                columns: table => new
                {
                    ExperimentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExperimentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    FeatureSelectionVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SplitManifestVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResearchQuestion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PrimaryMetric = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RandomSeed = table.Column<int>(type: "int", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CodeCommitHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailureMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlExperiment", x => x.ExperimentId);
                    table.ForeignKey(
                        name: "FK_MlExperiment_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MlExperiment_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelTrainingRun",
                schema: "ml",
                columns: table => new
                {
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExperimentId = table.Column<long>(type: "bigint", nullable: false),
                    TrainerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HyperparametersJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    TrainingRowCount = table.Column<int>(type: "int", nullable: false),
                    ValidationRowCount = table.Column<int>(type: "int", nullable: false),
                    TestRowCount = table.Column<int>(type: "int", nullable: false),
                    PositiveTrainingCount = table.Column<int>(type: "int", nullable: false),
                    NegativeTrainingCount = table.Column<int>(type: "int", nullable: false),
                    FeatureCount = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrainingDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailureMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelTrainingRun", x => x.ModelTrainingRunId);
                    table.ForeignKey(
                        name: "FK_ModelTrainingRun_MlExperiment_ExperimentId",
                        column: x => x.ExperimentId,
                        principalSchema: "ml",
                        principalTable: "MlExperiment",
                        principalColumn: "ExperimentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelArtifact",
                schema: "ml",
                columns: table => new
                {
                    ModelArtifactId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    ModelVersionCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ArtifactPath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    InputSchemaHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    FeatureSchemaHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ApprovalNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelArtifact", x => x.ModelArtifactId);
                    table.ForeignKey(
                        name: "FK_ModelArtifact_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelMetric",
                schema: "ml",
                columns: table => new
                {
                    ModelMetricId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    Partition = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MetricValue = table.Column<double>(type: "float(53)", nullable: false),
                    Threshold = table.Column<double>(type: "float(53)", nullable: true),
                    SubgroupName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SubgroupValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelMetric", x => x.ModelMetricId);
                    table.ForeignKey(
                        name: "FK_ModelMetric_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PredictionRecord",
                schema: "ml",
                columns: table => new
                {
                    PredictionRecordId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelTrainingRunId = table.Column<long>(type: "bigint", nullable: false),
                    Partition = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    ObservationQuarterId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    Probability = table.Column<float>(type: "real", nullable: false),
                    PredictedLabel = table.Column<bool>(type: "bit", nullable: false),
                    Threshold = table.Column<double>(type: "float(53)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionRecord", x => x.PredictionRecordId);
                    table.ForeignKey(
                        name: "FK_PredictionRecord_CalendarQuarter_ObservationQuarterId",
                        column: x => x.ObservationQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionRecord_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionRecord_ModelTrainingRun_ModelTrainingRunId",
                        column: x => x.ModelTrainingRunId,
                        principalSchema: "ml",
                        principalTable: "ModelTrainingRun",
                        principalColumn: "ModelTrainingRunId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionRecord_State_StateId",
                        column: x => x.StateId,
                        principalSchema: "core",
                        principalTable: "State",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MlExperiment_DatasetVersionId",
                schema: "ml",
                table: "MlExperiment",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MlExperiment_FeatureSetVersionId",
                schema: "ml",
                table: "MlExperiment",
                column: "FeatureSetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MlExperiment_TaskName_ExperimentName",
                schema: "ml",
                table: "MlExperiment",
                columns: new[] { "TaskName", "ExperimentName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelArtifact_ModelTrainingRunId",
                schema: "ml",
                table: "ModelArtifact",
                column: "ModelTrainingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelArtifact_ModelVersionCode",
                schema: "ml",
                table: "ModelArtifact",
                column: "ModelVersionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelArtifact_Sha256",
                schema: "ml",
                table: "ModelArtifact",
                column: "Sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetric_ModelTrainingRunId_Partition_MetricName_SubgroupName_SubgroupValue",
                schema: "ml",
                table: "ModelMetric",
                columns: new[] { "ModelTrainingRunId", "Partition", "MetricName", "SubgroupName", "SubgroupValue" },
                unique: true,
                filter: "[SubgroupName] IS NOT NULL AND [SubgroupValue] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTrainingRun_ExperimentId_TrainerName",
                schema: "ml",
                table: "ModelTrainingRun",
                columns: new[] { "ExperimentId", "TrainerName" });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecord_DrugId",
                schema: "ml",
                table: "PredictionRecord",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecord_ModelTrainingRunId_Partition_DrugId_StateId_ObservationQuarterId",
                schema: "ml",
                table: "PredictionRecord",
                columns: new[] { "ModelTrainingRunId", "Partition", "DrugId", "StateId", "ObservationQuarterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecord_ObservationQuarterId",
                schema: "ml",
                table: "PredictionRecord",
                column: "ObservationQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionRecord_StateId",
                schema: "ml",
                table: "PredictionRecord",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelArtifact",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "ModelMetric",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "PredictionRecord",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "ModelTrainingRun",
                schema: "ml");

            migrationBuilder.DropTable(
                name: "MlExperiment",
                schema: "ml");
        }
    }
}
