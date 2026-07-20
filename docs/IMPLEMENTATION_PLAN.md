# Implementation plan

The authoritative sequence remains `docs/PROJECT_BLUEPRINT.md` and `docs/CODEX_MASTER_PROMPT.md`.

Milestone 0 established the buildable boundaries. Milestone 1 establishes pure Domain primitives/entities/calculations, SQL Server EF Core metadata persistence, immutable dataset/source provenance foundations, explicit job state, and one reviewable initial migration. It does not ingest files or connect to a database.

Milestone 2 is the next permitted slice only when explicitly requested: local FDA and Medicaid CSV ingestion contracts, safe sample inputs, NDC normalization, mapping, data-quality results, immutable dataset registration, and tests. Feature generation, predictive ML, causal estimation, Python, counterfactuals, Gemini/RAG, and research results remain deferred to their later milestones.
