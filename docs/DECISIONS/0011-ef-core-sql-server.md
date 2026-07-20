# ADR 0011: EF Core 10 and SQL Server foundation

Status: Accepted (Milestone 1)

Use EF Core 10.0.10 for canonical entities and metadata with the SQL Server provider. Central package management keeps all EF packages on the same stable patch. EF types remain in Data; Domain remains pure. SQL configuration is registered through dependency injection, but the context is not resolved or migrated at startup.

The design-time factory exists to generate migrations without secrets. Automatic migration is rejected because schema changes affecting research lineage require explicit review and controlled execution.
