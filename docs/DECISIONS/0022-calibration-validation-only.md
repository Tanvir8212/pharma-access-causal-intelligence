# ADR 0022: Fit calibration on validation only

Status: Accepted

Platt calibration is fitted on validation data, frozen, then evaluated once on test data. Raw probabilities remain available. This prevents test-label feedback and supports reproducibility; isotonic calibration is deferred until sample support justifies it.
