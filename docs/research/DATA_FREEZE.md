# Research data freeze

Stages are `ProtocolDraft`, `SourceFilesRegistered`, `SourceFilesValidated`, `DatasetBuilt`, `DatasetValidated`, `CohortDefined`, `FeatureSetValidated`, `PredictiveSplitFrozen`, `CausalProtocolFrozen`, `ValidationBundleGenerated`, `ReadyForAnalysis`, `Rejected`, and `Archived`. Transitions are sequential. `ReadyForAnalysis` requires an approved protocol, zero blocking findings, actor/timestamp/reason, and becomes immutable. Synthetic freezes can never be marked real.

Freeze identity hashes sources, dataset, cohort, feature schema, predictive split, causal protocol, Python/.NET environments and commit. Changed lineage creates a new version.
