# Probability calibration

`NextQuarterStateEntry` supports uncalibrated probabilities and deterministic Platt scaling. The calibrator is fitted only on the validation partition, rejected for small or single-class samples, frozen before test evaluation, and never replaces the retained raw probability. Stored parameters are slope and intercept.

Evaluation includes Brier score, log loss, ECE, MCE, calibration slope/intercept, and observed-versus-predicted equal-width or equal-frequency bins. Bins below the support threshold emit warnings. Synthetic calibration is development evidence only.
