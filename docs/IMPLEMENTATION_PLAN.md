# Implementation plan

The authoritative sequence remains `docs/PROJECT_BLUEPRINT.md` and `docs/CODEX_MASTER_PROMPT.md`.

Milestone 0 established the buildable boundaries. Milestone 1 establishes pure Domain primitives/entities/calculations, SQL Server EF Core metadata persistence, immutable dataset/source provenance foundations, explicit job state, and one reviewable initial migration. It does not ingest files or connect to a database.

Milestone 3 establishes deterministic feature-set metadata, generic-launch identity, leakage-safe state-quarter and summary generation, frozen historical weights, nullable labels, leakage auditing, and `feature` persistence. It does not train or select models. Milestone 4 remains the first predictive-modeling milestone.

Milestone 4 implements only the NextQuarterStateEntry predictive vertical slice: versioned feature selection, temporal grouped holdout, baselines, four explicit ML.NET trainers, validation-only selection, frozen test evaluation, hashed candidate artifacts, model cards/manifests, experiment metadata, and reference-based scoring.

Milestone 5 extends that task only with validation-fitted Platt calibration, support-based uncertainty, validation-only permutation importance and threshold selection, safe subgroup/error analysis, manual approval/champion controls, reproducible reports and `ml` metadata persistence.

Milestone 6 added the `PeerStateExposureToNextQuarterEntry` observational causal foundation: versioned treatment/outcome/DAG/adjustment definitions, ATT/ATE risk differences, deterministic propensity/outcome nuisance models, weighting, balance/positivity diagnostics, AIPW, grouped bootstrap, placebo/sensitivity infrastructure, exploratory subgroup effects, support-aware scenarios, causal persistence, and synthetic-only reports.

Milestone 7 adds isolated Python 3.12 validation, a hashed filesystem contract, deterministic C# synthetic cohort/nuisance/fold export, strict formula parity, independently fitted DoWhy/EconML diagnostics, and reproducible synthetic-only reports. It adds no database schema and does not start Milestone 8.

Milestone 8 adds research-protocol approval, local source registration, deterministic profiling/cohort construction, predictive and causal pre-analysis freezes, environment/Git safety gates, immutable freeze approval, synthetic package export, and `research` metadata persistence. It does not run real data, final training, or final causal estimation.
