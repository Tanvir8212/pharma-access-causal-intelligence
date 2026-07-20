# Milestone 1 data dictionary

## Domain value objects

- `CalendarQuarter`: year, quarter 1–4, deterministic date boundaries, arithmetic, distance, comparison, and `YYYY-QN` parsing.
- `StateCode`: trimmed, uppercase two-letter code; scientific eligibility is not embedded.
- `Percentage`: finite value in `[0,100]` without storage rounding.
- `AccessGapValue`: finite value in `[-100,100]` because WD minus ND may be negative.
- `DatasetVersionCode`: trimmed identifier up to 64 characters using letters, digits, `.`, `_`, and `-`.

## Canonical entities

- `Drug`: normalized ingredient and optional descriptive identifiers with UTC creation/update timestamps.
- `DrugProduct`: preserved original NDC, optional normalized NDC, source identity, and optional mapping confidence. NDC normalization is deferred.
- `FirstGenericApproval`: FDA or other approval reference. Approval date is a launch proxy, not proof of commercial launch.
- `State`: code/name, optional region/division, eligibility flag, and optional exclusion reason.
- `QuarterDimension`: persistence key plus values derived from `CalendarQuarter`.
- `DatasetVersion`: snapshot code, schema/feature/code lineage, validation lifecycle, counts, timestamps, and notes.
- `SourceFile`: source category, original filename, retrieval/import metadata, SHA-256, size/counts, schema/license/status/error metadata. No file ingestion exists yet.
- `StateDrugUtilization`: drug/state/quarter/dataset lineage, optional product, nonnegative prescriptions, decimal reimbursement, suppression, and quality status. No labels or predictions are stored.
- `JobRun`: explicit pending/running/terminal lifecycle, correlation, optional dataset, UTC timestamps, and optional diagnostic metadata. No worker orchestration exists yet.

## Foundational calculations

ND is active eligible states divided by eligible states times 100. WD uses nonnegative frozen baseline weights. Both reject an undefined zero denominator rather than returning zero or NaN. Access Gap is WD minus ND and may be negative. These are pure calculations only; no research result is generated.
