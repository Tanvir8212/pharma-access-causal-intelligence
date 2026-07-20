# Feature engineering foundation

Milestone 3 builds deterministic, split-ready analytical rows in C#. The grain is `GenericLaunchId × StateId × ObservationQuarter`; EF additionally scopes uniqueness by feature-set version. FDA first-generic approval is only a documented launch reference proxy, not evidence of commercial launch.

The staged build validates dataset/feature-set state, eligibility and frozen-weight cutoffs; builds launch summaries and state-quarter rows; attaches exact-quarter lags; creates nullable labels; audits leakage; and persists only when dry-run is false and no blocking finding exists. Missing quarters remain missing. Current target-launch history may be used only through the row's observation cutoff; future observations never enter predictors.

Ordinary growth is `(current - lag) / lag`; it is null for missing values or a zero lag. Concentration shares use nonnegative prescription volume. When total utilization is zero, HHI, top-state share, and top-five share are null rather than zero or NaN.

The current synthetic in-memory pipeline generates 32 state-quarter rows and 8 launch-quarter summaries from two launches, four states, and five possible quarters. It is not a research dataset and produces no scientific conclusion.
