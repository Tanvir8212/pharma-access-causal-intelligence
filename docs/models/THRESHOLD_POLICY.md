# Threshold policy

The transparent default is 0.5. Validation-only alternatives are maximum F1, minimum recall, minimum precision, Youden index and cost-sensitive selection. Objectives and constraints are stored; impossible constraints fail explicitly. The selected threshold is frozen before test evaluation, and test metrics never select policy.
