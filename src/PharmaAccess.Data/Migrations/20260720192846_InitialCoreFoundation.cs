using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCoreFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "CalendarQuarter",
                schema: "core",
                columns: table => new
                {
                    QuarterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalendarYear = table.Column<int>(type: "int", nullable: false),
                    QuarterNumber = table.Column<int>(type: "int", nullable: false),
                    QuarterStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QuarterEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DisplayCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarQuarter", x => x.QuarterId);
                });

            migrationBuilder.CreateTable(
                name: "DatasetVersion",
                schema: "core",
                columns: table => new
                {
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SchemaVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FeatureVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeCommitHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TotalSourceFiles = table.Column<int>(type: "int", nullable: false),
                    TotalRows = table.Column<long>(type: "bigint", nullable: true),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetVersion", x => x.DatasetVersionId);
                });

            migrationBuilder.CreateTable(
                name: "Drug",
                schema: "core",
                columns: table => new
                {
                    DrugId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NormalizedIngredient = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IngredientCombination = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DosageForm = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RxNormId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TherapeuticClass = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drug", x => x.DrugId);
                });

            migrationBuilder.CreateTable(
                name: "State",
                schema: "core",
                columns: table => new
                {
                    StateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Division = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_State", x => x.StateId);
                });

            migrationBuilder.CreateTable(
                name: "JobRun",
                schema: "audit",
                columns: table => new
                {
                    JobRunId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRun", x => x.JobRunId);
                    table.ForeignKey(
                        name: "FK_JobRun_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourceFile",
                schema: "core",
                columns: table => new
                {
                    SourceFileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportingPeriod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: true),
                    RejectedRowCount = table.Column<long>(type: "bigint", nullable: true),
                    SchemaVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LicenseNote = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ImportStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceFile", x => x.SourceFileId);
                    table.ForeignKey(
                        name: "FK_SourceFile_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugProduct",
                schema: "core",
                columns: table => new
                {
                    DrugProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    OriginalNdc = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedNdc = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Labeler = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MappingConfidence = table.Column<double>(type: "float", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugProduct", x => x.DrugProductId);
                    table.ForeignKey(
                        name: "FK_DrugProduct_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FirstGenericApproval",
                schema: "core",
                columns: table => new
                {
                    ApprovalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ApplicationNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Applicant = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApprovalSource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsPrimaryLaunchReference = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstGenericApproval", x => x.ApprovalId);
                    table.ForeignKey(
                        name: "FK_FirstGenericApproval_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateDrugUtilization",
                schema: "core",
                columns: table => new
                {
                    StateDrugUtilizationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    DrugProductId = table.Column<int>(type: "int", nullable: true),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    QuarterId = table.Column<int>(type: "int", nullable: false),
                    PrescriptionCount = table.Column<long>(type: "bigint", nullable: false),
                    ReimbursementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SourceRowCount = table.Column<int>(type: "int", nullable: false),
                    IsSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    DataQualityStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DatasetVersionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateDrugUtilization", x => x.StateDrugUtilizationId);
                    table.ForeignKey(
                        name: "FK_StateDrugUtilization_CalendarQuarter_QuarterId",
                        column: x => x.QuarterId,
                        principalSchema: "core",
                        principalTable: "CalendarQuarter",
                        principalColumn: "QuarterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateDrugUtilization_DatasetVersion_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalSchema: "core",
                        principalTable: "DatasetVersion",
                        principalColumn: "DatasetVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateDrugUtilization_DrugProduct_DrugProductId",
                        column: x => x.DrugProductId,
                        principalSchema: "core",
                        principalTable: "DrugProduct",
                        principalColumn: "DrugProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateDrugUtilization_Drug_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "core",
                        principalTable: "Drug",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateDrugUtilization_State_StateId",
                        column: x => x.StateId,
                        principalSchema: "core",
                        principalTable: "State",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarQuarter_CalendarYear_QuarterNumber",
                schema: "core",
                table: "CalendarQuarter",
                columns: new[] { "CalendarYear", "QuarterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarQuarter_DisplayCode",
                schema: "core",
                table: "CalendarQuarter",
                column: "DisplayCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetVersion_VersionCode",
                schema: "core",
                table: "DatasetVersion",
                column: "VersionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drug_NormalizedIngredient",
                schema: "core",
                table: "Drug",
                column: "NormalizedIngredient");

            migrationBuilder.CreateIndex(
                name: "IX_DrugProduct_DrugId",
                schema: "core",
                table: "DrugProduct",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugProduct_SourceSystem_OriginalNdc",
                schema: "core",
                table: "DrugProduct",
                columns: new[] { "SourceSystem", "OriginalNdc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FirstGenericApproval_DrugId_ApprovalDate",
                schema: "core",
                table: "FirstGenericApproval",
                columns: new[] { "DrugId", "ApprovalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JobRun_CorrelationId",
                schema: "audit",
                table: "JobRun",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRun_DatasetVersionId",
                schema: "audit",
                table: "JobRun",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceFile_DatasetVersionId_Sha256",
                schema: "core",
                table: "SourceFile",
                columns: new[] { "DatasetVersionId", "Sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_State_StateCode",
                schema: "core",
                table: "State",
                column: "StateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateDrugUtilization_DatasetVersionId_DrugId_StateId_QuarterId",
                schema: "core",
                table: "StateDrugUtilization",
                columns: new[] { "DatasetVersionId", "DrugId", "StateId", "QuarterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateDrugUtilization_DrugId",
                schema: "core",
                table: "StateDrugUtilization",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_StateDrugUtilization_DrugProductId",
                schema: "core",
                table: "StateDrugUtilization",
                column: "DrugProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StateDrugUtilization_QuarterId",
                schema: "core",
                table: "StateDrugUtilization",
                column: "QuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_StateDrugUtilization_StateId",
                schema: "core",
                table: "StateDrugUtilization",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirstGenericApproval",
                schema: "core");

            migrationBuilder.DropTable(
                name: "JobRun",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "SourceFile",
                schema: "core");

            migrationBuilder.DropTable(
                name: "StateDrugUtilization",
                schema: "core");

            migrationBuilder.DropTable(
                name: "CalendarQuarter",
                schema: "core");

            migrationBuilder.DropTable(
                name: "DatasetVersion",
                schema: "core");

            migrationBuilder.DropTable(
                name: "DrugProduct",
                schema: "core");

            migrationBuilder.DropTable(
                name: "State",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Drug",
                schema: "core");
        }
    }
}
