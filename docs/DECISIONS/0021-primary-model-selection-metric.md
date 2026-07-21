# ADR 0021: PR AUC model selection

Status: Accepted (Milestone 4)

Select on validation PR AUC, then lower log loss, lower Brier score, and simpler model. Accuracy is not primary under imbalance. Test metrics never select models, and approval remains manual.
