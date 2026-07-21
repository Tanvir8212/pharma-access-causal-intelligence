# Local development for the database foundation

Prerequisites are the installed stable .NET SDK 10.0.302 and normal NuGet access. Restore the repository-local EF Core 10.0.10 tool with `dotnet tool restore`.

Use user secrets or environment-specific configuration for a future real `ConnectionStrings:PharmaAccess` value. Never commit credentials. The checked-in API setting is intentionally empty; ordinary build, tests, and `/health` require no SQL Server.

The Data project contains the design-time factory, so use it as both project and startup project:

```powershell
dotnet ef migrations list --no-connect --project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj --startup-project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj
dotnet ef migrations script --idempotent --project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj --startup-project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj --output .\artifacts\reports\milestone-1\InitialCoreFoundation.idempotent.sql
```

Do not run `dotnet ef database update` against an existing database. Automatic startup migration is disabled. Database provisioning and migration application require an explicitly approved environment and operational procedure.
## Milestone 3 feature foundation

Run `dotnet tool restore`, restore/build/test the solution, then use `dotnet ef migrations has-pending-model-changes` with the Data project, API startup project, and `PharmaAccessDbContext`. Review scripts may be generated with `dotnet ef migrations script --idempotent`; never run `database update` implicitly. Feature builds default to dry-run at their caller boundary and require explicit configured persistence.

The feature dictionary review artifact is under `artifacts/reports/milestone-3/`. No SQL Server is required for metadata tests or the synthetic in-memory pipeline.

## Milestone 4 synthetic ML

ML.NET 5.0.0 packages restore through NuGet. Generated models/cards/manifests belong under ignored `artifacts/models/`; set `PHARMAACCESS_ML_ARTIFACT_ROOT` only for an explicit local synthetic workflow. LightGBM requires its packaged native runtime. Build/tests need no SQL Server or internet after restore. Never present synthetic metrics as research results or run `dotnet ef database update` implicitly.
# Milestone 5 evaluation

Synthetic reports are generated beneath `artifacts/reports/milestone-5/` and must state that they are not research results. Restore local tools before EF commands. Generate/list/script migrations only; never run `dotnet ef database update` as part of this milestone. Approval has no anonymous API endpoint.

# Milestone 6 causal development

Run only the deterministic synthetic causal workflow; never point it at a previous or real research database. Set `PHARMAACCESS_M6_REPORT_PATH=artifacts/reports/milestone-6` to retain generated reports. Restore/build/test normally, generate migration metadata and an idempotent review script, and do not run `dotnet ef database update`. Causal execution endpoints remain disabled because authentication is absent.
# Milestone 7 Python validation

Run `scripts/setup-python-validation.ps1`, then `scripts/run-cross-language-parity.ps1 -StudyCode synthetic-peer-exposure -ValidationRunCode m7-synthetic-001`. PowerShell execution policy must be configured by the local administrator. The workflow uses synthetic data only and never contacts SQL Server.

# Milestone 8 synthetic freeze

Run `scripts/run-synthetic-research-freeze.ps1 -FreezeCode m8-synthetic-freeze-v1` after building. Output is ignored under `artifacts/research-freezes/`. It uses no database, network, real data, training or causal estimation.

## Milestone 9A

Set `PHARMAACCESS_PRIVATE_DATA_ROOT` to the ignored private root. Use the Worker `discover-research-sources` command for metadata-only discovery and `prepare-m9` for a sanitized missing-input report under ignored `artifacts/research-audit/`. Configure a future research connection only through user secrets or environment variables. `PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE=YES` is separately required and is never set by the application. See `docs/research/MILESTONE_9_RUNBOOK.md`; do not run `database update`, real import, final training, or final causal estimation without explicit prerequisites and authorization.
