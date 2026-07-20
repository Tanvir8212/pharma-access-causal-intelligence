# Local setup

Milestone 1 uses the installed stable .NET SDK 10.0.302 and targets `net10.0`. Run `dotnet tool restore` for repository-local `dotnet-ef` 10.0.10, then use the root README commands. SQL Server is not required for build, tests, migration metadata, or API `/health`; no migration runs automatically. Use user secrets or environment-specific configuration for future credentials. See `local-development.md` for no-connect EF commands.
