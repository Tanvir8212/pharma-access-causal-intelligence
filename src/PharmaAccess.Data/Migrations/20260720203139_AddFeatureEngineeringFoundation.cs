using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureEngineeringFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "feature");

            migrationBuilder.CreateTable(
                name: "FeatureSetVersion",
                schema: "feature",
                columns: table => new
                {
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DefinitionHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CodeCommitHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureSetVersion", x => x.FeatureSetVersionId);
                    table.ForeignKey(
                        name: "FK_FeatureSetVersion_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GenericLaunch",
                schema: "core",
                columns: table => new
                {
                    GenericLaunchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    PrimaryApprovalId = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ApprovalQuarterId = table.Column<int>(type: "int", nullable: false),
                    LaunchReferenceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ObservationStartQuarterId = table.Column<int>(type: "int", nullable: false),
                    ObservationEndQuarterId = table.Column<int>(type: "int", nullable: true),
                    IsEligibleForAnalysis = table.Column<bool>(type: "bit", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenericLaunch", x => x.GenericLaunchId);
                    table.ForeignKey(
                        name: "FK_GenericLaunch_CalendarQuarter_ApprovalQuarterId",
                        column: x => x.ApprovalQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenericLaunch_CalendarQuarter_ObservationEndQuarterId",
                        column: x => x.ObservationEndQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenericLaunch_CalendarQuarter_ObservationStartQuarterId",
                        column: x => x.ObservationStartQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenericLaunch_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenericLaunch_FirstGenericApproval_PrimaryApprovalId",
                        column: x => x.PrimaryApprovalId,
                        principalSchema: "core",
                        principalTable: "FirstGenericApproval",
                        principalColumn: "ApprovalId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeatureDefinition",
                schema: "feature",
                columns: table => new
                {
                    FeatureDefinitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    FeatureName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AvailableAsOfRule = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MissingValuePolicy = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValidMinimum = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    ValidMaximum = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    LeakageRisk = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FeatureCategory = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsModelInput = table.Column<bool>(type: "bit", nullable: false),
                    IsLabel = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureDefinition", x => x.FeatureDefinitionId);
                    table.ForeignKey(
                        name: "FK_FeatureDefinition_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegionalHistoricalProfile",
                schema: "feature",
                columns: table => new
                {
                    RegionalHistoricalProfileId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AvailableAsOfQuarterId = table.Column<int>(type: "int", nullable: false),
                    HistoricalEntryRate = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    ActiveStateShare = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    PrescriptionGrowth = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    EligibleStateCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionalHistoricalProfile", x => x.RegionalHistoricalProfileId);
                    table.ForeignKey(
                        name: "FK_RegionalHistoricalProfile_CalendarQuarter_AvailableAsOfQuarterId",
                        column: x => x.AvailableAsOfQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegionalHistoricalProfile_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegionalHistoricalProfile_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateHistoricalProfile",
                schema: "feature",
                columns: table => new
                {
                    StateHistoricalProfileId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    AvailableAsOfQuarterId = table.Column<int>(type: "int", nullable: false),
                    HistoricalGenericVolume = table.Column<long>(type: "bigint", nullable: false),
                    HistoricalLaunchCount = table.Column<int>(type: "int", nullable: false),
                    HistoricalEntryRate = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    HistoricalMedianEntryDelay = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    HistoricalMarketWeight = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    VolumePercentile = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    DataCompleteness = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateHistoricalProfile", x => x.StateHistoricalProfileId);
                    table.ForeignKey(
                        name: "FK_StateHistoricalProfile_CalendarQuarter_AvailableAsOfQuarterId",
                        column: x => x.AvailableAsOfQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateHistoricalProfile_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateHistoricalProfile_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateHistoricalProfile_State_StateId",
                        column: x => x.StateId,
                        principalSchema: "core",
                        principalTable: "State",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugStateQuarterFeature",
                schema: "feature",
                columns: table => new
                {
                    DrugStateQuarterFeatureId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    GenericLaunchId = table.Column<int>(type: "int", nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    ObservationQuarterId = table.Column<int>(type: "int", nullable: false),
                    ApprovalQuarterId = table.Column<int>(type: "int", nullable: false),
                    QuarterSinceApproval = table.Column<int>(type: "int", nullable: false),
                    AvailableAsOfQuarterId = table.Column<int>(type: "int", nullable: false),
                    ObservedPrescriptionCount = table.Column<long>(type: "bigint", nullable: true),
                    ObservedReimbursementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    IsPresent = table.Column<bool>(type: "bit", nullable: false),
                    IsFirstEntryQuarter = table.Column<bool>(type: "bit", nullable: false),
                    IsObservedZero = table.Column<bool>(type: "bit", nullable: false),
                    IsMissing = table.Column<bool>(type: "bit", nullable: false),
                    IsSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    ConsecutiveActiveQuarterCount = table.Column<int>(type: "int", nullable: false),
                    Lag1PrescriptionCount = table.Column<long>(type: "bigint", nullable: true),
                    Lag2PrescriptionCount = table.Column<long>(type: "bigint", nullable: true),
                    Lag1ReimbursementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    Lag2ReimbursementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    PrescriptionGrowthRate = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    ReimbursementGrowthRate = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    InitialActiveStateCount = table.Column<int>(type: "int", nullable: false),
                    InitialPrescriptionVolume = table.Column<long>(type: "bigint", nullable: false),
                    PreviousQuarterNumericDistribution = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    PreviousQuarterWeightedDistribution = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    PreviousQuarterAccessGap = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    NationalActiveStateSharePreviousQuarter = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    NationalPrescriptionCountPreviousQuarter = table.Column<long>(type: "bigint", nullable: true),
                    NationalReimbursementPreviousQuarter = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    LaunchCohort = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NumberOfObservedQuarters = table.Column<int>(type: "int", nullable: false),
                    StateHistoricalGenericVolume = table.Column<long>(type: "bigint", nullable: false),
                    StateHistoricalLaunchCount = table.Column<int>(type: "int", nullable: false),
                    StateHistoricalEntryRate = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    StateHistoricalMedianEntryDelay = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    StateHistoricalMarketWeight = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    StateVolumePercentile = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    StateDataCompleteness = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    RegionActiveStateShare = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    RegionHistoricalEntryRate = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    NeighborStateAdoptionShare = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    SimilarStateAdoptionShare = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    RegionPrescriptionGrowth = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    NationalPrescriptionGrowth = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    LabelNextQuarterEntry = table.Column<bool>(type: "bit", nullable: true),
                    LabelQuartersUntilEntry = table.Column<int>(type: "int", nullable: true),
                    LabelFutureQ4NumericDistribution = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    LabelFutureQ4WeightedDistribution = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    LabelFutureQ4AccessGap = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    LabelPersistentInequality = table.Column<bool>(type: "bit", nullable: true),
                    DataQualityStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MissingFeatureCount = table.Column<int>(type: "int", nullable: false),
                    FeatureDefinitionHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugStateQuarterFeature", x => x.DrugStateQuarterFeatureId);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_CalendarQuarter_ApprovalQuarterId",
                        column: x => x.ApprovalQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_CalendarQuarter_AvailableAsOfQuarterId",
                        column: x => x.AvailableAsOfQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_CalendarQuarter_ObservationQuarterId",
                        column: x => x.ObservationQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_GenericLaunch_GenericLaunchId",
                        column: x => x.GenericLaunchId,
                        principalSchema: "core",
                        principalTable: "GenericLaunch",
                        principalColumn: "GenericLaunchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugStateQuarterFeature_State_StateId",
                        column: x => x.StateId,
                        principalSchema: "core",
                        principalTable: "State",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LaunchQuarterSummary",
                schema: "feature",
                columns: table => new
                {
                    LaunchQuarterSummaryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureSetVersionId = table.Column<int>(type: "int", nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    GenericLaunchId = table.Column<int>(type: "int", nullable: false),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    ObservationQuarterId = table.Column<int>(type: "int", nullable: false),
                    QuarterSinceApproval = table.Column<int>(type: "int", nullable: false),
                    ActiveStateCount = table.Column<int>(type: "int", nullable: false),
                    EligibleStateCount = table.Column<int>(type: "int", nullable: false),
                    NumericDistribution = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    WeightedDistribution = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    AccessGap = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    MarketWeightVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TotalPrescriptionCount = table.Column<long>(type: "bigint", nullable: false),
                    TotalReimbursementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ConcentrationIndex = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    TopStateShare = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    TopFiveStateShare = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    RegionalCoverageCount = table.Column<int>(type: "int", nullable: false),
                    IsCompleteQuarter = table.Column<bool>(type: "bit", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaunchQuarterSummary", x => x.LaunchQuarterSummaryId);
                    table.ForeignKey(
                        name: "FK_LaunchQuarterSummary_CalendarQuarter_ObservationQuarterId",
                        column: x => x.ObservationQuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LaunchQuarterSummary_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LaunchQuarterSummary_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LaunchQuarterSummary_FeatureSetVersion_FeatureSetVersionId",
                        column: x => x.FeatureSetVersionId,
                        principalSchema: "feature",
                        principalTable: "FeatureSetVersion",
                        principalColumn: "FeatureSetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LaunchQuarterSummary_GenericLaunch_GenericLaunchId",
                        column: x => x.GenericLaunchId,
                        principalSchema: "core",
                        principalTable: "GenericLaunch",
                        principalColumn: "GenericLaunchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_ApprovalQuarterId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                column: "ApprovalQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_AvailableAsOfQuarterId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                column: "AvailableAsOfQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_DatasetVersionId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_DrugId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_FeatureSetVersionId_DrugId_StateId_ObservationQuarterId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                columns: new[] { "FeatureSetVersionId", "DrugId", "StateId", "ObservationQuarterId" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_FeatureSetVersionId_GenericLaunchId_StateId_ObservationQuarterId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                columns: new[] { "FeatureSetVersionId", "GenericLaunchId", "StateId", "ObservationQuarterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_GenericLaunchId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                column: "GenericLaunchId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_ObservationQuarterId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                column: "ObservationQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugStateQuarterFeature_StateId",
                schema: "feature",
                table: "DrugStateQuarterFeature",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureDefinition_FeatureSetVersionId_FeatureName",
                schema: "feature",
                table: "FeatureDefinition",
                columns: new[] { "FeatureSetVersionId", "FeatureName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSetVersion_DatasetVersionId",
                schema: "feature",
                table: "FeatureSetVersion",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSetVersion_VersionCode",
                schema: "feature",
                table: "FeatureSetVersion",
                column: "VersionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GenericLaunch_ApprovalQuarterId",
                schema: "core",
                table: "GenericLaunch",
                column: "ApprovalQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_GenericLaunch_DrugId_ApprovalQuarterId",
                schema: "core",
                table: "GenericLaunch",
                columns: new[] { "DrugId", "ApprovalQuarterId" });

            migrationBuilder.CreateIndex(
                name: "IX_GenericLaunch_ObservationEndQuarterId",
                schema: "core",
                table: "GenericLaunch",
                column: "ObservationEndQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_GenericLaunch_ObservationStartQuarterId",
                schema: "core",
                table: "GenericLaunch",
                column: "ObservationStartQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_GenericLaunch_PrimaryApprovalId",
                schema: "core",
                table: "GenericLaunch",
                column: "PrimaryApprovalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LaunchQuarterSummary_DatasetVersionId",
                schema: "feature",
                table: "LaunchQuarterSummary",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LaunchQuarterSummary_DrugId",
                schema: "feature",
                table: "LaunchQuarterSummary",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_LaunchQuarterSummary_FeatureSetVersionId_DrugId_ObservationQuarterId",
                schema: "feature",
                table: "LaunchQuarterSummary",
                columns: new[] { "FeatureSetVersionId", "DrugId", "ObservationQuarterId" });

            migrationBuilder.CreateIndex(
                name: "IX_LaunchQuarterSummary_FeatureSetVersionId_GenericLaunchId_ObservationQuarterId",
                schema: "feature",
                table: "LaunchQuarterSummary",
                columns: new[] { "FeatureSetVersionId", "GenericLaunchId", "ObservationQuarterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LaunchQuarterSummary_GenericLaunchId",
                schema: "feature",
                table: "LaunchQuarterSummary",
                column: "GenericLaunchId");

            migrationBuilder.CreateIndex(
                name: "IX_LaunchQuarterSummary_ObservationQuarterId",
                schema: "feature",
                table: "LaunchQuarterSummary",
                column: "ObservationQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionalHistoricalProfile_AvailableAsOfQuarterId",
                schema: "feature",
                table: "RegionalHistoricalProfile",
                column: "AvailableAsOfQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionalHistoricalProfile_DatasetVersionId",
                schema: "feature",
                table: "RegionalHistoricalProfile",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionalHistoricalProfile_FeatureSetVersionId_Region_AvailableAsOfQuarterId",
                schema: "feature",
                table: "RegionalHistoricalProfile",
                columns: new[] { "FeatureSetVersionId", "Region", "AvailableAsOfQuarterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateHistoricalProfile_AvailableAsOfQuarterId",
                schema: "feature",
                table: "StateHistoricalProfile",
                column: "AvailableAsOfQuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_StateHistoricalProfile_DatasetVersionId",
                schema: "feature",
                table: "StateHistoricalProfile",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StateHistoricalProfile_FeatureSetVersionId_StateId_AvailableAsOfQuarterId",
                schema: "feature",
                table: "StateHistoricalProfile",
                columns: new[] { "FeatureSetVersionId", "StateId", "AvailableAsOfQuarterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateHistoricalProfile_StateId",
                schema: "feature",
                table: "StateHistoricalProfile",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugStateQuarterFeature",
                schema: "feature");

            migrationBuilder.DropTable(
                name: "FeatureDefinition",
                schema: "feature");

            migrationBuilder.DropTable(
                name: "LaunchQuarterSummary",
                schema: "feature");

            migrationBuilder.DropTable(
                name: "RegionalHistoricalProfile",
                schema: "feature");

            migrationBuilder.DropTable(
                name: "StateHistoricalProfile",
                schema: "feature");

            migrationBuilder.DropTable(
                name: "GenericLaunch",
                schema: "core");

            migrationBuilder.DropTable(
                name: "FeatureSetVersion",
                schema: "feature");
        }
    }
}
