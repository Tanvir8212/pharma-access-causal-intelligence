# ADR 0002: SQL Server

Status: Accepted; foundation implemented in Milestone 1

SQL Server is the primary transactional and analytical metadata database. Milestone 1 adds EF Core configuration and a source-only migration for `core` and `audit`; it does not connect or apply the migration. Dapper remains deferred until a demonstrated analytical-query need.
