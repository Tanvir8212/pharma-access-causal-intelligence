# System architecture

The solution uses Clean Architecture. Domain is dependency-free; Application depends on Domain; Infrastructure, Data, ML, Causal, and Llm sit outside Application; Worker, API, and Web compose outer capabilities. Predictive ML and causal inference remain separate sibling boundaries. API exposes a Milestone 0 `/health` proof and Web serves a minimal foundation page.

All production and test projects target `net10.0` using the installed stable .NET SDK 10.0.302. Nullable analysis, deterministic builds, and warnings-as-errors are configured centrally while Domain remains independent of ASP.NET Core, EF Core, ML.NET, Gemini, and SQL Server.
