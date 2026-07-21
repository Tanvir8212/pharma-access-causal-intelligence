# Implementation status

## Milestone 9 current status

Phase 9A execution preparation is implemented; phase 9B is blocked. The repository contains locally supplied private FDA, Medicaid, and reference files and a private assignment manifest. The dedicated research database exists with all eight migrations and its verified ownership marker. There is still no approved real protocol or standing `PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE=YES` authorization for import. No real row was imported and no real freeze candidate was approved.

The repaired guarded migration subsequently completed under human control. Read-only verification found exactly all eight expected migrations, the exact `PharmaAccessCausalIntelligence` / `pharma-access-causal-intelligence` ownership marker, eight expected application schemas, and all 57 expected user/migration tables with no unexpected table. The database baseline is safe, but this alone does not authorize import.

Final 9B readiness repair classifies FDA 2024 record 79 (physical CSV line 80) as an explicit non-data continuation fragment: only the dosage-strength continuation and SourceYear are populated, immediately after ANDA 218112; all approval identity/date fields are blank. Converted FDA annual CSVs are canonical and originals/XLSX/ZIP are provenance-only. Complete validation reports 26,212,199 canonical rows: 26,212,198 accepted, zero rejected, one explicitly excluded and zero blocking findings. Four ambiguous ten-digit and two invalid Medicaid NDCs remain unresolved review items; suppressed amounts remain missing rather than zero. The database contains no real protocol or approval row, so `approval-to-access-real` version `1.0` is only a proposed identifier and cannot yet be approved. The guarded import readiness preview correctly exits nonzero at that gate and writes no rows. Confirmed persistence remains deliberately unavailable pending protocol definition/approval and separate persistence-adapter review; Milestone 9B is not safe to execute.

Verification after the readiness repair: the changed Application and Application-test projects built with 0 warnings/errors and all 237 .NET tests passed. A full solution build attempt was blocked only by inaccessible NuGet repository-signature metadata for unchanged API/ML projects, including after an external retry; no compiler or test failure occurred. `pip check` found no broken requirements and all 21 Python tests passed with 12 upstream/cache warnings. No import, dataset finalization, freeze approval, predictive training, causal estimation, Milestone 10 work or commit occurred.

9A adds Git protection for `data/private/**`, canonical local-path and reparse-point enforcement, bounded source discovery, explicit/hash-stable assignments, safe ZIP/GZIP extraction, dry-run delimited schema validation, conservative NDC diagnostics, versioned mapping review, deterministic checkpoints/reconciliation, a distinct real-dataset lifecycle, dedicated-database preflight/write gating, sanitized reports, worker commands, synthetic/private-like tests, operator runbooks, and ADRs. Final verification results are recorded at completion below; Milestone 10 is not started.

Final Milestone 9A verification: repository-local tool restore and solution restore succeeded; build succeeded with 0 warnings/errors; all 234 .NET tests passed; `pip check` reported no broken requirements; all 21 Python tests passed with 11 upstream SHAP Matplotlib/pydot pyparsing deprecation warnings. Migration `20260721041312_AddResearchDatabaseOwnership` adds only the project ownership marker; EF reports no pending model changes and lists all eight migrations with `--no-connect`. A review-only idempotent SQL script was generated under ignored `artifacts/reports/milestone-9/`; no migration was applied and no database was contacted. The sanitized ignored report `artifacts/research-audit/m9-preflight-final.json` confirms 9A can run and 9B is blocked by five findings: missing assignments, unapproved source dry run, unavailable dedicated database, missing approved real protocol, and dirty repository without an approved exception.

## Milestone 9 failed-migration repair

The guarded migration attempt applied `InitialCoreFoundation`, `AddRawAndStagingIngestion`, and `AddFeatureEngineeringFoundation`, then failed transactionally in the previously unapplied `AddPredictiveMlFoundation`: decimal-style `HasPrecision(20,10)` facets on approximate CLR numbers generated invalid SQL Server `real(20)` for `float` and unsuitable `float(20)` for `double`. The same configuration defect affected 44 columns across the predictive, model-evaluation, and causal migration chain. The unapplied chain, all corresponding designers, and the final snapshot were repaired in place. The explicit storage policy is now C# `float` to SQL Server `real`, C# `double` to SQL Server `float(53)`, and precision/scale facets only on true decimal properties.

The guarded migration script now checks `$LASTEXITCODE` after every important `dotnet` and `sqlcmd` invocation, returns nonzero through its failure trap, stops before ownership insertion after an EF failure, verifies the ownership table exists, and verifies the inserted `ProjectId` before printing success. Exact target checks, the explicit write gate, `ShouldProcess`, and the refusal to create a database remain intact.

Repair-only verification succeeded without connecting to or modifying SQL Server: PowerShell parsing succeeded; the invalid approximate-type scan returned no matches; the solution built with 0 warnings/errors; all 234 .NET tests passed; and EF reported `No changes have been made to the model since the last migration.` The fresh idempotent review script at `artifacts/reports/milestone-9/AllMigrationsRepaired.sql` contains 44 valid approximate-number declarations and no `real(n)`, non-53 `float(n)`, or destructive drop/truncate/delete statement. The migration was not retried. The database remains partially migrated with only the first three migrations applied and no ownership marker. Real import, Milestone 10, and commit remain prohibited pending explicit authorization and resolution of the FDA blocking row, Medicaid NDC ambiguity, protocol approval, migration application, ownership verification, and dirty-worktree exception.

## Milestone 9 actual preflight and real-source dry run

The ignored assignment manifest `data/private/mappings/research-sources.private.json` registers 24 recognized local files by root-relative path, SHA-256, profile candidate and detected coverage year. The download-hash ledger is reported as unrecognized audit metadata and is not treated as a research source. Required FDA, Medicaid, state/region and adjacency categories are present; optional RxNorm/manual mapping directories are empty. No duplicate or conflicting assignment was found.

The repaired streaming dry run inspected 26,285,767 records without persisting rows: 26,285,765 accepted and 2 rejected, with 4 field errors. All assigned hashes and schemas matched. The two rejected representations are the same FDA 2024 source record in converted and archival CSVs; it lacks both ANDA number and approval date and is blocking. Medicaid files contain 26,192,645 valid 11-digit NDC observations, four ambiguous ten-digit values and two invalid values, all in 2021; ambiguity remains unresolved. Suppressed Medicaid rows retain blank prescription/reimbursement values and are accepted as suppression warnings rather than converted to zero. Ten adjacency records with blank neighbor GEOID and five dictionary row-format anomalies are nonblocking warnings. Original FDA CSVs were detected as ISO-8859-1; converted/data/reference CSVs are UTF-8. The local Orange Book ZIP and archival XLSX files passed structural/archive safety checks.

Read-only SQL preflight used the configured integrated-security target `Server=.` and exact database name `PharmaAccessCausalIntelligence_ResearchDev`. SQL Server 14.0.1000.169 Developer Edition connected successfully, but the dedicated database does not exist. Therefore it has no migration history or ownership marker, all eight migrations are pending, and no table/ownership inspection or write was attempted. `PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE` remained disabled. `scripts/Invoke-ResearchDatabaseMigration.ps1` now provides an explicit-write-gated, exact-target, ownership-aware, confirmable migration path and refuses to create the database; it was not executed. Milestone 9B is unsafe: the FDA blocker, absent database/ownership marker, unapplied migrations, dirty repository exception, and still-unapproved real research protocol must be resolved first. No functional real-row import command is authorized yet. No import, feature build, split, causal readiness analysis, freeze approval, predictive training or causal estimation ran.

## Milestone 8 current verification

Research Data Freeze and Pre-Analysis Protocol are implemented for deterministic synthetic development data only. The implementation adds explicit protocol review/approval, source registration with local-path and SHA-256 controls, immutable freeze lifecycle and lineage, cohort/exclusion manifests, predictive test-partition locking, a frozen observational-causal protocol, 30 blocking quality gates, profiling, environment and Git-safety manifests, and a hash-verifiable 24-artifact freeze bundle. The synthetic workflow reaches `ReadyForAnalysis`; this authorizes no real-data analysis and produces no predictive or causal research result.

Persistence adds ten restrictive-lineage tables under `research` and migration `20260721022430_AddResearchFreezeFoundation`. EF reports no pending model changes. A review-only idempotent SQL script was generated under ignored `artifacts/reports/milestone-8/`; no migration was applied and no database was contacted or modified.

Verification: solution restore succeeded; the final build succeeded with 0 warnings/errors; all 221 .NET tests passed; `pip check` succeeded; all 21 Python tests passed with 11 upstream SHAP Matplotlib/pydot pyparsing deprecation warnings. The synthetic freeze produced 24 required artifacts plus `file_hashes.json`, passed hash verification, and reproduced SHA-256 `14CBFA296D05EECD6066E7C17E6FC124277D083A2BECCDC1693571366D4DC83D` across deterministic runs. No real or previous-project data was accessed, no full training or final causal estimation was run, no Gemini/RAG work was started, no database update was performed, and no commit was created.

## Milestone 7 current verification

Independent Python causal validation and cross-language estimate parity are implemented and verified for deterministic synthetic development data only. Contract 1.1 replaces the invalid numeric C# enum representation with explicit estimator, estimand, and effect-scale strings; Python rejects unknown, numeric, duplicate, or missing identifiers before parity. The standard AIPW ATE hand fixture produces row contributions `0.875` and `0.6333333333333333`, mean `0.7541666666666667`.

Verification: `pip check` succeeded; 8 focused Python tests passed; all 21 Python tests passed; the deterministic 465-row C#/Python workflow passed all four formula comparisons. Solution restore succeeded, a full post-change build succeeded with 0 warnings/errors, and all 195 .NET tests passed. A subsequent redundant build attempt after restore was blocked while NuGet tried to fetch repository-signature metadata, despite an escalated retry; this did not affect the already successful build/test or parity results. Remaining warnings are upstream SHAP Matplotlib and pydot pyparsing deprecations. No database was contacted, no migration was created/applied, and Milestone 8/Gemini/RAG were not started.

## Historical status header (superseded below)

Milestone 4 — Predictive ML Baseline Vertical Slice — implemented and verified for `NextQuarterStateEntry`. Milestone 5 was not started.

## Current milestone

Milestone 6 — Causal Inference Foundation and Counterfactual Simulation — implemented and verified with deterministic synthetic development data only. This historical line is superseded by the Milestone 7 verification section above.

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

## Milestone 5 model evaluation, calibration, and error analysis

- Extended only `NextQuarterStateEntry` with validation-only Platt calibration; retained raw probabilities; explicit calibration status; Brier, log-loss, ECE, MCE, slope/intercept and configurable calibration bins. Small/single-class validation inputs fail safely, and the frozen calibrator alone evaluates test rows.
- Added support-based uncertainty (`Low`, `Moderate`, `High`, `Unsupported`) using threshold distance, calibration support, missing/out-of-range/unseen inputs, distribution distance, model disagreement and schema compatibility. It is explicitly not a confidence interval or clinical conclusion.
- Added deterministic repeated validation-only permutation importance, safe subgroup evaluation across eight dimensions, structured error categories, and six validation-only threshold policies. Test labels cannot fit calibration, select thresholds or calculate model-selection importance.
- Added explicit audited approval with artifact integrity/schema checks, development-only synthetic restriction, and controlled champion replacement. Training still cannot approve or promote a model; no approval endpoint was exposed.
- Added nine restricted-lineage tables under `ml` and migration `20260721003057_AddModelEvaluationFoundation`. EF reports no pending model changes. The migration was not applied and no database connection or update occurred.
- Generated eight JSON/Markdown synthetic development reports plus a review-only idempotent SQL script under ignored `artifacts/reports/milestone-5/`. Every evaluation report states: **Synthetic development data — not research results.**
- No packages were added. Existing stable ML.NET 5.0.0 and EF Core 10.0.10 package families are unchanged. Domain remains free of ML.NET and EF Core.
- Verification: local tool restore and solution restore succeeded; build succeeded with 0 warnings and 0 errors; 161 tests passed, 0 failed, 0 skipped. EF `migrations list --no-connect` listed all five migrations including `AddModelEvaluationFoundation`.
- No causal inference, counterfactual simulation, Gemini/RAG, Python, full real-data training, database migration application, automatic model promotion, or commit was performed.

Unresolved: SQL Server execution is not integration-tested because no isolated test database was authorized; EF metadata and generated SQL were verified without substituting an in-memory provider. The synthetic workflow establishes mechanics only and provides no research performance evidence.

## Next milestone

Milestone 6 is not started and requires explicit authorization. Proposed scope: authenticated administrative approval/deployment controls and operational model-governance integration for `NextQuarterStateEntry`; causal inference, counterfactuals, Gemini/RAG, Python and real-data execution remain separately governed.

## Milestone 6 causal inference foundation

- Added `PeerStateExposureToNextQuarterEntry` at eligible Drug × State × ObservationQuarter grain, with observation-quarter time zero, next-quarter binary first-entry outcome, explicit censoring exclusion, and blocking temporal-order findings.
- Added versioned neighbor, regional, similar-state, and early-large-market exposure definitions that preserve continuous exposure and a prespecified binary assignment. Added human-authored DAG v1 JSON/DOT, leakage-safe adjustment definitions, and an assumption registry that acknowledges partial interference rather than silently claiming SUTVA.
- Primary estimand is ATT risk difference; ATE is supported. Implemented descriptive unadjusted difference, propensity weighting, binary outcome regression, and AIPW. The deterministic C# logistic propensity model excludes outcome variables and is independent of the Milestone 4 predictive model.
- Added ATT/ATE weights, explicit clipping, extreme-weight reporting, effective sample size, common-support/positivity diagnostics, unweighted/weighted SMD, variance ratio, missingness difference, grouped-launch percentile bootstrap, future/pre-treatment/permutation placebos, sensitivity records, and safely suppressed exploratory subgroup estimates.
- Added support-aware counterfactual scenarios with `Supported`, `WeaklySupported`, `Extrapolative`, and `Unsupported` classifications. Results retain study/estimator/scenario lineage and are described as model-based observational scenarios, never guaranteed outcomes.
- Added nine restrictive-lineage EF tables under schema `causal` and migration `20260721005403_AddCausalInferenceFoundation`. The migration was not applied; no database was contacted or modified.
- Deterministic synthetic workflow: 480 generated rows across 60 launches and 8 states; 465 eligible after 15 censored rows, 195 treated, and 270 controls. The generator has a declared synthetic log-odds treatment coefficient of 1.0. Synthetic development risk-difference estimates were: descriptive 0.2362, propensity weighted 0.1875, outcome regression 0.1658, and AIPW 0.1881 with grouped-bootstrap interval [0.1254, 0.2822]. These are **synthetic development data — not research results**.
- Generated twelve JSON/Markdown causal reports and a review-only idempotent SQL script beneath ignored `artifacts/reports/milestone-6/`. No package was added.
- Verification: local tool restore, solution restore, and build succeeded; build produced 0 warnings and 0 errors. All 191 tests passed with 0 failures and 0 skips. EF reported no pending model changes and listed six migrations with `--no-connect`.
- No real or previous-project data/database was accessed; no Python, DoWhy/EconML, Gemini/RAG, autonomous agent, unrestricted API endpoint, database update, or commit was added.

Unresolved identification risks: strict no-interference is implausible; unmeasured manufacturer, wholesaler, payer, supply, and true launch timing can confound estimates; approval and utilization are imperfect access measures; suppression/missingness may be informative; DAG and nuisance-model specifications may be wrong; and cross-fitting is not implemented. Cross-fitting and independent Python validation are required before final research analysis.

## Historical Milestone 7 boundary (superseded)

The planned boundary was an isolated, pinned Python research-validation environment; schema-versioned .NET export/import; independent cohort reconstruction; DoWhy identification/refutation; EconML DML/heterogeneous-effect sensitivity; and strict JSON result validation. It must not change the production C# estimates silently, use a real database without authorization, or add Gemini/RAG.

## Superseded Milestone 4 handoff

Milestone 5 must not begin until Milestone 4 is accepted. Proposed scope: next predictive task(s), richer categorical handling and calibration/subgroup diagnostics, explicit validation-only threshold policies, and controlled experiment comparison—still without causal inference, Gemini/RAG, or Python unless a later authorized milestone requires them.
