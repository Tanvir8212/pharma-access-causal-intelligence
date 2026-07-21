# Implementation status

## Current milestone

Milestone 4 — Predictive ML Baseline Vertical Slice — implemented and verified for `NextQuarterStateEntry`. Milestone 5 was not started.

## Environment and packages

- Selected stable SDK: .NET SDK 10.0.302; all 17 projects target `net10.0`.
- EF Core package family: 10.0.10 for `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Microsoft.EntityFrameworkCore.SqlServer`, and `Microsoft.EntityFrameworkCore.Design`.
- `Microsoft.EntityFrameworkCore.Design` is private to `PharmaAccess.Data` with explicit private assets.
- Repository-local tool: `dotnet-ef` 10.0.10 in `.config/dotnet-tools.json`; no global tool was installed.
- ML.NET 5.0.0 core, FastTree, and LightGBM packages are centrally managed and referenced only by `PharmaAccess.ML`. No Dapper, direct SqlClient, Gemini, authentication, mediator, Python, AutoML, ONNX, SHAP, or unrelated packages were added.
- Nullable reference analysis and implicit usings are enabled centrally; warnings remain errors.

## Projects modified

- `PharmaAccess.Domain`: pure value objects, entities, enums, invariant guards, and distribution calculations.
- `PharmaAccess.Application`: focused `IDatasetVersionRepository` persistence port; Application still has no Data dependency.
- `PharmaAccess.Data`: EF Core context, configurations, SQL Server DI registration, repository implementation, design-time factory, and migration.
- `PharmaAccess.Api`: empty non-secret connection-string placeholder only; existing health behavior is unchanged and the context is not resolved at startup.
- `PharmaAccess.Domain.Tests`: invariant, lifecycle, calculation, and strengthened dependency tests.
- `PharmaAccess.Application.Tests`: strengthened outer-layer dependency checks.
- `PharmaAccess.Data.Tests`: EF metadata/configuration tests without a database.

## Domain foundation

Value objects: `CalendarQuarter`, `StateCode`, `Percentage`, `AccessGapValue`, and `DatasetVersionCode`.

Entities: `Drug`, `DrugProduct`, `FirstGenericApproval`, `State`, `QuarterDimension`, `DatasetVersion`, `SourceFile`, `StateDrugUtilization`, and `JobRun`, with controlled dataset/source/quality/job enums.

Pure services implement Numeric Distribution, frozen-weight Weighted Distribution, and Access Gap. Undefined zero denominators throw explicit domain errors and never return zero or NaN. No rounding or research result generation occurs.

Dataset lifecycle transitions are explicit: Draft → Validating → Validated → Finalized → Archived, with rejection from draft/validation. Rejected or unvalidated versions cannot be finalized, and only finalization sets the UTC finalization time.

## Persistence foundation

`PharmaAccessDbContext` exposes DbSets for all nine Milestone 1 entities. Every entity uses a separate `IEntityTypeConfiguration<T>`. SQL Server mappings define keys, foreign keys, lengths, required fields, enum conversions, UTC materialization, indexes, unique constraints, `decimal(19,4)` reimbursement, and restrictive provenance deletion.

The migration defines only:

- `core`: Drug, DrugProduct, FirstGenericApproval, State, CalendarQuarter, DatasetVersion, SourceFile, StateDrugUtilization.
- `audit`: JobRun.

No `raw`, `stg`, `feature`, `ml`, `causal`, `rag`, or `research` table/schema is created.

`AddPharmaAccessData` configures SQL Server through DI. A connection string is required only when the context is resolved. The API does not resolve it for `/health`, does not call `Database.Migrate`, and does not require SQL Server for build or tests.

## Migration and database safety

- Migration: `20260720192846_InitialCoreFoundation` under `src/PharmaAccess.Data/Migrations`.
- Design-time strategy: Data-project `IDesignTimeDbContextFactory` with a non-secret LocalDB-shaped placeholder used only for model construction.
- EF migration list command used `--no-connect` with the Data project as both project and startup project.
- EF reported the migration and confirmed: `No changes have been made to the model since the last migration.`
- EF correctly could not show applied/pending status because connectivity was deliberately disabled.
- Review-only idempotent SQL generated at ignored `artifacts/reports/milestone-1/InitialCoreFoundation.idempotent.sql` (12,826 bytes).
- During initial migration generation and `--no-connect` verification, no database connection occurred. The later required API-startup migration listing used only the non-production design-time LocalDB placeholder for a read-only status check. No database was created or modified, no migration was applied, and no data was seeded.

## Verification

- `dotnet tool restore`: succeeded; local `dotnet-ef` 10.0.10 restored.
- `dotnet restore .\PharmaAccess.sln`: succeeded; all projects up to date.
- `dotnet build .\PharmaAccess.sln --no-restore`: succeeded with 0 warnings and 0 errors.
- `dotnet test .\PharmaAccess.sln --no-build`: succeeded; 83 passed, 0 failed, 0 skipped.
- Test runs contained only the seven projects under `tests`.
- Domain dependency audit: no NuGet packages and no project references.
- Authoritative blueprint and master-prompt SHA-256 hashes are unchanged from the completed Milestone 0 audit.

## Scope and safety audit

- No local or real source file was ingested and no previous-project data was accessed.
- No real secrets, credentials, or populated connection strings exist.
- Feature engineering foundation was added, but no ML.NET training, causal estimator, Gemini/RAG client, Python environment, research metrics, or fabricated records were added.
- No commit was created.

## Warnings and unresolved issues

- There is no available authorized isolated SQL Server test database, so SQL Server execution semantics were not integration-tested. EF metadata tests validate the SQL Server model without pretending the in-memory or SQLite provider proves SQL Server behavior.
- Applied migration state is intentionally not queried. Migration sources and the model snapshot are verified without a database connection.
- `PharmaAccess.Tests.sln` remains a redundant legacy convenience solution; documented verification uses `PharmaAccess.sln`.
- Remote CI was not run, and no project license has been selected.
- `git diff --check` found no whitespace errors; Git emitted Windows `core.autocrlf` LF-to-CRLF normalization notices for modified text files. These are not compiler warnings.

## EF API startup-project tooling repair

- Added a development-only `Microsoft.EntityFrameworkCore.Design` reference to `PharmaAccess.Api`, inheriting centrally managed stable version 10.0.10. The existing private Design reference in `PharmaAccess.Data` is preserved; no other project received the package.
- Final tool restore, solution restore, build, and all 83 tests succeeded; build produced 0 warnings and 0 errors.
- `dotnet ef migrations list --project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj --startup-project .\src\PharmaAccess.Api\PharmaAccess.Api.csproj` succeeded and reported `20260720192846_InitialCoreFoundation (Pending)`.
- The command used only the non-production design-time LocalDB placeholder to read migration status. It did not apply the migration, modify a database, or access an existing research database. `dotnet ef database update` was not run.

## Milestone 3 feature engineering foundation

- Added pure feature-set lifecycle, configurable state-entry policies, frozen historical market weights, ordinary growth, concentration metrics, and persistent-inequality policy.
- Added deterministic Application feature building at `GenericLaunchId × StateId × ObservationQuarter`, exact-quarter lags, nullable next-quarter/Q4 labels, dry-run isolation, reproducibility hashing, and a dedicated blocking leakage audit.
- Added `core.GenericLaunch` and six `feature` tables with restrictive lineage, controlled enums, precision, required indexes, and migration `AddFeatureEngineeringFoundation`; the migration was not applied.
- Added a machine-readable feature dictionary and a clearly labeled synthetic fixture. The executable pipeline test uses two launches, four states, 32 state-quarter observations, eight launch-quarter summaries, complete Q4 follow-up for one launch, and censored follow-up for the other.
- Milestone 3 added no ML.NET; Milestone 4 now adds predictive ML.NET only. No causal, Gemini/RAG, Python, real data, database connection, database update, or commit was added.
- Final verification: local tool restore and solution restore succeeded; build succeeded with 0 warnings and 0 errors; 127 tests passed with 0 failures and 0 skips. The synthetic dry-run generated 32 state-quarter rows and 8 launch-quarter summaries, emitted state/regional historical profiles, persisted nothing, and produced a stable SHA-256 reproducibility hash across repeated builds.
- EF reported no pending model changes after `AddFeatureEngineeringFoundation`; `migrations list --no-connect` listed all three migrations. A review-only idempotent script was generated under ignored `artifacts/reports/milestone-3/` and was not applied.

Known baseline limitation: the branch received for Milestone 3 still documents Milestone 2 importer orchestration as incomplete and lacks `docs/data/INGESTION_PIPELINE.md`. Milestone 3 therefore consumes explicit validated-snapshot contracts and does not claim an operational file-to-feature path.

## Milestone 4 predictive baseline

- Added centrally managed stable ML.NET 5.0.0 packages only to `PharmaAccess.ML`: core, FastTree/FastForest, and LightGBM. Domain remains ML.NET-free.
- Added a versioned 32-feature schema, prohibited-feature validation, nullable/censored-label exclusion, deterministic dataset/split hashes, and chronological GenericLaunch-grouped train/validation/test manifests.
- Added prevalence and historical-state-rate baselines; SDCA logistic regression, FastTree, FastForest, and LightGBM candidates; training-fitted imputation; logistic normalization; and custom PR AUC, ROC AUC, Brier, log-loss, confusion, and threshold metrics.
- Candidate selection uses validation PR AUC, then log loss, Brier score, and simplicity. Test is evaluated only after selection. Status remains `ValidationSelected`, never automatically `Approved`.
- Added path-safe immutable filesystem artifacts with SHA-256 validation, schema validation, model cards, reproducibility manifests, batch scoring, and a reference-only `/api/v1/predictions/next-state-entry` endpoint.
- Added `ml` EF entities/configurations for experiments, runs, metrics, artifacts, and optional development predictions. Migration `AddPredictiveMlFoundation` was generated but not applied.
- Synthetic workflow: 56 rows across seven launch groups; 14 positive and 42 negative labels (25% prevalence); partitions 24/16/16. FastTree was selected on validation. Synthetic validation/test PR AUC were both 1.0 on deliberately separable development data. These values are **synthetic development results, not research results** and imply no real-world performance.
- No causal inference, counterfactual simulation, Gemini/RAG, Python, real-data import, real-database connection, automatic promotion, or commit was performed.
- Final verification: tool restore and solution restore succeeded; build succeeded with 0 warnings and 0 errors; 140 tests passed with 0 failures and 0 skips. EF reported no pending model changes and listed all four migrations with `--no-connect`. The review-only idempotent script was generated under ignored `artifacts/reports/milestone-4/`; no migration was applied.

## Next milestone

Milestone 5 must not begin until Milestone 4 is accepted. Proposed scope: next predictive task(s), richer categorical handling and calibration/subgroup diagnostics, explicit validation-only threshold policies, and controlled experiment comparison—still without causal inference, Gemini/RAG, or Python unless a later authorized milestone requires them.
