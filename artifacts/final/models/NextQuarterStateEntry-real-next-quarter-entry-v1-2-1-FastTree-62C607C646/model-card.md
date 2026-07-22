# NextQuarterStateEntry model card

**Synthetic development data — not research results**

- Intended use: development prediction of next-quarter first state entry.
- Prohibited use: clinical decisions, causal claims, or claims of complete national access.
- Algorithm: FastTree; dataset version: 2; feature set: 1; selection: next-entry-features-v1.
- Training/validation/test groups: 147/66/48.
- Validation PR AUC: 0.0766625; test PR AUC: 0.111169; threshold: 0.5.
- Artifact SHA-256: 62C607C6462D603D04CDF43662B16EAD773138C916912080B15A7F65C85F9022; approval: ValidationSelected (not Approved).
- FDA approval may not equal commercial launch. Medicaid utilization is not complete national access.
- Probabilities are not described as calibrated. Feature importance is not causal evidence. Subgroup evaluation is not yet complete.