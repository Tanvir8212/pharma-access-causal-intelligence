# System architecture

The solution uses Clean Architecture. Domain is dependency-free and now owns pure invariants and foundational metrics; Application depends on Domain and exposes a narrow dataset-version persistence port. Data implements that port with EF Core 10 and SQL Server. Infrastructure, ML, Causal, and Llm remain outside Application; Worker, API, and Web compose outer capabilities. Predictive ML and causal inference remain separate sibling boundaries. API preserves its database-independent `/health` endpoint.

All production and test projects target `net10.0` using the installed stable .NET SDK 10.0.302. Nullable analysis, deterministic builds, and warnings-as-errors are configured centrally while Domain remains independent of ASP.NET Core, EF Core, ML.NET, Gemini, and SQL Server.
