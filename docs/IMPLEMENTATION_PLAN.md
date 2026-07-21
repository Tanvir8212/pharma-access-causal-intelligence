# Implementation plan

The authoritative sequence remains `docs/PROJECT_BLUEPRINT.md` and `docs/CODEX_MASTER_PROMPT.md`.

Milestone 0 established the buildable boundaries. Milestone 1 establishes pure Domain primitives/entities/calculations, SQL Server EF Core metadata persistence, immutable dataset/source provenance foundations, explicit job state, and one reviewable initial migration. It does not ingest files or connect to a database.

Milestone 3 establishes deterministic feature-set metadata, generic-launch identity, leakage-safe state-quarter and summary generation, frozen historical weights, nullable labels, leakage auditing, and `feature` persistence. It does not train or select models. Milestone 4 remains the first predictive-modeling milestone.

Milestone 4 implements only the NextQuarterStateEntry predictive vertical slice: versioned feature selection, temporal grouped holdout, baselines, four explicit ML.NET trainers, validation-only selection, frozen test evaluation, hashed candidate artifacts, model cards/manifests, experiment metadata, and reference-based scoring. Milestone 5 is not started.
