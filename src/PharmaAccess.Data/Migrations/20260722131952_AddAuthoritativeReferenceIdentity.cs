using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthoritativeReferenceIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reference");

            migrationBuilder.CreateSequence(
                name: "ReferenceSnapshotEntitySequence");

            migrationBuilder.CreateTable(
                name: "DrugsFdaSnapshot",
                schema: "reference",
                columns: table => new
                {
                    ReferenceSnapshotId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR [ReferenceSnapshotEntitySequence]"),
                    SourceRegistryId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RelativeArchivePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugsFdaSnapshot", x => x.ReferenceSnapshotId);
                    table.ForeignKey(
                        name: "FK_DrugsFdaSnapshot_ResearchSourceRegistration_SourceRegistryId",
                        column: x => x.SourceRegistryId,
                        principalSchema: "research",
                        principalTable: "ResearchSourceRegistration",
                        principalColumn: "SourceRegistryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NdcDirectorySnapshot",
                schema: "reference",
                columns: table => new
                {
                    ReferenceSnapshotId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR [ReferenceSnapshotEntitySequence]"),
                    SourceRegistryId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RelativeArchivePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: false),
                    DatasetUpdateDate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AggregateManifestSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NdcDirectorySnapshot", x => x.ReferenceSnapshotId);
                    table.ForeignKey(
                        name: "FK_NdcDirectorySnapshot_ResearchSourceRegistration_SourceRegistryId",
                        column: x => x.SourceRegistryId,
                        principalSchema: "research",
                        principalTable: "ResearchSourceRegistration",
                        principalColumn: "SourceRegistryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrangeBookSnapshot",
                schema: "reference",
                columns: table => new
                {
                    ReferenceSnapshotId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR [ReferenceSnapshotEntitySequence]"),
                    SourceRegistryId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RelativeArchivePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrangeBookSnapshot", x => x.ReferenceSnapshotId);
                    table.ForeignKey(
                        name: "FK_OrangeBookSnapshot_ResearchSourceRegistration_SourceRegistryId",
                        column: x => x.SourceRegistryId,
                        principalSchema: "research",
                        principalTable: "ResearchSourceRegistration",
                        principalColumn: "SourceRegistryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductFamilyIdentity",
                schema: "reference",
                columns: table => new
                {
                    ProductFamilyIdentityId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedApplicationNumber = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedProductNumber = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedIngredientSet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DosageForm = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedStrength = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFamilyIdentity", x => x.ProductFamilyIdentityId);
                });

            migrationBuilder.CreateTable(
                name: "RxNormSnapshot",
                schema: "reference",
                columns: table => new
                {
                    ReferenceSnapshotId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR [ReferenceSnapshotEntitySequence]"),
                    SourceRegistryId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RelativeArchivePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: false),
                    PublishedMd5 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReleaseVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RxNormSnapshot", x => x.ReferenceSnapshotId);
                    table.ForeignKey(
                        name: "FK_RxNormSnapshot_ResearchSourceRegistration_SourceRegistryId",
                        column: x => x.SourceRegistryId,
                        principalSchema: "research",
                        principalTable: "ResearchSourceRegistration",
                        principalColumn: "SourceRegistryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugsFdaApplication",
                schema: "reference",
                columns: table => new
                {
                    DrugsFdaApplicationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugsFdaSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationNumberRaw = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NormalizedApplicationNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ApplicationTypeRaw = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SponsorNameRaw = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugsFdaApplication", x => x.DrugsFdaApplicationId);
                    table.ForeignKey(
                        name: "FK_DrugsFdaApplication_DrugsFdaSnapshot_DrugsFdaSnapshotId",
                        column: x => x.DrugsFdaSnapshotId,
                        principalSchema: "reference",
                        principalTable: "DrugsFdaSnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugsFdaMarketingStatus",
                schema: "reference",
                columns: table => new
                {
                    DrugsFdaMarketingStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugsFdaSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    NormalizedApplicationNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    NormalizedProductNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MarketingStatusCode = table.Column<int>(type: "int", nullable: false),
                    MarketingStatusDescription = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugsFdaMarketingStatus", x => x.DrugsFdaMarketingStatusId);
                    table.ForeignKey(
                        name: "FK_DrugsFdaMarketingStatus_DrugsFdaSnapshot_DrugsFdaSnapshotId",
                        column: x => x.DrugsFdaSnapshotId,
                        principalSchema: "reference",
                        principalTable: "DrugsFdaSnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugsFdaProduct",
                schema: "reference",
                columns: table => new
                {
                    DrugsFdaProductId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugsFdaSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationNumberRaw = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NormalizedApplicationNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ProductNumberRaw = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NormalizedProductNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FormRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StrengthRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DrugNameRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActiveIngredientRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReferenceDrug = table.Column<bool>(type: "bit", nullable: true),
                    ReferenceStandard = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugsFdaProduct", x => x.DrugsFdaProductId);
                    table.ForeignKey(
                        name: "FK_DrugsFdaProduct_DrugsFdaSnapshot_DrugsFdaSnapshotId",
                        column: x => x.DrugsFdaSnapshotId,
                        principalSchema: "reference",
                        principalTable: "DrugsFdaSnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugsFdaTherapeuticEquivalence",
                schema: "reference",
                columns: table => new
                {
                    DrugsFdaTherapeuticEquivalenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugsFdaSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    NormalizedApplicationNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    NormalizedProductNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TherapeuticEquivalenceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugsFdaTherapeuticEquivalence", x => x.DrugsFdaTherapeuticEquivalenceId);
                    table.ForeignKey(
                        name: "FK_DrugsFdaTherapeuticEquivalence_DrugsFdaSnapshot_DrugsFdaSnapshotId",
                        column: x => x.DrugsFdaSnapshotId,
                        principalSchema: "reference",
                        principalTable: "DrugsFdaSnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NdcDirectoryPackage",
                schema: "reference",
                columns: table => new
                {
                    NdcDirectoryPackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NdcDirectorySnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    PackageNdcRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedPackageNdc = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProductNdcRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DescriptionRaw = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NdcDirectoryPackage", x => x.NdcDirectoryPackageId);
                    table.ForeignKey(
                        name: "FK_NdcDirectoryPackage_NdcDirectorySnapshot_NdcDirectorySnapshotId",
                        column: x => x.NdcDirectorySnapshotId,
                        principalSchema: "reference",
                        principalTable: "NdcDirectorySnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NdcDirectoryProduct",
                schema: "reference",
                columns: table => new
                {
                    NdcDirectoryProductId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NdcDirectorySnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    ProductNdcRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedProductNdc = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApplicationNumberRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NormalizedApplicationNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GenericNameRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BrandNameRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DosageFormRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RouteRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MarketingCategoryRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LabelerNameRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MarketingStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MarketingEndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NdcDirectoryProduct", x => x.NdcDirectoryProductId);
                    table.ForeignKey(
                        name: "FK_NdcDirectoryProduct_NdcDirectorySnapshot_NdcDirectorySnapshotId",
                        column: x => x.NdcDirectorySnapshotId,
                        principalSchema: "reference",
                        principalTable: "NdcDirectorySnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrangeBookProduct",
                schema: "reference",
                columns: table => new
                {
                    OrangeBookProductId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrangeBookSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    IngredientRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedIngredientSet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DosageFormRouteRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DosageForm = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TradeNameRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApplicantRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StrengthRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedStrength = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApplicationTypeRaw = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ApplicationNumberRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedApplicationNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProductNumberRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedProductNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TherapeuticEquivalenceCodeRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovalDateRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApprovalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReferenceListedDrugRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReferenceStandardRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProductTypeRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApplicantFullNameRaw = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrangeBookProduct", x => x.OrangeBookProductId);
                    table.ForeignKey(
                        name: "FK_OrangeBookProduct_OrangeBookSnapshot_OrangeBookSnapshotId",
                        column: x => x.OrangeBookSnapshotId,
                        principalSchema: "reference",
                        principalTable: "OrangeBookSnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductMappingEvidence",
                schema: "reference",
                columns: table => new
                {
                    ProductMappingEvidenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductFamilyIdentityId = table.Column<long>(type: "bigint", nullable: false),
                    MedicaidNormalizedNdc = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MappingTier = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConfidenceClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceSnapshotType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    SourceRecordHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CandidateCount = table.Column<int>(type: "int", nullable: false),
                    DecisionStatus = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMappingEvidence", x => x.ProductMappingEvidenceId);
                    table.ForeignKey(
                        name: "FK_ProductMappingEvidence_ProductFamilyIdentity_ProductFamilyIdentityId",
                        column: x => x.ProductFamilyIdentityId,
                        principalSchema: "reference",
                        principalTable: "ProductFamilyIdentity",
                        principalColumn: "ProductFamilyIdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RxNormConcept",
                schema: "reference",
                columns: table => new
                {
                    RxNormConceptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RxNormSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    RxCui = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TermType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceVocabulary = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NameRaw = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SuppressFlag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RxNormConcept", x => x.RxNormConceptId);
                    table.ForeignKey(
                        name: "FK_RxNormConcept_RxNormSnapshot_RxNormSnapshotId",
                        column: x => x.RxNormSnapshotId,
                        principalSchema: "reference",
                        principalTable: "RxNormSnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RxNormNdcAssociation",
                schema: "reference",
                columns: table => new
                {
                    RxNormNdcAssociationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RxNormSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<long>(type: "bigint", nullable: false),
                    RxCui = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NdcRaw = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedNdc = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsHistorical = table.Column<bool>(type: "bit", nullable: false),
                    SourceVocabulary = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RxNormNdcAssociation", x => x.RxNormNdcAssociationId);
                    table.ForeignKey(
                        name: "FK_RxNormNdcAssociation_RxNormSnapshot_RxNormSnapshotId",
                        column: x => x.RxNormSnapshotId,
                        principalSchema: "reference",
                        principalTable: "RxNormSnapshot",
                        principalColumn: "ReferenceSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaApplication_DrugsFdaSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "DrugsFdaApplication",
                columns: new[] { "DrugsFdaSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaApplication_NormalizedApplicationNumber",
                schema: "reference",
                table: "DrugsFdaApplication",
                column: "NormalizedApplicationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaMarketingStatus_DrugsFdaSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "DrugsFdaMarketingStatus",
                columns: new[] { "DrugsFdaSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaMarketingStatus_NormalizedApplicationNumber_NormalizedProductNumber",
                schema: "reference",
                table: "DrugsFdaMarketingStatus",
                columns: new[] { "NormalizedApplicationNumber", "NormalizedProductNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaProduct_DrugsFdaSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "DrugsFdaProduct",
                columns: new[] { "DrugsFdaSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaProduct_NormalizedApplicationNumber_NormalizedProductNumber",
                schema: "reference",
                table: "DrugsFdaProduct",
                columns: new[] { "NormalizedApplicationNumber", "NormalizedProductNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaSnapshot_SnapshotCode",
                schema: "reference",
                table: "DrugsFdaSnapshot",
                column: "SnapshotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaSnapshot_SourceRegistryId",
                schema: "reference",
                table: "DrugsFdaSnapshot",
                column: "SourceRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaSnapshot_SourceSha256",
                schema: "reference",
                table: "DrugsFdaSnapshot",
                column: "SourceSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugsFdaTherapeuticEquivalence_DrugsFdaSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "DrugsFdaTherapeuticEquivalence",
                columns: new[] { "DrugsFdaSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectoryPackage_NdcDirectorySnapshotId_SourceRowNumber",
                schema: "reference",
                table: "NdcDirectoryPackage",
                columns: new[] { "NdcDirectorySnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectoryPackage_NormalizedPackageNdc",
                schema: "reference",
                table: "NdcDirectoryPackage",
                column: "NormalizedPackageNdc");

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectoryProduct_NdcDirectorySnapshotId_SourceRowNumber",
                schema: "reference",
                table: "NdcDirectoryProduct",
                columns: new[] { "NdcDirectorySnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectoryProduct_NormalizedApplicationNumber",
                schema: "reference",
                table: "NdcDirectoryProduct",
                column: "NormalizedApplicationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectoryProduct_NormalizedProductNdc",
                schema: "reference",
                table: "NdcDirectoryProduct",
                column: "NormalizedProductNdc");

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectorySnapshot_SnapshotCode",
                schema: "reference",
                table: "NdcDirectorySnapshot",
                column: "SnapshotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectorySnapshot_SourceRegistryId",
                schema: "reference",
                table: "NdcDirectorySnapshot",
                column: "SourceRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_NdcDirectorySnapshot_SourceSha256",
                schema: "reference",
                table: "NdcDirectorySnapshot",
                column: "SourceSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrangeBookProduct_NormalizedApplicationNumber_NormalizedProductNumber",
                schema: "reference",
                table: "OrangeBookProduct",
                columns: new[] { "NormalizedApplicationNumber", "NormalizedProductNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OrangeBookProduct_OrangeBookSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "OrangeBookProduct",
                columns: new[] { "OrangeBookSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrangeBookSnapshot_SnapshotCode",
                schema: "reference",
                table: "OrangeBookSnapshot",
                column: "SnapshotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrangeBookSnapshot_SourceRegistryId",
                schema: "reference",
                table: "OrangeBookSnapshot",
                column: "SourceRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrangeBookSnapshot_SourceSha256",
                schema: "reference",
                table: "OrangeBookSnapshot",
                column: "SourceSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductFamilyIdentity_IdentityHash",
                schema: "reference",
                table: "ProductFamilyIdentity",
                column: "IdentityHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductFamilyIdentity_NormalizedApplicationNumber_NormalizedProductNumber",
                schema: "reference",
                table: "ProductFamilyIdentity",
                columns: new[] { "NormalizedApplicationNumber", "NormalizedProductNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMappingEvidence_MedicaidNormalizedNdc_RuleVersion_SourceSnapshotType_SourceSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "ProductMappingEvidence",
                columns: new[] { "MedicaidNormalizedNdc", "RuleVersion", "SourceSnapshotType", "SourceSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMappingEvidence_ProductFamilyIdentityId",
                schema: "reference",
                table: "ProductMappingEvidence",
                column: "ProductFamilyIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_RxNormConcept_RxCui",
                schema: "reference",
                table: "RxNormConcept",
                column: "RxCui");

            migrationBuilder.CreateIndex(
                name: "IX_RxNormConcept_RxNormSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "RxNormConcept",
                columns: new[] { "RxNormSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RxNormNdcAssociation_NormalizedNdc",
                schema: "reference",
                table: "RxNormNdcAssociation",
                column: "NormalizedNdc");

            migrationBuilder.CreateIndex(
                name: "IX_RxNormNdcAssociation_RxCui",
                schema: "reference",
                table: "RxNormNdcAssociation",
                column: "RxCui");

            migrationBuilder.CreateIndex(
                name: "IX_RxNormNdcAssociation_RxNormSnapshotId_SourceRowNumber",
                schema: "reference",
                table: "RxNormNdcAssociation",
                columns: new[] { "RxNormSnapshotId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RxNormSnapshot_SnapshotCode",
                schema: "reference",
                table: "RxNormSnapshot",
                column: "SnapshotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RxNormSnapshot_SourceRegistryId",
                schema: "reference",
                table: "RxNormSnapshot",
                column: "SourceRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_RxNormSnapshot_SourceSha256",
                schema: "reference",
                table: "RxNormSnapshot",
                column: "SourceSha256",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugsFdaApplication",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "DrugsFdaMarketingStatus",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "DrugsFdaProduct",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "DrugsFdaTherapeuticEquivalence",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "NdcDirectoryPackage",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "NdcDirectoryProduct",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "OrangeBookProduct",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "ProductMappingEvidence",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "RxNormConcept",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "RxNormNdcAssociation",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "DrugsFdaSnapshot",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "NdcDirectorySnapshot",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "OrangeBookSnapshot",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "ProductFamilyIdentity",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "RxNormSnapshot",
                schema: "reference");

            migrationBuilder.DropSequence(
                name: "ReferenceSnapshotEntitySequence");
        }
    }
}
