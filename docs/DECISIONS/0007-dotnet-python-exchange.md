# ADR 0007: Schema-based .NET/Python exchange

Status: Accepted for Milestone 7

.NET will export versioned CSV or Parquet research snapshots; Python will return strict schema-validated JSON. This favors reproducibility, isolation, and auditable lineage over live notebook execution.
