# Real protocol review — approval-to-access-real / 1.0

Lifecycle target: `Draft`, followed by a separate human-confirmed transition to `UnderReview`. Creation, submission and approval are separate writes. None has been executed.

The complete machine-readable proposal is `config/research-protocols/approval-to-access-real-1.0.json`. It reconciles the frozen Milestone 6–8 policies: drug-state-quarter analysis; FDA approval as a launch-reference proxy; `any-positive-v1` entry; Numeric Distribution, historical-baseline Weighted Distribution and Access Gap; next-quarter entry prediction; chronological whole-`GenericLaunchId` grouping with a locked test partition; lagged `HighNeighborAdoptionExposure`; next-quarter entry outcome; ATT risk difference; AIPW primary analysis; `peer-exposure-dag-v1`; `peer-exposure-adjustment-v1`; empirical overlap; explicit missingness/censoring; suppressed values as unknown; unresolved ambiguous NDCs; invalid-NDC exclusion; the FDA continuation-fragment exclusion; and provenance-only archival FDA representations.

## Human decisions still required

1. Select the FDA approval-cohort start and end dates.
2. Select exact train, validation and locked-test temporal boundaries after reviewing complete-follow-up cohort counts.
3. Confirm the minimum eligible-neighbor count and lag encoded by `neighbor-v1` against the frozen causal artifact.
4. Decide whether the four ambiguous and two invalid NDC observations remain excluded from normalized analysis or receive human-reviewed mappings.

Until those fields are resolved, the creation command must refuse a confirmed write. Approval is not technically or scientifically available before creation and submission.
