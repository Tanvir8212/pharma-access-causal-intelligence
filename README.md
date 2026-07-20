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
