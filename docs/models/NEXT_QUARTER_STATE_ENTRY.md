# Next-quarter state entry

Task: binary prediction that a not-yet-entered eligible drug-state pair first satisfies the configured entry policy in the next observable quarter. Censored labels, prior entries, invalid feature sets, blocking quality/leakage rows, and ineligible states are excluded—not labeled negative.

The v1 schema uses 32 ordered timing, current/lagged utilization, launch-history, state-history, regional, geographic, and missingness features. IDs, version keys, as-of quarter shortcuts, all labels, and all future Q4 outcomes are prohibited. Categorical state/region fields are initially excluded to avoid unstable cardinality and because the synthetic contract currently provides historical numeric representations.
# Milestone 5 evaluation extension

Responses preserve raw probability and optionally include a validated calibrated probability, calibration status, frozen threshold, support-based uncertainty and reasons, conservative important-feature summary, approval status, complete dataset/feature/model lineage, and warnings. A calibrated probability is not called a confidence score.
