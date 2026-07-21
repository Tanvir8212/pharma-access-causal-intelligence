# PharmaAccess AI — Starting Point

The authoritative project specification is

- `docsPROJECT_BLUEPRINT.md`
- `docsCODEX_MASTER_PROMPT.md`

The project must be implemented incrementally.

The first Codex session must perform Milestone 0 only.

Do not begin database ingestion, ML training, causal estimation, Gemini integration, Python setup, or real-data modification until their respective milestones are explicitly requested.

Milestone 8 provides research-freeze governance and a synthetic-only pre-analysis workflow. A `ReadyForAnalysis` synthetic freeze is not authorization to execute a real research run; see `docs/research/REAL_DATA_EXECUTION_GUIDE.md`.

Milestone 9A provides controlled real-data preparation. Private files stay under ignored configured roots; a dedicated owned database and `PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE=YES` are mandatory before any write. Follow `docs/research/MILESTONE_9_RUNBOOK.md`. Milestone 9 does not train final models or estimate final causal effects.
