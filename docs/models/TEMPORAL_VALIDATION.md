# Temporal grouped validation

Primary validation is `TemporalGroupedHoldout` grouped by `GenericLaunchId`. Earlier cohorts train, later cohorts validate, and newest fully observable cohorts test. Boundaries are configuration, not scientific constants. Chronology and nonoverlap are validated, and every exact row/group membership is hashed. Random row splitting is prohibited; no grouped-random diagnostic is implemented in this milestone.
