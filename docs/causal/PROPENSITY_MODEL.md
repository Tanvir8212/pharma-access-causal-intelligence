# Propensity model

A deterministic C# logistic model predicts treatment using only the adjustment set; outcome fields are rejected. Configuration records iterations, learning rate, seed, and explicit clipping. It is distinct from the Milestone 4 predictive outcome model. Extreme probabilities, clipping, quasi-separation, and overlap failures are reported. Cross-fitting is not implemented and is required before final research analysis.
