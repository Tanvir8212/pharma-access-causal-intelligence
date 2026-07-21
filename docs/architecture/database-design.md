# Milestone 1 database design

`PharmaAccessDbContext` in `PharmaAccess.Data` persists canonical metadata and provenance with EF Core 10 and the SQL Server provider. Domain remains provider-free. Entity mappings are separate `IEntityTypeConfiguration<T>` classes.

## Schema ownership

- `core`: Drug, DrugProduct, FirstGenericApproval, State, CalendarQuarter, DatasetVersion, SourceFile, and StateDrugUtilization.
- `audit`: JobRun.
- `raw`, `stg`, `feature`, `ml`, `causal`, `rag`, and `research` are not created by Milestone 1.
- `feature` (Milestone 3): FeatureSetVersion, FeatureDefinition, DrugStateQuarterFeature, LaunchQuarterSummary, StateHistoricalProfile, and RegionalHistoricalProfile. `core.GenericLaunch` owns the documented approval-proxy identity.
- `ml` (Milestone 4): MlExperiment, ModelTrainingRun, ModelMetric, ModelArtifact, and optional PredictionRecord development rows. Relationships use restricted deletion and binaries remain outside SQL Server.

Integer surrogate keys favor clear SQL Server and EF compatibility. Reimbursement uses `decimal(19,4)`; prescription counts and provenance row counts use `bigint` where scale warrants it. Enums persist as controlled strings. A model-wide converter restores `DateTimeKind.Utc` when UTC timestamps are materialized. Calendar dates use `date`.

## Uniqueness and lineage

- State code and dataset version code are unique.
- Calendar year/quarter and display code are unique.
- A source system plus original NDC identifies a source product in this foundation.
- A source SHA-256 may occur once within a dataset version.
- Utilization is unique per dataset version, drug, state, and quarter.

All provenance-sensitive foreign keys use `Restrict`; deleting a parent must never silently erase approvals, products, source lineage, utilization, or audit history.

## Runtime and migration policy

`AddPharmaAccessData` registers the context and SQL Server provider. A missing connection string is rejected only if the context is resolved. The API does not resolve the context during ordinary health startup, and it never calls `Database.Migrate`.

The design-time factory uses a non-secret LocalDB-shaped placeholder only to build migration metadata. Migration creation, listing with `--no-connect`, and script generation do not connect. Migration application is an explicit future operational decision; `dotnet ef database update` was not run.
