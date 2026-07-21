# Binary metrics

Primary selection metric is PR AUC because entry can be imbalanced and accuracy can obscure minority-class performance. Secondary metrics are log loss, Brier score, recall, and ROC AUC. Reports also include accuracy, precision, F1, specificity, confusion counts, prevalence, predicted-positive rate, log-loss reduction, and threshold.

PR AUC is average stepwise precision over recall increments after descending probability rank. Brier score is mean squared probability error. ROC AUC uses rank statistics. The transparent v1 threshold is 0.5 and is frozen before test evaluation. These implementations have hand-calculated unit tests.
