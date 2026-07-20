# Leakage prevention

Every predictor is constrained to `AvailableAsOfQuarterId <= ObservationQuarterId`. Lags require exact prior quarters; missing quarters are not forward-filled. Historical profiles use periods strictly before their cutoff. Market weights end before the observation window. Future labels remain separate from model inputs.

The audit emits controlled findings with severity, feature, row identifier, description, and recommendation. It checks future availability, duplicate analytical keys, definitions marked both input and label, prohibited model inputs, and provides the boundary for later split-aware checks. Blocking findings prevent persistence and feature-set finalization.
