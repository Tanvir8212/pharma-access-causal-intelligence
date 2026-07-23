# PharmaAccess Causal Intelligence

PharmaAccess Causal Intelligence is a reproducible research and
software-engineering framework for studying state-level generic-drug
entry in U.S. Medicaid markets.

The project links FDA generic approval records with Medicaid State Drug
Utilization Data to construct a longitudinal drug–state–quarter panel.
It combines:

- temporal grouped machine-learning evaluation;
- observational causal estimation;
- SQL Server data engineering;
- ML.NET predictive modeling;
- Python-based independent causal validation;
- reproducible dataset, experiment, and artifact lineage.

## Published paper

Tanvir Mahmud Khan. “From Approval to State Entry: Temporal Machine
Learning and Observational Causal Analysis of Generic Drug Adoption in
U.S. Medicaid Markets.” Zenodo Preprint, 2026.

DOI: https://doi.org/10.5281/zenodo.21502258

## Main results

- 261 eligible ANDA-level generic launches
- 174,471 drug–state–quarter observations
- 147 training, 66 validation, and 48 locked-test launches
- Locked-test ROC AUC: 0.8221
- Locked-test PR AUC: 0.1112
- Validation-selected classification threshold: 0.08
- Governed .NET AIPW ATT estimate: 0.00157
- 95% bootstrap CI: −0.00377 to 0.00928
- Independent Python grouped cross-fitted estimate: −0.01382

The predictive model showed useful out-of-time discrimination. The
causal estimates were not robust across specifications and do not
establish that peer-state exposure causes generic entry.