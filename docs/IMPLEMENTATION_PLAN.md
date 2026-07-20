# Implementation plan

The authoritative milestone sequence is in `docs/PROJECT_BLUEPRINT.md` and `docs/CODEX_MASTER_PROMPT.md`. Work must proceed from Milestone 0 through Milestone 11 only when each milestone is explicitly requested. Milestone 0 establishes repository governance and buildable boundaries; Milestone 1 is the next permitted slice and introduces the initial domain model, database context, state/quarter dimensions, dataset version, sample configuration, migrations, and repository smoke test.

Every milestone must inspect current state, state its file scope, implement a coherent slice, restore/build/test, fix failures, update status, and disclose warnings. Database ingestion, feature engineering, ML, causal estimation, Python, counterfactuals, Gemini/RAG, continual learning, and final research exports remain deferred to their named milestones.
