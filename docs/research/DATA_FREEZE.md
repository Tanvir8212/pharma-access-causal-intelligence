# Research data freeze

Stages are `ProtocolDraft`, `SourceFilesRegistered`, `SourceFilesValidated`, `DatasetBuilt`, `DatasetValidated`, `CohortDefined`, `FeatureSetValidated`, `PredictiveSplitFrozen`, `CausalProtocolFrozen`, `ValidationBundleGenerated`, `ReadyForAnalysis`, `Rejected`, and `Archived`. Transitions are sequential. `ReadyForAnalysis` requires an approved protocol, zero blocking findings, actor/timestamp/reason, and becomes immutable. Synthetic freezes can never be marked real.

Freeze identity hashes sources, dataset, cohort, feature schema, predictive split, causal protocol, Python/.NET environments and commit. Changed lineage creates a new version.

A real candidate additionally requires successful import reconciliation, resolved/excluded blocking mappings, an owned dedicated database, applied expected migrations, explicit write enablement, real/synthetic distinction, clean Git state or approved exception, and human approval. It is never auto-approved.
