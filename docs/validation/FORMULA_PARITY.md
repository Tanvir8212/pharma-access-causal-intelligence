# Formula parity

Formula parity does not refit nuisance models. It recomputes unadjusted and weighted risk differences, ATT/ATE weights, effective sample size, standardized mean differences, and AIPW contributions from exported row values. Defaults are absolute `1e-9` and relative `1e-8`; reports retain maximum differences and first mismatch.

C# ATT AIPW sums treated `(Y-m0)/n1` and control `-p/(1-p)*(Y-m0)/n1`; ATE averages `m1-m0 + T(Y-m1)/p - (1-T)(Y-m0)/(1-p)`.
