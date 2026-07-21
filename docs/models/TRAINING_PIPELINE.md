# Training pipeline

`TemporalTrainingDatasetBuilder` filters eligibility and censoring, validates class counts, assigns whole generic-launch groups chronologically, hashes the dataset and manifest, and constructs the versioned schema. ML.NET missing-value replacement and any normalization are fitted on training only. Logistic regression uses normalization; trees do not. Undefined numeric fields remain `NaN` until the training-fitted replacement transform; missingness indicators remain inputs. Suppressed values are never changed to observed zero.

Candidates are SDCA logistic regression, FastTree, FastForest, and LightGBM with small explicit configurations and seed 17 in synthetic verification. No AutoML or broad tuning occurs. Validation PR AUC ranks models; ties use log loss, Brier score, then simpler algorithm. Only the frozen winner is evaluated on test. It becomes `ValidationSelected`, never automatically `Approved`.
