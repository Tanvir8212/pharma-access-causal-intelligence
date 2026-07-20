# Local setup

Milestone 0 uses the already-installed stable .NET SDK 10.0.302 and targets `net10.0`. Do not install software; NuGet dependencies are obtained only through `dotnet restore`. Build and test the main solution with the commands in the root README. Python is not configured, Docker is absent, and SQL Server is not configured or required. API health is available at `/health` when `PharmaAccess.Api` is run.
