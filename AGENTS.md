# Repository instructions

Read `START_HERE.md`, `docs/PROJECT_BLUEPRINT.md`, and `docs/CODEX_MASTER_PROMPT.md` completely before editing. The blueprint is the project specification; the master prompt is the implementation authority.

Work one explicitly requested milestone at a time. Preserve predictive/causal separation, temporal grouped validation, observational-causal language, immutable raw inputs, and full lineage. Never add secrets, real raw data, generated models, arbitrary SQL/paths, clinical advice, or unsupported causal claims.

Keep `PharmaAccess.Domain` free of ASP.NET Core, EF Core, ML.NET, Gemini, and SQL Server dependencies. Dependencies point inward through Application abstractions. Build and run relevant tests after changes; update `docs/IMPLEMENTATION_STATUS.md`. Do not commit unless explicitly asked.

The installed SDK selected by the Milestone 0 repair is 10.0.302, targeting `net10.0`. Do not install software or select preview SDKs.
