# Local development for the database foundation

Prerequisites are the installed stable .NET SDK 10.0.302 and normal NuGet access. Restore the repository-local EF Core 10.0.10 tool with `dotnet tool restore`.

Use user secrets or environment-specific configuration for a future real `ConnectionStrings:PharmaAccess` value. Never commit credentials. The checked-in API setting is intentionally empty; ordinary build, tests, and `/health` require no SQL Server.

The Data project contains the design-time factory, so use it as both project and startup project:

```powershell
dotnet ef migrations list --no-connect --project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj --startup-project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj
dotnet ef migrations script --idempotent --project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj --startup-project .\src\PharmaAccess.Data\PharmaAccess.Data.csproj --output .\artifacts\reports\milestone-1\InitialCoreFoundation.idempotent.sql
```

Do not run `dotnet ef database update` against an existing database. Automatic startup migration is disabled. Database provisioning and migration application require an explicitly approved environment and operational procedure.
