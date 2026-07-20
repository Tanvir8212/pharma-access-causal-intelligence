using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRawAndStagingIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stg");

            migrationBuilder.EnsureSchema(
                name: "raw");

            migrationBuilder.CreateTable(
                name: "FdaFirstGenericApprovalNormalized",
                schema: "stg",
                columns: table => new
                {
                    StagingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    NormalizedIngredient = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DosageForm = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Applicant = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ApplicationNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "date", nullable: false),
                    MappingStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ValidationMessagesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FdaFirstGenericApprovalNormalized", x => x.StagingId);
                    table.ForeignKey(
                        name: "FK_FdaFirstGenericApprovalNormalized_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FdaFirstGenericApprovalRaw",
                schema: "raw",
                columns: table => new
                {
                    RawRecordId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationNumberRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProductNumberRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActiveIngredientRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DosageFormRaw = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    StrengthRaw = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ApplicantRaw = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ApprovalDateRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ParsedApprovalDate = table.Column<DateTime>(type: "date", nullable: true),
                    ParseStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FdaFirstGenericApprovalRaw", x => x.RawRecordId);
                    table.ForeignKey(
                        name: "FK_FdaFirstGenericApprovalRaw_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicaidStateDrugUtilizationNormalized",
                schema: "stg",
                columns: table => new
                {
                    StagingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    StateCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    OriginalNdc = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedNdc = table.Column<string>(type: "nchar(11)", fixedLength: true, maxLength: 11, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CalendarYear = table.Column<int>(type: "int", nullable: false),
                    QuarterNumber = table.Column<int>(type: "int", nullable: false),
                    PrescriptionCount = table.Column<long>(type: "bigint", nullable: false),
                    ReimbursementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    IsSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    MappingStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ValidationMessagesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicaidStateDrugUtilizationNormalized", x => x.StagingId);
                    table.ForeignKey(
                        name: "FK_MedicaidStateDrugUtilizationNormalized_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicaidStateDrugUtilizationRaw",
                schema: "raw",
                columns: table => new
                {
                    RawRecordId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    UtilizationTypeRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StateCodeRaw = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NdcRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProductNameRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PackageSizeRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    YearRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    QuarterRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PrescriptionCountRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReimbursementAmountRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ParsedYear = table.Column<int>(type: "int", nullable: true),
                    ParsedQuarter = table.Column<int>(type: "int", nullable: true),
                    ParsedPrescriptionCount = table.Column<long>(type: "bigint", nullable: true),
                    ParsedReimbursementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ParseStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicaidStateDrugUtilizationRaw", x => x.RawRecordId);
                    table.ForeignKey(
                        name: "FK_MedicaidStateDrugUtilizationRaw_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateReferenceNormalized",
                schema: "stg",
                columns: table => new
                {
                    StagingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    StateCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Division = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ValidationMessagesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReferenceNormalized", x => x.StagingId);
                    table.ForeignKey(
                        name: "FK_StateReferenceNormalized_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateReferenceRaw",
                schema: "raw",
                columns: table => new
                {
                    RawRecordId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    StateCodeRaw = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StateNameRaw = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RegionRaw = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DivisionRaw = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EligibilityRaw = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ParseStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReferenceRaw", x => x.RawRecordId);
                    table.ForeignKey(
                        name: "FK_StateReferenceRaw_SourceFile_SourceFileId",
                        column: x => x.SourceFileId,
                        principalSchema: "core",
                        principalTable: "SourceFile",
                        principalColumn: "SourceFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FdaFirstGenericApprovalNormalized_NormalizedIngredient_ApprovalDate",
                schema: "stg",
                table: "FdaFirstGenericApprovalNormalized",
                columns: new[] { "NormalizedIngredient", "ApprovalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FdaFirstGenericApprovalNormalized_SourceFileId_SourceRowNumber",
                schema: "stg",
                table: "FdaFirstGenericApprovalNormalized",
                columns: new[] { "SourceFileId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FdaFirstGenericApprovalNormalized_SourceFileId_ValidationStatus",
                schema: "stg",
                table: "FdaFirstGenericApprovalNormalized",
                columns: new[] { "SourceFileId", "ValidationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_FdaFirstGenericApprovalRaw_SourceFileId_ParseStatus",
                schema: "raw",
                table: "FdaFirstGenericApprovalRaw",
                columns: new[] { "SourceFileId", "ParseStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_FdaFirstGenericApprovalRaw_SourceFileId_SourceRowNumber",
                schema: "raw",
                table: "FdaFirstGenericApprovalRaw",
                columns: new[] { "SourceFileId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidStateDrugUtilizationNormalized_NormalizedNdc",
                schema: "stg",
                table: "MedicaidStateDrugUtilizationNormalized",
                column: "NormalizedNdc");

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidStateDrugUtilizationNormalized_SourceFileId_SourceRowNumber",
                schema: "stg",
                table: "MedicaidStateDrugUtilizationNormalized",
                columns: new[] { "SourceFileId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidStateDrugUtilizationNormalized_SourceFileId_ValidationStatus",
                schema: "stg",
                table: "MedicaidStateDrugUtilizationNormalized",
                columns: new[] { "SourceFileId", "ValidationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidStateDrugUtilizationNormalized_StateCode_CalendarYear_QuarterNumber",
                schema: "stg",
                table: "MedicaidStateDrugUtilizationNormalized",
                columns: new[] { "StateCode", "CalendarYear", "QuarterNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidStateDrugUtilizationRaw_SourceFileId_ParseStatus",
                schema: "raw",
                table: "MedicaidStateDrugUtilizationRaw",
                columns: new[] { "SourceFileId", "ParseStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicaidStateDrugUtilizationRaw_SourceFileId_SourceRowNumber",
                schema: "raw",
                table: "MedicaidStateDrugUtilizationRaw",
                columns: new[] { "SourceFileId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateReferenceNormalized_SourceFileId_SourceRowNumber",
                schema: "stg",
                table: "StateReferenceNormalized",
                columns: new[] { "SourceFileId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateReferenceNormalized_SourceFileId_StateCode",
                schema: "stg",
                table: "StateReferenceNormalized",
                columns: new[] { "SourceFileId", "StateCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateReferenceNormalized_SourceFileId_ValidationStatus",
                schema: "stg",
                table: "StateReferenceNormalized",
                columns: new[] { "SourceFileId", "ValidationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StateReferenceRaw_SourceFileId_ParseStatus",
                schema: "raw",
                table: "StateReferenceRaw",
                columns: new[] { "SourceFileId", "ParseStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StateReferenceRaw_SourceFileId_SourceRowNumber",
                schema: "raw",
                table: "StateReferenceRaw",
                columns: new[] { "SourceFileId", "SourceRowNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FdaFirstGenericApprovalNormalized",
                schema: "stg");

            migrationBuilder.DropTable(
                name: "FdaFirstGenericApprovalRaw",
                schema: "raw");

            migrationBuilder.DropTable(
                name: "MedicaidStateDrugUtilizationNormalized",
                schema: "stg");

            migrationBuilder.DropTable(
                name: "MedicaidStateDrugUtilizationRaw",
                schema: "raw");

            migrationBuilder.DropTable(
                name: "StateReferenceNormalized",
                schema: "stg");

            migrationBuilder.DropTable(
                name: "StateReferenceRaw",
                schema: "raw");
        }
    }
}
