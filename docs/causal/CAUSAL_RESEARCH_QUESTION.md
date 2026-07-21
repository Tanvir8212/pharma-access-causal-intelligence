# Causal research question

Study `PeerStateExposureToNextQuarterEntry` asks whether exposure to earlier generic adoption in neighboring, regional, or historically similar state Medicaid markets affects a target state's probability of first entry in the next observable quarter.

The unit is eligible Drug × State × ObservationQuarter. Time zero is the start of the observation quarter. Baseline confounders are measured no later than treatment assignment; exposure uses information available before the outcome quarter; the binary outcome is observed next-quarter first entry. Already-entered, censored, incomplete, invalid, leakage-blocked, or missing treatment/outcome rows are excluded.

This is an observational causal-estimation framework under assumptions, not proof of causation. Prediction, association, causal estimation, and model-based counterfactual scenarios remain separate outputs.
