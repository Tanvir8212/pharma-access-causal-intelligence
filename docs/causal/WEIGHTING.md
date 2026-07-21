# Propensity weighting

ATT weights are 1 for treated rows and `p/(1-p)` for controls. ATE weights are `1/p` for treated rows and `1/(1-p)` for controls. Scores must be in `(0,1)`. Reports include effective sample size, maximum and 95th-percentile weights, extreme counts, clipping configuration, and positivity warnings.
