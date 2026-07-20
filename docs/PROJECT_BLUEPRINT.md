# PharmaAccess Causal Intelligence

## Complete Project and Research Blueprint

### Working application name

**PharmaAccess AI**

### Repository name

`pharma-access-causal-intelligence`

### Proposed paper title

**From Approval to Access: A Causal Machine-Learning and Counterfactual Simulation Framework for Geographic Inequality in U.S. Generic Drug Adoption**

### Alternative technical title

**Temporal Causal Machine Learning for Estimating and Simulating State-Level Generic Drug Access in U.S. Medicaid Markets**

---

# 1. Project vision

PharmaAccess AI will be a research-oriented artificial intelligence platform that studies how newly approved generic drugs become available across U.S. state Medicaid markets.

The system will:

1. Construct longitudinal drug–state–quarter datasets from FDA and Medicaid data.
2. Predict future state entry, utilization and access inequality.
3. estimate causal effects under explicitly defined assumptions.
4. Generate counterfactual simulations of hypothetical early-access interventions.
5. Quantify predictive and causal uncertainty.
6. Explain model outputs through an evidence-grounded Gemini assistant.
7. Detect data and model drift.
8. Retrain candidate models through a governed champion–challenger process.
9. Maintain complete experiment, model and dataset lineage.
10. Produce reproducible research artifacts for a publishable paper.

This must be a separate project from the previous `generic-launch-diffusion-mlnet` repository.

The earlier project may be cited as preliminary exploratory work, but the new project must not reuse the same central target, experimental setup or research claim.

---

# 2. Difference from the earlier project

## Earlier project

The earlier project:

* Used aggregated launch-quarter observations.
* Calculated Numeric Distribution, Weighted Distribution and Access Gap.
* Classified adoption as Fast, Medium or Slow.
* Primarily used multiclass prediction.
* Had a relatively small launch-level training dataset.
* Focused on predicting an adoption category.

## New project

The new project will:

* Use **drug–state–quarter** as the primary unit of analysis.
* Model longitudinal state-specific trajectories.
* Predict next-quarter state entry and future access outcomes.
* Estimate average and heterogeneous treatment effects.
* Simulate counterfactual access scenarios.
* Use temporal and grouped validation.
* Quantify uncertainty.
* Include causal diagrams and confounding assumptions.
* Include model drift and governed continual learning.
* Evaluate the LLM independently.
* Produce a substantially different paper and application.

## Scientific progression

The new research should openly describe the old work as a preliminary study that motivated a deeper state-level causal and temporal analysis.

It should not pretend there is no relationship between the two projects.

The distinction is:

> The earlier study classified launch-level diffusion patterns. The new study models state-level longitudinal adoption, estimates intervention effects under stated causal assumptions and simulates alternative access trajectories.

---

# 3. Primary research problem

Newly approved generic drugs do not necessarily appear across all state Medicaid markets at the same time or at the same intensity.

The project investigates:

> Can early launch characteristics and state-level historical patterns predict later geographic access, and under explicit causal assumptions, what improvement might be associated with broader or less concentrated early access?

This contains two related but separate problems.

## Predictive problem

What is likely to happen?

Examples:

* Will a generic enter a particular state next quarter?
* How long will entry take?
* What will its Q4 state coverage be?
* What will its Q4 access gap be?
* Will inequality remain high after four quarters?

## Causal problem

What might happen under a hypothetical intervention?

Examples:

* What would the expected Q4 access outcome be if Q1 state coverage were broader?
* What would happen if initial concentration in a few high-volume states were lower?
* Which launch types appear most responsive to broader early entry?
* Which states or state groups appear to experience the largest expected benefit?

The predictive model and causal model must remain logically separated.

A model that predicts well does not automatically identify a causal effect.

---

# 4. Research questions

## Primary research question

Under explicit observational causal assumptions, what is the estimated effect of broader early state-level generic availability on later geographic access in U.S. Medicaid markets?

## Secondary research questions

1. How accurately can state entry in the next quarter be predicted?
2. How accurately can Q4 Numeric Distribution be predicted?
3. How accurately can Q4 Weighted Distribution be predicted?
4. How accurately can Q4 Access Gap be predicted?
5. Which early launch features are most predictive of persistent access inequality?
6. Does adding historical state similarity improve prediction?
7. How heterogeneous are estimated treatment effects across drugs, states, regions and launch cohorts?
8. Do doubly robust estimators produce more stable estimates than unadjusted comparisons?
9. How sensitive are conclusions to treatment definitions and unmeasured confounding?
10. Can a governed continual-learning process improve out-of-time performance without worsening calibration or subgroup performance?
11. Can an evidence-constrained LLM explain results accurately without inventing values or causal conclusions?

---

# 5. Research hypotheses

The final paper should preregister or clearly state hypotheses before final model testing.

## H1 — Early breadth

Launches with broader Q1 geographic coverage will be associated with higher Q4 Numeric Distribution after adjustment for measured confounders.

## H2 — Early concentration

Launches with highly concentrated Q1 utilization will be associated with larger later Access Gaps.

## H3 — State readiness

States with historically faster generic adoption will have shorter time-to-entry for newly approved generics.

## H4 — Heterogeneous effects

The estimated effect of early breadth will vary by initial market size, therapeutic category, historical state adoption profile and launch cohort.

## H5 — Doubly robust estimation

Doubly robust estimators will produce more stable treatment-effect estimates than naive outcome comparisons.

## H6 — Temporal validation

Performance measured through random row splitting will appear materially better than performance measured on future launch cohorts because random splitting creates optimistic estimates.

## H7 — Graph-derived state features

State-neighborhood or state-similarity features will improve next-quarter state-entry prediction over purely launch-level features.

## H8 — Governed retraining

A champion–challenger retraining process will improve out-of-time predictive performance while maintaining calibration and subgroup safeguards.

## H9 — Grounded explanation

An LLM receiving structured verified model outputs and retrieved evidence will produce fewer unsupported claims than an LLM receiving raw data and an unconstrained prompt.

---

# 6. Scope

## Included

* FDA first-generic approval data.
* CMS Medicaid State Drug Utilization Data.
* Existing cleaned and mapped data from the prior project where legally and scientifically appropriate.
* State-level longitudinal utilization.
* Drug-level launch cohorts.
* State-level entry and utilization outcomes.
* Prediction.
* Causal effect estimation.
* Counterfactual simulation.
* Explainability.
* Drift monitoring.
* Model versioning.
* Gemini-based evidence-grounded explanation.
* Python-based causal validation.
* Research report generation.
* Reproducible experiments.

## Not included in the initial version

* Clinical efficacy recommendations.
* Individual patient predictions.
* Prescribing advice.
* Drug safety recommendations.
* Manufacturer commercial strategy recommendations.
* Real-world supply-chain data not present in the public datasets.
* Claims of definitive causation.
* Fully autonomous production model promotion without safeguards.
* A full deep graph neural network.
* A conversational system that can execute arbitrary SQL.
* Medical decision support.

---

# 7. Scientific claim boundaries

The application and paper must use language such as:

* “estimated effect”
* “effect under stated assumptions”
* “observational causal estimate”
* “counterfactual simulation”
* “association after measured-confounder adjustment”
* “not proof of causation”
* “sensitive to unmeasured confounding”

The application must not state:

* “This intervention will cause…”
* “The model proves…”
* “Drug X was unavailable because…”
* “State Y deliberately delayed…”
* “The system recommends a clinical action…”

Every counterfactual page must show a visible methodological disclaimer.

---

# 8. Core conceptual definitions

## Launch

A launch is associated with a generic drug or normalized molecule and an FDA first-generic approval date used as a launch reference or proxy.

FDA approval date and true market-entry date are not necessarily identical. This limitation must be documented.

## State entry

A generic is considered to have entered a state when a valid utilization observation meeting the configured entry threshold is detected.

The system must support configurable definitions:

1. Any positive prescription count.
2. Prescription count above a minimum threshold.
3. Positive reimbursement and prescription count.
4. Persistence-based entry requiring activity in more than one observation.

The primary analysis must define one entry rule before final evaluation.

Sensitivity analysis must compare alternatives.

## Numeric Distribution

For a given drug and period:

```text
ND = Number of states meeting the availability threshold
     ---------------------------------------------------
     Number of eligible states
     × 100
```

The denominator and treatment of missing jurisdictions must be explicit.

## Weighted Distribution

Weighted Distribution should represent coverage weighted by a state-level market-size measure.

A general definition:

```text
WD = Sum of reference market weights for states where the generic is present
     -------------------------------------------------------------------------
     Sum of reference market weights across eligible states
     × 100
```

The weighting variable must be frozen before observing the outcome period.

Potential weighting references:

* Previous-period brand utilization.
* Historical class-level utilization.
* Historical total Medicaid prescription volume.
* A prelaunch baseline.

The weighting variable must not use future information.

## Access Gap

```text
Access Gap = WD − ND
```

A large positive gap indicates that availability is concentrated in higher-weight states while many lower-weight states remain uncovered.

Because this metric can have nuanced interpretations, the application must explain it rather than label every positive value as inherently harmful.

## Quarter since launch

```text
QuarterSinceLaunch = 0, 1, 2, 3, ...
```

This must be calculated consistently using a documented calendar-quarter rule.

---

# 9. Primary unit of analysis

The principal table will use:

```text
Drug × State × Quarter
```

Each row represents one normalized drug or molecule in one state during one calendar quarter.

This design creates longitudinal observations while preserving state-level behavior.

The system must not randomly split these rows without grouping, because observations from the same drug can leak across training and testing.

---

# 10. Data sources

## FDA first-generic data

Used for:

* Approval reference date.
* Product identity.
* Active ingredient or molecule.
* Applicant or manufacturer, where available.
* Dosage form and strength, where consistently available.
* Approval cohort.

## CMS Medicaid State Drug Utilization Data

Used for:

* State-level prescription counts.
* Reimbursement.
* Quarter and year.
* Product or NDC identifiers.
* Suppression or missingness indicators where available.

## Optional enrichment

Only introduce optional enrichment after the base pipeline is reproducible.

Possible enrichment:

* RxNorm normalization.
* Therapeutic class.
* ATC mapping.
* Census region.
* State adjacency.
* Historical state market size.
* State-level Medicaid program descriptors from reliable public sources.

Every enrichment source must have:

* Source URL.
* Retrieval date.
* Version or release date.
* License or reuse note.
* Transformation script.
* Data dictionary.
* Validation checks.

---

# 11. Data governance and provenance

Every raw file must be immutable after ingestion.

Recommended zones:

```text
Raw
Staging
Curated
Feature
ResearchSnapshot
```

## Raw

Exact downloaded files with hashes.

## Staging

Parsed source-specific tables.

## Curated

Cleaned, normalized and deduplicated entities.

## Feature

Model-ready features and labels.

## ResearchSnapshot

Frozen dataset used for a specific paper experiment.

Each dataset version must record:

* DatasetVersionId.
* Source name.
* Source retrieval date.
* File hash.
* Row count.
* Column count.
* Schema version.
* Transformation commit hash.
* Validation status.
* Created timestamp.
* Notes.

---

# 12. Data-quality requirements

The ingestion pipeline must detect:

* Duplicate source rows.
* Duplicate drug-state-quarter rows.
* Invalid state codes.
* Invalid years and quarters.
* Negative prescription counts.
* Negative reimbursement values where not meaningful.
* Missing NDC.
* Malformed NDC.
* Inconsistent drug names.
* Multiple FDA mappings for the same product.
* Approval dates after observed utilization.
* Utilization far before the reference launch.
* Unexplained state-count changes.
* Extreme outliers.
* Suppressed observations.
* Schema drift.
* Unexpected null rates.
* Unexpected row-count changes.

Data-quality rules must have severity levels:

```text
Info
Warning
Error
Blocking
```

A blocking validation failure must prevent model training.

---

# 13. Entity normalization

## Drug identity

The system needs a durable normalized identifier.

Recommended hierarchy:

1. Normalized active ingredient or ingredient combination.
2. Dosage form.
3. Route.
4. Strength where scientifically appropriate.
5. NDC product mapping.
6. FDA application mapping.

The project must distinguish:

* Molecule-level analysis.
* Product-level analysis.
* Strength-level analysis.

The primary paper should choose one level and justify it.

Avoid merging clinically different combinations merely because names are similar.

## NDC normalization

NDCs may have formatting differences.

The mapping pipeline must:

* Preserve original NDC.
* Produce normalized NDC.
* Record normalization rule.
* Record match confidence.
* Store unmatched values.
* Never silently discard unmatched values.

## State normalization

Use a state dimension containing:

* StateCode.
* StateName.
* CensusRegion.
* CensusDivision.
* Eligibility flag.
* Included-in-primary-analysis flag.
* Exclusion reason.

---

# 14. Proposed database architecture

## Schemas

```text
raw
stg
core
feature
ml
causal
rag
audit
research
```

## Core tables

### `core.Drug`

```text
DrugId
NormalizedIngredient
IngredientCombination
DosageForm
Route
Strength
RxNormId
TherapeuticClass
CreatedAt
UpdatedAt
```

### `core.DrugProduct`

```text
DrugProductId
DrugId
OriginalNdc
NormalizedNdc
ProductName
Labeler
SourceSystem
MappingConfidence
```

### `core.FirstGenericApproval`

```text
ApprovalId
DrugId
ApprovalDate
ApplicationNumber
Applicant
ApprovalSource
IsPrimaryLaunchReference
```

### `core.State`

```text
StateId
StateCode
StateName
Region
Division
IsEligible
ExclusionReason
```

### `core.CalendarQuarter`

```text
QuarterId
CalendarYear
QuarterNumber
QuarterStartDate
QuarterEndDate
```

### `core.StateDrugUtilization`

```text
StateDrugUtilizationId
DrugId
DrugProductId
StateId
QuarterId
PrescriptionCount
ReimbursementAmount
SourceRowCount
IsSuppressed
DataQualityStatus
DatasetVersionId
```

## Feature tables

### `feature.DrugStateQuarter`

Suggested columns:

```text
DrugId
StateId
QuarterId
ApprovalQuarterId
QuarterSinceApproval
ObservedPrescriptionCount
ObservedReimbursement
IsPresent
IsFirstEntryQuarter
PreviousQuarterPrescriptionCount
PreviousQuarterReimbursement
StateHistoricalGenericEntryRate
StateHistoricalMedianEntryDelay
StateHistoricalMarketWeight
RegionHistoricalEntryRate
NationalStateCoveragePreviousQuarter
NationalPrescriptionCountPreviousQuarter
NationalReimbursementPreviousQuarter
PreviousQuarterND
PreviousQuarterWD
PreviousQuarterAccessGap
StateNeighborAdoptionRate
StateSimilarityAdoptionRate
LaunchCohort
TherapeuticClass
LabelNextQuarterEntry
LabelFuturePrescriptionCount
LabelQ4ND
LabelQ4WD
LabelQ4AccessGap
LabelPersistentInequality
FeatureVersion
```

### `feature.LaunchQuarterSummary`

```text
DrugId
QuarterSinceApproval
ActiveStateCount
EligibleStateCount
NumericDistribution
WeightedDistribution
AccessGap
TotalPrescriptions
TotalReimbursement
ConcentrationIndex
TopStateShare
TopFiveStateShare
RegionalCoverageCount
```

## ML governance tables

### `ml.Experiment`

```text
ExperimentId
ExperimentName
ResearchQuestion
DatasetVersionId
FeatureVersion
CodeCommitHash
ConfigurationJson
StartedAt
CompletedAt
Status
RandomSeed
Notes
```

### `ml.ModelVersion`

```text
ModelVersionId
ExperimentId
ModelName
TaskType
Algorithm
HyperparametersJson
ArtifactPath
ArtifactHash
TrainingPeriodStart
TrainingPeriodEnd
ValidationPeriodStart
ValidationPeriodEnd
TestPeriodStart
TestPeriodEnd
MetricsJson
CalibrationMetricsJson
SubgroupMetricsJson
IsChampion
CreatedAt
```

### `ml.Prediction`

```text
PredictionId
ModelVersionId
DrugId
StateId
QuarterId
PredictionType
PredictedValue
Probability
LowerBound
UpperBound
ActualValue
CreatedAt
```

### `ml.DriftReport`

```text
DriftReportId
ModelVersionId
DatasetVersionId
FeatureDriftJson
PredictionDriftJson
PerformanceDriftJson
SubgroupDriftJson
Decision
CreatedAt
```

## Causal tables

### `causal.AnalysisDefinition`

```text
CausalAnalysisId
Name
TreatmentDefinition
OutcomeDefinition
ConfoundersJson
EffectModifiersJson
EligibilityCriteriaJson
DagVersion
CreatedAt
```

### `causal.Estimate`

```text
CausalEstimateId
CausalAnalysisId
DatasetVersionId
Estimator
Population
AverageTreatmentEffect
StandardError
LowerConfidenceBound
UpperConfidenceBound
DiagnosticsJson
AssumptionsJson
CreatedAt
```

### `causal.CounterfactualScenario`

```text
ScenarioId
DrugId
ScenarioName
BaselineJson
InterventionJson
ModelVersionId
CausalEstimateId
PredictedOutcome
LowerBound
UpperBound
CreatedAt
```

## RAG and LLM tables

### `rag.Document`

```text
DocumentId
Title
SourceOrganization
SourceUrl
PublicationDate
RetrievalDate
DocumentHash
DocumentType
TrustLevel
```

### `rag.DocumentChunk`

```text
ChunkId
DocumentId
ChunkIndex
ChunkText
Embedding
MetadataJson
```

### `rag.LlmInteraction`

```text
InteractionId
PromptVersion
ModelName
Question
StructuredContextJson
RetrievedChunkIdsJson
Response
ValidationStatus
UnsupportedClaimCount
LatencyMs
TokenUsageJson
CreatedAt
```

---

# 15. Cohort construction

A formal cohort builder must determine which launches qualify.

## Example primary eligibility criteria

* Valid FDA approval reference.
* Valid mapping to Medicaid utilization records.
* At least one valid postapproval state observation.
* Minimum required observation window.
* No blocking data-quality errors.
* Sufficient prelaunch information for confounder construction.
* No use of outcome-period values in treatment or confounder features.

## Complete-outcome cohort

For Q4 outcome analysis, require enough elapsed time to observe Q4.

Recent launches without Q4 should not be treated as negative outcomes.

They are right-censored or excluded from that specific analysis.

## Prediction cohort

For next-quarter entry, each eligible drug-state-quarter row must have an observable next quarter unless explicitly treated as censored.

---

# 16. Missingness and suppression

Missing utilization may mean:

* True zero.
* No reported observation.
* Suppression.
* Mapping failure.
* Source omission.

These must not be treated as identical.

Create explicit indicators:

```text
IsObservedZero
IsMissing
IsSuppressed
IsUnmapped
```

The primary paper must document the selected interpretation.

Sensitivity analyses should compare reasonable alternatives.

---

# 17. Data leakage prevention

The following are prohibited as features for a prediction made at time `t`:

* Values from quarter `t+1` or later.
* Q4 outcomes when predicting Q4.
* Final adoption class.
* Aggregate statistics calculated using the test period.
* Future state entry.
* Features standardized using the entire dataset.
* Target-derived variables.
* Embeddings trained using future labels without isolation.

Every feature must have an `AvailableAsOf` concept.

The feature-generation code should support:

```text
AsOfQuarter
```

Any feature not knowable at the prediction time must be rejected.

---

# 18. Dataset splitting strategy

Random row splitting must not be the primary evaluation.

## Required split types

### Temporal launch-cohort split

Example structure:

```text
Train: earliest launch cohorts
Validation: subsequent launch cohorts
Test: most recent fully observable launch cohorts
```

Exact dates must be data-driven and documented.

### Grouped split

All rows from the same drug must remain in one partition where appropriate.

### Rolling-origin evaluation

Train on all data available up to time `t` and evaluate on the next period.

Repeat across multiple cutoffs.

### Leave-one-cohort-out sensitivity

Hold out launch cohorts to measure generalization.

### Optional region holdout

Evaluate geographic robustness by holding out states or regions.

## Leakage audit

Every experiment must run automated checks verifying:

* No DrugId overlap when the experiment requires unseen-drug testing.
* No future feature timestamp.
* No duplicate observation across partitions.
* No feature fitted on test data.
* No label contamination.

---

# 19. Predictive tasks

## Task 1 — Next-quarter state entry

### Unit

Drug–state–quarter where the state has not entered yet.

### Target

```text
LabelNextQuarterEntry = 1
```

when the drug meets the state-entry definition in the next quarter.

### Models

* Logistic regression baseline.
* FastTree binary classification.
* FastForest binary classification.
* LightGBM binary classification.
* Calibrated ensemble.

### Metrics

* ROC AUC.
* PR AUC.
* Log loss.
* Brier score.
* Precision.
* Recall.
* F1.
* Recall at top K.
* Calibration slope.
* Expected Calibration Error.
* Performance by launch cohort.
* Performance by region.
* Performance by therapeutic class.

PR AUC should be emphasized when positive entry events are uncommon.

## Task 2 — Time-to-state-entry

### Target

Number of quarters from approval to state entry.

### Method options

* Discrete-time hazard model.
* Regression baseline.
* Survival analysis in Python.
* Censoring-aware evaluation.

### Metrics

* Concordance index.
* Time-dependent Brier score.
* Median absolute error for uncensored events.
* Calibration by time horizon.

## Task 3 — Future Numeric Distribution

Predict Q4 ND using only features available by Q1 or another declared cutoff.

### Models

* Mean baseline.
* Linear regression.
* FastTree regression.
* FastForest regression.
* LightGBM regression.
* Weighted ensemble.

### Metrics

* MAE.
* RMSE.
* R².
* Median absolute error.
* Error by launch cohort.
* Prediction-interval coverage.

## Task 4 — Future Weighted Distribution

Same model family and validation strategy as ND.

## Task 5 — Future Access Gap

Predict Q4 Access Gap.

Because Access Gap is derived from ND and WD, compare:

1. Direct Access Gap prediction.
2. Separately predicting ND and WD and calculating the difference.
3. A multi-output approximation or coordinated model.

## Task 6 — Persistent inequality risk

Define a preregistered threshold such as:

```text
Q4 Access Gap ≥ threshold
```

The threshold must be justified from the empirical distribution or an external conceptual criterion.

Do not select the final threshold after seeing test-set results.

---

# 20. Baseline models

Every advanced model must beat meaningful baselines.

Required baselines:

* Overall mean or prevalence.
* Previous-quarter value.
* Linear or logistic model.
* Historical state entry rate.
* Nearest historical launch analogue.
* Simple rule using Q1 ND or Q1 Access Gap.

Without baselines, high model complexity is not scientifically meaningful.

---

# 21. ML.NET modeling strategy

ML.NET will be the production predictive framework.

ML.NET provides LightGBM and FastTree trainers for classification and regression. The project must pin stable package versions rather than relying on preview APIs.

## Required ML pipelines

### Numeric preprocessing

* Missing-value replacement.
* Robust transformations where needed.
* Optional log transform for highly skewed counts.
* Feature concatenation.
* Normalization only when required by the selected learner.

### Categorical preprocessing

* One-hot encoding for low-cardinality fields.
* Hashing only when necessary.
* Unknown-category handling.
* Stable category mapping.

### Class imbalance

Evaluate:

* Class weights.
* Threshold adjustment.
* Resampling only inside training data.
* Ranking metrics.
* Calibrated probability output.

## Hyperparameter search

Use a controlled experiment process.

Do not allow unrestricted AutoML to become the research contribution.

For each algorithm, define a bounded search space.

Record:

* Search space.
* Trial count.
* Random seed.
* Optimization metric.
* Best configuration.
* All failed and completed trials.

## Ensemble

Create a weighted ensemble only if validation supports it.

Possible ensemble:

```text
FinalPrediction =
    w1 × LightGBM
  + w2 × FastTree
  + w3 × FastForest
  + w4 × LinearBaseline
```

Weights must be learned from validation data, not test data.

## Calibration

For binary models:

* Reliability plots.
* Brier score.
* Expected Calibration Error.
* Isotonic or Platt-style calibration where supported or implemented.
* Calibration by subgroup.

## Explainability

Include:

* Permutation Feature Importance.
* Tree feature weights where appropriate.
* Partial dependence calculated carefully.
* Local perturbation-based explanations.
* Global and subgroup feature importance.
* Stability of feature importance across folds.

Feature importance is not causal evidence.

The UI must explicitly say so.

---

# 22. Causal question formulation

Each causal analysis must define:

```text
Population
Treatment
Comparator
Outcome
Time zero
Follow-up window
Measured confounders
Effect modifiers
Eligibility criteria
Estimator
Estimand
```

## Example primary causal question

### Population

Eligible generic launches with complete Q1 and Q4 observation windows.

### Treatment

Broader-than-threshold Q1 state coverage.

Example:

```text
T = 1 when Q1 ND ≥ prespecified threshold
T = 0 otherwise
```

Alternative continuous treatment:

```text
T = Q1 ND
```

Binary treatment is easier for the first implementation.

### Outcome

Q4 Numeric Distribution or Q4 Access Gap.

### Time zero

End of Q1 after the approval reference quarter.

### Follow-up

Through Q4.

### Estimand

* Average Treatment Effect.
* Average Treatment Effect on the Treated.
* Conditional Average Treatment Effect.

## Important treatment-design rule

Do not define treatment using the same information as the outcome.

Do not include posttreatment variables as confounders.

---

# 23. Proposed treatment definitions

The project should support multiple analyses but designate one primary analysis.

## Treatment A — High early geographic breadth

```text
Q1 ND above a prespecified threshold
```

## Treatment B — High early concentration

```text
Q1 top-five-state utilization share above a threshold
```

## Treatment C — Early regional diversity

```text
Presence in a prespecified number of Census regions during Q1
```

## Treatment D — Early entry into historically high-readiness states

This is more difficult to interpret and should remain secondary.

## Treatment E — Continuous early coverage

Use generalized propensity or continuous-treatment methods only after the binary treatment pipeline is validated.

---

# 24. Confounders

Potential measured confounders available before or at treatment assignment may include:

* Prelaunch brand or reference market size.
* Drug or molecule category.
* Dosage form.
* Launch year and quarter.
* Number of mapped products.
* Initial national prescription volume.
* Historical utilization of the therapeutic class.
* Historical state generic-adoption readiness.
* Geographic market-weight distribution.
* Manufacturer-related characteristics when consistently available.
* Baseline concentration.
* Number of eligible states with valid data.
* Data completeness indicators.

Every confounder must be justified as a common cause of treatment and outcome or a precision variable.

Do not automatically include every variable.

---

# 25. Variables that may not be adjusted for

Avoid adjusting for:

* Variables caused by Q1 breadth.
* Q2 or later utilization.
* Later state entry.
* Q4 market size.
* Postlaunch manufacturer expansion measures.
* Any mediator unless performing a separate mediation analysis.

Conditioning on mediators can distort the total effect.

---

# 26. Causal directed acyclic graph

Create a version-controlled DAG.

Example conceptual graph:

```text
Drug characteristics ─────────┬──> Early breadth
                              └──> Later access

Baseline market size ─────────┬──> Early breadth
                              └──> Later access

Launch cohort ────────────────┬──> Early breadth
                              └──> Later access

State readiness ──────────────┬──> Early breadth
                              └──> Later access

Early breadth ───────────────────> Later access

Unmeasured supply factors ────┬──> Early breadth
                              └──> Later access
```

The unmeasured supply factor should be shown to make limitations visible.

Maintain the DAG in:

```text
research/causal/dag/
```

Suggested formats:

* DOT.
* Mermaid.
* JSON metadata.
* Rendered PNG or SVG.

---

# 27. Causal estimation pipeline

## Stage 1 — Unadjusted comparison

Calculate:

* Mean outcome in treated group.
* Mean outcome in comparison group.
* Raw difference.
* Confidence interval.

This is descriptive, not causal.

## Stage 2 — Propensity model

Estimate:

```text
P(Treatment = 1 | measured confounders)
```

Models:

* Logistic regression.
* LightGBM sensitivity model.

The primary propensity model should favor interpretability and stability.

## Stage 3 — Overlap diagnostics

Evaluate:

* Propensity distributions.
* Common support.
* Extreme propensity values.
* Effective sample size.
* Positivity violations.
* Treated/control overlap by cohort.

Trim or restrict only using prespecified rules.

Report how many observations are removed.

## Stage 4 — Covariate balance

Before and after adjustment, calculate:

* Standardized Mean Difference.
* Variance ratios.
* Love plot.
* Maximum absolute imbalance.
* Effective sample size.

Suggested balance target:

```text
Absolute SMD < 0.10
```

This is a guideline, not proof of successful identification.

## Stage 5 — Estimators

Implement and compare:

1. Outcome regression.
2. Inverse Probability of Treatment Weighting.
3. Augmented Inverse Probability Weighting.
4. Doubly robust estimation.
5. Matching as a sensitivity method.
6. Causal forest or DML in Python for heterogeneous effects.

## Stage 6 — Uncertainty

Use:

* Bootstrap confidence intervals clustered by drug where appropriate.
* Robust standard errors.
* Repeated sample splitting for DML.
* Confidence intervals for ATE and CATE.

## Stage 7 — Refutation and sensitivity

Required checks:

* Placebo treatment.
* Random common-cause addition.
* Subset refutation.
* Bootstrap refutation.
* Alternative treatment threshold.
* Alternative state-entry definition.
* Alternative weighting definition.
* Leave-one-launch-out analysis.
* Leave-one-cohort-out analysis.
* Sensitivity to unmeasured confounding.
* Negative-control outcome or exposure if a credible one can be defined.

DoWhy explicitly supports a workflow involving modeling assumptions, identification, estimation and refutation, which is why Python verification is included.

---

# 28. Python research-validation module

Python is not required to run the web application, but it is strongly recommended for research validation.

## Environment

Use:

```text
Python 3.x
venv
requirements.txt or pyproject.toml
```

## Libraries

Potential libraries:

```text
pandas
numpy
scikit-learn
statsmodels
matplotlib
dowhy
econml
shap
jupyter
pyarrow
sqlalchemy
```

Pin exact versions after setup.

## Responsibilities

The Python module will:

* Independently reconstruct analysis cohorts.
* Validate selected ML.NET predictions.
* Implement DoWhy causal workflows.
* Implement EconML estimators.
* Estimate heterogeneous effects.
* Produce research diagnostics.
* Produce publication figures.
* Export machine-readable results to the .NET application.

EconML includes causal ML estimators such as CATE and double-machine-learning approaches, while DoWhy provides a structured causal analysis and refutation framework.

## Integration rule

The ASP.NET application must not execute arbitrary notebook code.

Use a controlled interface:

```text
.NET exports versioned Parquet or CSV snapshot
Python runs a versioned analysis command
Python exports validated JSON results
.NET imports only schema-validated results
```

Possible production integration can later use a Python worker, but file-based reproducible exchange is safer initially.

---

# 29. Counterfactual simulation

The counterfactual simulator will estimate expected outcomes under user-defined hypothetical interventions.

## Example scenarios

* Increase Q1 ND from observed value to a selected value.
* Add entry into a selected number of states.
* Reduce Q1 top-five-state concentration.
* Increase regional diversity.
* Apply the early-breadth pattern of a matched historical launch.
* Move a launch from low-breadth to high-breadth treatment.

## Simulator output

For each scenario, return:

```text
Observed treatment
Simulated treatment
Observed or predicted baseline outcome
Estimated counterfactual outcome
Estimated change
Confidence interval
Eligible population
Estimator
Model version
Dataset version
Assumptions
Overlap warning
Extrapolation warning
Top effect modifiers
```

## Guardrails

The simulator must:

* Reject impossible values.
* Detect values outside observed support.
* Warn when extrapolating.
* Avoid displaying precise causal values when uncertainty is excessive.
* Distinguish predictive simulation from causal simulation.
* Never use Gemini to calculate the numerical estimate.
* Log every scenario.

## Individual treatment effect caution

Drug-specific counterfactual estimates should be labeled:

```text
Estimated conditional effect
```

not a known individual causal truth.

---

# 30. Similar-launch matching

To make results interpretable, identify historical analogues.

Similarity features may include:

* Therapeutic class.
* Baseline market size.
* Approval cohort.
* Dosage form.
* Q1 ND.
* Q1 WD.
* Concentration.
* Initial prescription volume.
* Regional distribution.

Use distance calculation after training-only scaling.

The system may display:

* Most similar historical launches.
* Similarity score.
* Their observed trajectories.
* Why they were selected.

Similarity must not leak future outcomes into the prediction model.

---

# 31. State-similarity and geographic features

A lightweight graph-inspired layer can improve the project without requiring a full GNN.

## State relationships

* Geographic adjacency.
* Same Census region.
* Historical utilization-profile similarity.
* Historical generic-entry similarity.
* Similar total Medicaid market size.

## Derived features

* Share of neighboring states already entered.
* Similar-state adoption rate.
* Region-level adoption rate.
* Weighted exposure to entered states.
* State readiness percentile.
* Historical median entry delay.

These must be calculated only from information available before the prediction time.

---

# 32. Uncertainty framework

The project must not provide unsupported point predictions alone.

## Predictive uncertainty

Use:

* Bootstrap ensembles.
* Quantile models where practical.
* Residual-based prediction intervals.
* Conformal prediction as an optional advanced method.
* Calibration intervals.

## Causal uncertainty

Use:

* Confidence intervals.
* Bootstrap distributions.
* Sensitivity ranges.
* Overlap warnings.
* Effective sample size.

## UI display

Show:

```text
Estimated Q4 ND: 54.2%
95% interval: 46.1%–62.8%
```

Also show plain-language interpretation.

---

# 33. Continual-learning design

The system should be described as **governed continual learning**, not unrestricted self-learning.

## Trigger

A candidate cycle may begin when:

* A new quarter is ingested.
* A material amount of new data is available.
* Drift exceeds a threshold.
* Scheduled periodic evaluation occurs.
* A model underperforms.

## Workflow

1. Ingest new immutable raw data.
2. Run schema and quality validation.
3. Create a new dataset version.
4. Recalculate features using the feature version.
5. Score the existing champion model.
6. Calculate drift.
7. Train challenger models.
8. Evaluate challengers using temporal back-testing.
9. Compare calibration.
10. Compare subgroup performance.
11. Compare robustness.
12. Generate a promotion recommendation.
13. Require human approval initially.
14. Promote or reject.
15. Retain rollback capability.
16. Record the full decision.

## Promotion criteria

A configurable policy may require:

```text
Primary metric improves by at least a configured margin
AND calibration does not materially worsen
AND worst-group performance does not materially worsen
AND no blocking data-quality issue exists
AND no leakage test fails
AND model artifact validation passes
AND reproducibility check passes
```

Example thresholds must be configuration values, not hard-coded scientific truths.

## Champion–challenger strategy

Maintain:

* One production champion.
* Multiple challengers.
* Archived rejected models.
* Reason for promotion or rejection.
* Full model lineage.

## Rollback

If postdeployment evaluation shows significant degradation:

* Mark the model unhealthy.
* Restore the previous approved champion.
* Record the incident.
* Do not delete the failed model.

---

# 34. Drift monitoring

## Data drift

Monitor:

* Population Stability Index.
* Jensen–Shannon divergence.
* Kolmogorov–Smirnov statistic for numeric variables.
* Category-frequency changes.
* Missingness changes.
* State and drug cohort composition.

## Prediction drift

Monitor:

* Probability distribution.
* Predicted risk distribution.
* Predicted ND/WD/Gap distribution.
* Confidence interval width.
* Rate of out-of-support scenarios.

## Performance drift

When labels become available:

* AUC changes.
* PR AUC changes.
* MAE changes.
* Calibration changes.
* Subgroup error changes.

## Concept drift

Investigate whether relationships between features and outcomes changed.

Drift alone does not mandate retraining.

---

# 35. Gemini integration

Gemini will be an explanation and retrieval layer, not the numerical inference engine.

Google’s Gemini APIs currently support function calling, structured output and embeddings. The current official documentation recommends the Interactions API for new projects as of June 2026, but the provider must remain abstracted because APIs and model availability change.

## Gemini responsibilities

* Explain a prediction.
* Explain an estimated causal effect.
* Compare baseline and counterfactual scenarios.
* Retrieve methodology documents.
* Explain ND, WD and Access Gap.
* Summarize model limitations.
* Generate a structured research narrative.
* Translate a natural-language question into an allow-listed analytical request.
* Refuse unsupported requests.
* Produce evidence-linked responses.

## Gemini must not

* Calculate model outputs.
* Invent numerical values.
* Execute arbitrary SQL.
* alter the production model.
* Promote a model.
* Claim causation beyond the stored analysis.
* Provide clinical recommendations.
* Cite documents that were not retrieved.
* use internet search as an unverified source inside scientific results.

## Provider abstraction

Define:

```csharp
public interface ILanguageModelClient
{
    Task<StructuredLlmResponse<T>> GenerateStructuredAsync<T>(
        LlmRequest request,
        CancellationToken cancellationToken);
}
```

The implementation may use Gemini, but the rest of the application must not depend directly on Gemini-specific classes.

## Structured inputs

Gemini should receive verified context such as:

```json
{
  "analysisType": "counterfactual",
  "drug": {
    "id": 123,
    "displayName": "Example Drug"
  },
  "baseline": {
    "q1NumericDistribution": 18.2,
    "predictedQ4NumericDistribution": 41.7
  },
  "scenario": {
    "q1NumericDistribution": 30.0,
    "estimatedQ4NumericDistribution": 52.4,
    "confidenceInterval": [45.8, 59.1]
  },
  "method": {
    "estimator": "AIPW",
    "datasetVersion": "2026Q2-v1",
    "overlapStatus": "acceptable"
  },
  "limitations": [
    "Observational estimate",
    "Unmeasured supply factors may remain"
  ]
}
```

## Structured output

Require schema-validated fields:

```json
{
  "summary": "",
  "keyDrivers": [],
  "uncertaintyExplanation": "",
  "assumptions": [],
  "limitations": [],
  "evidenceReferences": [],
  "unsupportedClaimsDetected": false
}
```

## LLM validation

After receiving a response:

1. Parse structured output.
2. Verify every numerical value against supplied context.
3. Ensure citations refer to retrieved chunks.
4. Detect forbidden causal language.
5. Remove or reject unsupported claims.
6. Log the interaction.
7. fall back to deterministic templates if validation fails.

---

# 36. RAG architecture

## Trusted document collection

Include:

* FDA methodological documents.
* CMS Medicaid data dictionaries.
* Official dataset documentation.
* Project data dictionary.
* Model cards.
* Causal analysis definitions.
* Research paper references with stored metadata.
* Internal project methodology.

## Document ingestion

1. Download or add approved document.
2. Calculate hash.
3. Record metadata.
4. Extract text.
5. Split semantically.
6. Generate embeddings.
7. Store chunks.
8. Validate retrieval.
9. Mark document active.

Gemini provides embedding APIs and managed file-search capabilities, but the system should hide provider-specific retrieval behind an interface.

## Retrieval strategy

Use:

* Metadata filtering.
* Semantic retrieval.
* Keyword fallback.
* Document trust ranking.
* Top-K limits.
* Duplicate-chunk removal.

## Citation format

Every LLM answer about methodology must cite:

```text
Document title
Source organization
Section or chunk
Publication date where available
```

---

# 37. LLM evaluation

Create a fixed benchmark set.

## Question categories

* Metric explanation.
* Prediction explanation.
* Causal estimate explanation.
* Limitation identification.
* Source retrieval.
* Unsupported clinical request.
* Prompt-injection attempt.
* Numerical consistency.
* Counterfactual comparison.
* Ambiguous question.

## Metrics

* Numerical faithfulness.
* Unsupported-claim rate.
* Citation precision.
* Citation recall.
* Structured-output validity.
* Correct refusal rate.
* Completeness.
* Latency.
* Cost.
* Human evaluator score.

## Comparative experiments

Compare:

1. Unconstrained prompt.
2. Structured context only.
3. RAG only.
4. Structured context plus RAG.
5. Structured context plus RAG plus validation.
6. Deterministic template baseline.

The final research paper may include the LLM as a secondary evaluation rather than the central scientific contribution.

---

# 38. Application architecture

## Recommended platform

* Current stable .NET release available at project creation.
* ASP.NET Core.
* Blazor Server or ASP.NET Core MVC for the initial UI.
* Web API endpoints.
* SQL Server.
* Entity Framework Core for transactional metadata.
* Dapper for complex analytical queries where useful.
* ML.NET for predictive modeling.
* Background worker for training and ingestion.
* Python research-validation module.
* Gemini API through a provider abstraction.
* xUnit for tests.
* Serilog or equivalent structured logging.
* OpenTelemetry where practical.

## Clean architecture projects

```text
src/
  PharmaAccess.Domain
  PharmaAccess.Application
  PharmaAccess.Infrastructure
  PharmaAccess.Data
  PharmaAccess.ML
  PharmaAccess.Causal
  PharmaAccess.Llm
  PharmaAccess.Worker
  PharmaAccess.Api
  PharmaAccess.Web

tests/
  PharmaAccess.Domain.Tests
  PharmaAccess.Application.Tests
  PharmaAccess.Data.Tests
  PharmaAccess.ML.Tests
  PharmaAccess.Causal.Tests
  PharmaAccess.Llm.Tests
  PharmaAccess.Api.IntegrationTests

research/
  python
  notebooks
  causal
  experiments
  figures
  tables
  paper

database/
  migrations
  schemas
  tables
  views
  procedures
  validation
  seed

docs/
  architecture
  data
  models
  causal
  llm
  operations
  research
```

---

# 39. Domain design

Core domain entities:

```text
Drug
DrugProduct
Approval
State
CalendarQuarter
UtilizationObservation
DatasetVersion
FeatureSetVersion
Experiment
ModelVersion
Prediction
CausalAnalysis
CausalEstimate
CounterfactualScenario
DriftReport
Document
LlmInteraction
```

Use strongly typed value objects where helpful:

```text
DrugId
StateCode
Quarter
Percentage
ModelVersionId
DatasetVersionId
```

Domain entities must not depend on ML.NET, Entity Framework or Gemini classes.

---

# 40. API design

Suggested endpoints:

## Drugs

```text
GET /api/drugs
GET /api/drugs/{drugId}
GET /api/drugs/{drugId}/trajectory
GET /api/drugs/{drugId}/similar-launches
```

## States

```text
GET /api/states
GET /api/states/{stateCode}
GET /api/states/{stateCode}/historical-readiness
```

## Predictions

```text
POST /api/predictions/next-state-entry
POST /api/predictions/q4-access
GET /api/predictions/{predictionId}
```

## Counterfactuals

```text
POST /api/counterfactuals
GET /api/counterfactuals/{scenarioId}
```

## Causal analysis

```text
GET /api/causal/analyses
GET /api/causal/analyses/{id}
GET /api/causal/analyses/{id}/diagnostics
```

## Explanations

```text
POST /api/explanations/prediction
POST /api/explanations/counterfactual
POST /api/explanations/methodology
```

## Models

```text
GET /api/models
GET /api/models/champion
GET /api/models/{modelVersionId}
GET /api/models/{modelVersionId}/card
```

## Operations

```text
POST /api/admin/data/import
POST /api/admin/training/run
POST /api/admin/models/{id}/approve
POST /api/admin/models/{id}/reject
POST /api/admin/models/{id}/rollback
GET /api/admin/drift
```

Administrative routes require authorization.

---

# 41. User interface

## Dashboard

Show:

* Number of eligible launches.
* Number of drugs.
* Number of states.
* Latest dataset version.
* Champion model.
* Latest drift status.
* Data-quality warnings.
* Summary access metrics.

## Drug explorer

Show:

* Drug details.
* Approval reference.
* State-entry map or table.
* Quarterly ND.
* Quarterly WD.
* Quarterly Access Gap.
* Similar launches.
* Predicted future trajectory.
* Uncertainty.

## State explorer

Show:

* Historical entry speed.
* Historical generic presence.
* Market weight.
* Region.
* Recent drug entries.
* Model performance for the state.

## Prediction page

Allow:

* Select drug.
* Select prediction cutoff.
* View predicted state entry.
* View probabilities.
* View calibration warning.
* View model explanation.
* View model version.

## Counterfactual simulator

Allow:

* Select drug.
* Select treatment.
* Adjust intervention value.
* Run scenario.
* Compare baseline and counterfactual.
* View confidence interval.
* View overlap and extrapolation status.
* Request grounded explanation.

## Model governance page

Show:

* Champion.
* Challengers.
* Metrics.
* Calibration.
* subgroup performance.
* Drift.
* promotion recommendation.
* approval history.
* rollback action.

## Research page

Show:

* Dataset version.
* Cohort flow.
* Hypotheses.
* Experiment results.
* Causal diagnostics.
* Downloadable tables.
* Reproducibility manifest.

---

# 42. Authentication and security

The public research dashboard may be read-only.

Administrative functions require authentication.

## Protect

* Gemini API keys.
* Database credentials.
* model artifacts.
* raw data import.
* training triggers.
* model promotion.
* rollback actions.

## Secrets

Use:

* Development user secrets locally.
* Environment variables or secret manager in deployment.
* Never commit keys.
* Include `.env.example` without real values.

## LLM security

* Treat retrieved documents as untrusted content.
* Ignore instructions contained within documents.
* Enforce allow-listed tools.
* No arbitrary SQL.
* No arbitrary file access.
* Maximum prompt length.
* Rate limiting.
* Output schema validation.
* Prompt-injection tests.

---

# 43. Observability

Use structured logs.

Every prediction should include:

```text
CorrelationId
ModelVersionId
DatasetVersionId
FeatureVersion
Request timestamp
Latency
Success status
```

Every LLM request should include:

```text
PromptVersion
Model provider
Model name
Retrieved document IDs
Token usage
Validation result
Latency
```

Do not log secret keys or sensitive connection information.

---

# 44. Testing strategy

## Unit tests

Test:

* ND calculation.
* WD calculation.
* Access Gap calculation.
* Quarter calculation.
* State-entry definition.
* NDC normalization.
* Treatment assignment.
* Propensity weight calculation.
* AIPW formula.
* Confidence interval calculation.
* Counterfactual validation.
* structured LLM validation.
* promotion policy.

## Data tests

Test:

* Unique keys.
* Null rules.
* Valid state codes.
* date consistency.
* pre/postlaunch consistency.
* no future feature availability.
* cohort inclusion.
* dataset row counts.

## ML tests

Test:

* Training completes.
* Same seed gives reproducible results within tolerance.
* No partition leakage.
* Prediction schema is valid.
* artifact can be loaded.
* artifact hash matches.
* baseline comparisons run.
* metrics are persisted.

## Causal tests

Use synthetic data with known treatment effect.

Verify:

* Propensity score behavior.
* IPTW recovers a known effect under ideal conditions.
* AIPW recovers a known effect under one correctly specified nuisance model.
* overlap warnings trigger.
* extreme weights are detected.
* bootstrap output is stable.

## LLM tests

Use mocked provider responses.

Test:

* Invalid JSON.
* invented number.
* missing citation.
* forbidden causal claim.
* prompt injection.
* timeout.
* provider failure.
* deterministic fallback.

## Integration tests

Test:

* Database migration.
* data ingestion.
* feature generation.
* model training.
* prediction API.
* counterfactual API.
* explanation API.
* model approval workflow.

## End-to-end tests

A complete scenario:

1. Load sample data.
2. Build features.
3. Train models.
4. register champion.
5. request prediction.
6. run counterfactual.
7. generate explanation.
8. verify audit records.

---

# 45. Reproducibility

Each experiment must be reproducible from:

```text
Git commit
Dataset version
Feature version
Configuration file
Random seed
Package lock
Environment details
Model artifact hash
```

Create:

```text
research_manifest.json
```

Example fields:

```json
{
  "experimentId": "",
  "gitCommit": "",
  "datasetVersion": "",
  "featureVersion": "",
  "randomSeed": 42,
  "dotnetVersion": "",
  "pythonVersion": "",
  "mlNetPackages": {},
  "pythonPackages": {},
  "trainPeriod": {},
  "validationPeriod": {},
  "testPeriod": {},
  "artifactHashes": {}
}
```

---

# 46. Model cards

Every approved model must have a model card containing:

* Purpose.
* task.
* intended users.
* nonintended uses.
* training data period.
* outcome definition.
* features.
* algorithm.
* metrics.
* calibration.
* subgroup performance.
* known limitations.
* uncertainty method.
* drift policy.
* approval history.
* ethical considerations.

---

# 47. Data cards

Each research dataset must include:

* Source.
* retrieval period.
* inclusion criteria.
* exclusions.
* missingness.
* suppression treatment.
* mapping process.
* label construction.
* potential biases.
* intended use.
* prohibited use.
* version and hash.

---

# 48. Ethical analysis

Potential concerns:

* Medicaid utilization is not equivalent to total population access.
* Approval date may not equal commercial launch date.
* Claims data reflect program and reporting processes.
* State differences may reflect policies, demographics, reporting and supply factors not measured.
* Low utilization does not necessarily prove unavailable supply.
* State comparisons may be misinterpreted.
* Manufacturer-related interpretations may be speculative.
* Model errors could disproportionately affect low-data states.

Mitigations:

* Clear scope.
* no clinical use.
* confidence intervals.
* subgroup evaluation.
* explicit missingness.
* no unsupported blame.
* observational causal disclaimers.
* auditability.
* human review.

---

# 49. Paper experimental design

## Study design

Retrospective longitudinal observational study.

## Main analysis

Estimate the effect of broad Q1 geographic coverage on Q4 access.

## Main outcome

Choose one primary outcome:

* Q4 Numeric Distribution, or
* Q4 Access Gap.

Recommended primary outcome:

```text
Q4 Numeric Distribution
```

Recommended secondary outcome:

```text
Q4 Access Gap
```

ND is easier to explain causally. Access Gap should remain an important secondary outcome.

## Primary estimator

Augmented Inverse Probability Weighting.

## Secondary estimators

* Outcome regression.
* IPTW.
* matching.
* DML.
* causal forest for heterogeneity.

## Main predictive task

Next-quarter state entry.

## Validation

Temporal grouped split.

## Required ablations

* Without state-history features.
* Without launch characteristics.
* Without graph-inspired features.
* Without calibration.
* Static model versus governed retraining.
* Direct Access Gap model versus derived Gap model.

## Required sensitivity analyses

* Alternative entry threshold.
* Alternative Q1 breadth threshold.
* Alternative WD weights.
* Alternative cohort dates.
* trimming extreme propensity.
* complete-case versus missingness-aware analysis.
* unobserved-confounding sensitivity.
* leave-one-cohort-out.
* leave-one-drug-out.

---

# 50. Proposed paper structure

## Abstract

* Background.
* objective.
* data.
* methods.
* principal results.
* conclusion.
* limitations.

## 1. Introduction

* Generic access problem.
* geographic variation.
* predictive versus causal gap.
* contribution.

## 2. Related work

* Generic adoption.
* Medicaid utilization research.
* distribution metrics.
* causal ML.
* temporal adoption forecasting.
* LLM-grounded explanation.

## 3. Data

* FDA.
* Medicaid SDUD.
* mapping.
* cohort.
* variables.
* missingness.

## 4. Methods

* ND, WD, Access Gap.
* predictive tasks.
* temporal splits.
* causal DAG.
* treatment and outcomes.
* estimators.
* uncertainty.
* refutation.
* LLM explanation system.

## 5. Results

* cohort.
* descriptive statistics.
* prediction.
* calibration.
* causal balance.
* effect estimates.
* heterogeneity.
* sensitivity.
* LLM evaluation.

## 6. Discussion

* interpretation.
* contribution.
* implications.
* comparison to earlier work.
* limitations.

## 7. Conclusion

Careful, limited conclusions.

## Appendices

* SQL definitions.
* feature dictionary.
* hyperparameters.
* cohort flow.
* additional balance plots.
* model cards.
* prompt templates.
* reproducibility manifest.

---

# 51. Publication-quality figures

Required figures may include:

1. End-to-end system architecture.
2. Cohort-construction flowchart.
3. Causal DAG.
4. State-entry trajectory examples.
5. ND, WD and Access Gap over time.
6. Temporal validation design.
7. ROC and PR curves.
8. Calibration plot.
9. Feature-importance stability.
10. Propensity overlap plot.
11. Covariate Love plot.
12. ATE forest plot across estimators.
13. Heterogeneous-effect plot.
14. Counterfactual scenario visualization.
15. Drift and champion–challenger workflow.
16. LLM factuality comparison.

---

# 52. Publication-quality tables

1. Data-source summary.
2. Cohort inclusion and exclusions.
3. Descriptive launch statistics.
4. State-level summary.
5. Predictive-model comparison.
6. Calibration metrics.
7. Subgroup performance.
8. Covariate balance.
9. Causal estimates.
10. Sensitivity analyses.
11. Counterfactual examples.
12. LLM evaluation.
13. Ablation results.
14. Reproducibility information.

---

# 53. Development phases

## Phase 0 — Repository and governance

Deliver:

* New repository.
* solution.
* README.
* blueprint.
* coding standards.
* architecture record.
* issue templates.
* contribution guide.
* license decision.
* secret handling.
* CI skeleton.

## Phase 1 — Database and ingestion

Deliver:

* database schemas.
* core entities.
* import pipeline.
* normalization.
* data-quality checks.
* dataset versioning.
* sample dataset.

## Phase 2 — Feature engineering

Deliver:

* drug–state–quarter table.
* launch-quarter summary.
* ND/WD/Gap implementation.
* temporal features.
* state-history features.
* labels.
* leakage audit.

## Phase 3 — Predictive ML baseline

Deliver:

* baseline models.
* temporal split.
* LightGBM, FastTree, FastForest.
* model registry.
* evaluation reports.
* prediction API.

## Phase 4 — Explainability and uncertainty

Deliver:

* calibration.
* feature importance.
* bootstrap intervals.
* subgroup evaluation.
* error analysis.

## Phase 5 — Causal foundation

Deliver:

* causal question definitions.
* DAG.
* propensity model.
* overlap.
* balance diagnostics.
* IPTW.
* outcome regression.
* AIPW.
* synthetic-data tests.

## Phase 6 — Python validation

Deliver:

* reproducible Python environment.
* DoWhy analysis.
* EconML analysis.
* refutation tests.
* CATE estimation.
* exported results.

## Phase 7 — Counterfactual simulator

Deliver:

* scenario engine.
* support validation.
* uncertainty.
* API.
* UI.
* scenario audit.

## Phase 8 — Gemini and RAG

Deliver:

* provider abstraction.
* Gemini client.
* document ingestion.
* embeddings.
* retrieval.
* structured explanations.
* validators.
* benchmark tests.

## Phase 9 — Continual learning

Deliver:

* drift detection.
* scheduled candidate training.
* champion–challenger comparison.
* promotion policy.
* approval UI.
* rollback.

## Phase 10 — Research package

Deliver:

* frozen dataset.
* full experiments.
* figures.
* tables.
* paper draft.
* model cards.
* data card.
* reproducibility manifest.

---

# 54. Minimum viable research product

The first scientifically meaningful release must contain:

* Reproducible drug–state–quarter dataset.
* Next-quarter state-entry model.
* Q4 ND prediction model.
* Temporal grouped validation.
* Model baselines.
* calibration.
* primary causal DAG.
* propensity diagnostics.
* AIPW estimate.
* confidence interval.
* one counterfactual simulator.
* methodological disclaimer.
* complete experiment log.

Gemini is not required for the first research milestone.

---

# 55. Advanced final release

The advanced release should contain:

* Multiple predictive tasks.
* calibrated ensemble.
* uncertainty intervals.
* state-similarity features.
* Python DML or causal forest.
* heterogeneous treatment effects.
* refutation suite.
* Gemini RAG.
* LLM factuality benchmark.
* governed continual learning.
* model cards.
* research dashboard.
* publication-ready paper.

---

# 56. Success criteria

## Engineering success

* Clean build.
* automated tests.
* reproducible local setup.
* no committed secrets.
* database migration works.
* model artifacts versioned.
* complete logs.
* APIs documented.
* rollback works.

## Predictive success

* Advanced model beats declared baselines on future cohorts.
* probability calibration is acceptable.
* performance is reported by subgroup.
* no known leakage.
* uncertainty is reported.

## Causal success

* Treatment and outcome are prespecified.
* DAG is explicit.
* overlap is acceptable or limitations are reported.
* balance improves.
* estimates are robust across reasonable methods.
* refutation and sensitivity results are reported.
* causal language remains appropriately limited.

## LLM success

* Numerical faithfulness is high.
* unsupported-claim rate is low.
* citations are valid.
* prompt injection is rejected.
* deterministic fallback works.

## Publication success

* Central question is narrow.
* methods are reproducible.
* test cohort remains untouched until final evaluation.
* conclusions match evidence.
* limitations are substantial and honest.
* project is clearly distinct from previous work.

---

# 57. Final implementation principles

1. Build data correctness before model complexity.
2. Separate prediction from causality.
3. Keep the test cohort untouched.
4. Do not use LLM output as scientific evidence.
5. Never allow future information into features.
6. Prefer reproducibility over flashy architecture.
7. Make every result traceable to a model and dataset version.
8. Treat uncertainty as a first-class result.
9. Use human approval for model promotion.
10. Be transparent that the data are observational.
11. Do not call the system autonomous unless autonomy is precisely defined.
12. Do not claim that adding Gemini makes the ML advanced.
13. The causal design, temporal validation and uncertainty framework are the principal research contributions.
14. The application is the reproducible delivery mechanism for the research.
15. Every advanced component must answer a specific research or operational need.
