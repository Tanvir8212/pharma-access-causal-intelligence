# Feature dictionary

The authoritative machine-readable export is `artifacts/reports/milestone-3/feature-dictionary.json`. Database definitions are versioned by `FeatureSetVersion.DefinitionHash`; every analytical row stores the definition hash used.

Core identifiers and timing fields are non-model metadata. Lagged utilization, historical, regional, geographic, and quality fields are model-input candidates only when their `AvailableAsOfRule` is satisfied. All `Label*` fields are labels and never inputs. Q4 outcome fields and next-quarter entry are future labels and remain null when follow-up is incomplete.

Milestone 4 feature-selection version `next-entry-features-v1` defines 32 deterministic numeric/missingness inputs. It excludes all labels, future outcomes, IDs/version keys, `AvailableAsOfQuarterId`, state/region categories, therapeutic class, and launch-cohort category. The exclusions avoid leakage or unstable encoding in the first synthetic baseline; later additions require metadata and a new schema hash.
