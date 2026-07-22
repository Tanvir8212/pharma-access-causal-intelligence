# PharmaAccess Causal Intelligence

PharmaAccess AI is a research-oriented platform for studying state-level adoption of newly approved generic drugs in U.S. Medicaid markets. Its planned unit of analysis is **Drug × State × Quarter**, with strict separation between prediction and observational causal estimation.

Milestone 1 adds pure domain primitives/entities/calculations and an EF Core 10 SQL Server persistence foundation with a source-only migration. No migration has been applied, no database is required, and ingestion, ML training, causal estimators, Python, and Gemini remain deferred.

## Build

```powershell
dotnet tool restore
dotnet restore PharmaAccess.sln
dotnet build PharmaAccess.sln --no-restore
dotnet test PharmaAccess.sln --no-build
```

The main solution contains all production and test projects. Test identity and dependencies are declared only by projects under `tests`, so the main-solution test command executes only those projects.

See `START_HERE.md`, `docs/PROJECT_BLUEPRINT.md`, and `docs/CODEX_MASTER_PROMPT.md` before making changes.

## Completed real-data analysis

The governed `real-2021-2025-v1` dataset is finalized with 261 eligible ANDA-level launches and 174,471 state-quarter rows. The chronological grouped split contains 147 training, 66 validation, and 48 locked-test launches. Final locked-test ROC AUC is 0.8221 and PR AUC is 0.1112.

The protocol-defined .NET AIPW ATT risk difference is 0.00157 (95% ANDA-clustered bootstrap CI -0.00377 to 0.00928). The independent grouped cross-fitted Python robustness estimate is -0.01382. Their material difference reflects different nuisance-model fitting procedures and is retained as a disclosed methodological limitation. The confidence interval includes zero; the analysis does not establish that neighbor exposure causes adoption.

The immutable final artifact package is under `artifacts/final`, with hashes recorded by freeze `real-2021-2025-v1-final/1.0`.
# Milestone 7 validation

The optional isolated Python research-validation layer lives under `python/`. It validates deterministic synthetic C# exports through DoWhy, EconML, and transparent reference formulas. It is not the application runtime and its outputs are not research results.
