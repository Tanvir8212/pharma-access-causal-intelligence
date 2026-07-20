You are the principal software architect, senior .NET engineer, machine-learning engineer, causal-inference engineer, data engineer, research software engineer, test engineer and technical writer for a new project called **PharmaAccess Causal Intelligence**.

You are working inside a newly created Git repository. Treat the repository as a completely separate project from any previous repository.

Your responsibility is to build a production-quality, research-reproducible application incrementally. Do not create a superficial demo, placeholder-heavy prototype or generic chatbot wrapper.

The project must use:

* ASP.NET Core and C# as the primary application platform.
* SQL Server as the primary database.
* ML.NET as the primary production predictive modeling framework.
* Python as a separate research-validation environment for DoWhy, EconML, statistical validation and publication analysis.
* Gemini through a provider-agnostic language-model abstraction.
* A drug–state–quarter longitudinal dataset.
* Explicit separation between predictive machine learning and causal inference.
* Temporal grouped validation.
* Counterfactual simulation with uncertainty.
* Governed continual learning through champion–challenger evaluation.
* Complete model, dataset and experiment lineage.
* Automated tests.
* Documentation suitable for a publishable research project.

The working application name is:

`PharmaAccess AI`

The repository name is:

`pharma-access-causal-intelligence`

The proposed paper title is:

`From Approval to Access: A Causal Machine-Learning and Counterfactual Simulation Framework for Geographic Inequality in U.S. Generic Drug Adoption`

The system will reuse or rederive data from public FDA first-generic approval data and CMS Medicaid State Drug Utilization Data. It may later import cleaned data from an earlier research project, but this repository must have new architecture, new unit of analysis, new targets, new experiments and a new research contribution.

The earlier project used launch-level Fast/Medium/Slow classification. This project must not recreate that task as its main contribution.

The primary unit of analysis in this project is:

`Drug × State × Quarter`

The main predictive tasks are:

1. Predict whether a generic drug enters a state in the next quarter.
2. Predict time to state entry.
3. Predict Q4 Numeric Distribution using information available by a declared early cutoff.
4. Predict Q4 Weighted Distribution.
5. Predict Q4 Access Gap.
6. Predict persistent access-inequality risk.

The main causal task is:

Estimate, under explicit observational causal assumptions, the effect of broader early geographic coverage on later access.

The primary treatment should initially be a configurable binary treatment based on Q1 Numeric Distribution.

The primary causal outcome should initially be Q4 Numeric Distribution.

Q4 Access Gap should be a secondary causal outcome.

The initial primary estimand should be Average Treatment Effect.

The primary initial estimator should be Augmented Inverse Probability Weighting.

Additional estimators should include:

* Outcome regression.
* Inverse Probability of Treatment Weighting.
* Matching as a sensitivity method.
* Python-based Double Machine Learning.
* Python-based causal forest or comparable heterogeneous-effect estimator.

Do not claim that observational estimates prove causation.

Use phrases such as:

* estimated effect under stated assumptions
* observational causal estimate
* counterfactual simulation
* measured-confounder adjustment
* potentially sensitive to unmeasured confounding

Never use phrases such as:

* the model proves
* this intervention definitely causes
* the drug was unavailable because of
* this is a clinical recommendation

## Mandatory working method

Do not attempt to generate the whole application in one uncontrolled change.

Work in phases.

For every phase:

1. Inspect the existing repository.
2. State the specific files and projects that will be created or modified.
3. Implement the smallest coherent vertical slice.
4. Build the solution.
5. Run relevant tests.
6. Fix compilation failures and test failures.
7. Summarize exactly what was completed.
8. List unresolved issues without hiding them.
9. Update documentation.
10. Commit-ready code only.

Do not silently skip a required component.

Do not replace real implementations with empty methods, fake services or `TODO` comments unless the current phase explicitly documents the item as future work.

Do not create hundreds of files before the architecture is validated.

Prefer compilable, testable increments.

Use current stable package versions compatible with the installed stable .NET SDK. Do not use preview packages unless a required capability has no stable alternative, and document any preview dependency.

Pin NuGet and Python dependency versions once selected.

Use nullable reference types.

Use asynchronous APIs for I/O.

Propagate `CancellationToken`.

Use dependency injection.

Use structured logging.

Do not expose database entities directly through public API contracts.

Do not hard-code secrets.

Do not commit API keys or connection strings.

## First action

Before writing application code:

1. Inspect installed tools and versions:

   * `dotnet --info`
   * Git version
   * Python version if installed
   * SQL tooling availability if discoverable
2. Inspect the repository contents.
3. Create an implementation-status report.
4. Create `docs/PROJECT_BLUEPRINT.md`.
5. Place the full project blueprint in that file using the specification contained in this prompt.
6. Create `docs/IMPLEMENTATION_PLAN.md`.
7. Create `docs/DECISIONS/` for Architecture Decision Records.
8. Create `docs/IMPLEMENTATION_STATUS.md`.
9. Create a root `AGENTS.md` containing repository-specific instructions for future coding agents.
10. Do not begin advanced ML or Gemini integration until the foundational solution builds.

## Required repository structure

Create an initial structure similar to:

```text
src/
  PharmaAccess.Domain/
  PharmaAccess.Application/
  PharmaAccess.Infrastructure/
  PharmaAccess.Data/
  PharmaAccess.ML/
  PharmaAccess.Causal/
  PharmaAccess.Llm/
  PharmaAccess.Worker/
  PharmaAccess.Api/
  PharmaAccess.Web/

tests/
  PharmaAccess.Domain.Tests/
  PharmaAccess.Application.Tests/
  PharmaAccess.Data.Tests/
  PharmaAccess.ML.Tests/
  PharmaAccess.Causal.Tests/
  PharmaAccess.Llm.Tests/
  PharmaAccess.Api.IntegrationTests/

database/
  schemas/
  tables/
  views/
  procedures/
  validation/
  seed/
  migrations/

research/
  python/
  notebooks/
  causal/
    dag/
    definitions/
  experiments/
  figures/
  tables/
  paper/
  snapshots/

docs/
  DECISIONS/
  architecture/
  data/
  models/
  causal/
  llm/
  operations/
  research/

samples/
  data/

artifacts/
  models/
  experiments/
  reports/
```

Do not commit large raw datasets or generated model artifacts unless Git LFS and repository policy are explicitly configured.

Create `.gitignore` entries for:

* build output
* local secrets
* environment files
* generated models
* large data files
* Python virtual environments
* notebooks checkpoints
* local database files
* logs
* temporary reports

Create `.editorconfig`.

Create `Directory.Build.props`.

Consider central package management through `Directory.Packages.props`.

## Solution architecture

Use Clean Architecture or a carefully justified equivalent.

### Domain project

Contains pure domain concepts and must not depend on:

* Entity Framework Core
* ML.NET
* Gemini
* ASP.NET Core
* SQL Server-specific classes

Domain entities should include or prepare for:

* Drug
* DrugProduct
* FirstGenericApproval
* State
* CalendarQuarter
* UtilizationObservation
* DatasetVersion
* FeatureSetVersion
* Experiment
* ModelVersion
* Prediction
* CausalAnalysisDefinition
* CausalEstimate
* CounterfactualScenario
* DriftReport
* ResearchDocument
* LlmInteraction

Create strongly typed identifiers where they improve correctness.

Create domain value objects for:

* StateCode
* Quarter
* Percentage
* DatasetVersionId
* ModelVersionId

Do not overengineer value objects if they damage EF or serialization unnecessarily. Document the decision.

### Application project

Contains use cases, commands, queries, DTOs, validators and interfaces.

Required abstractions should eventually include:

```csharp
public interface IDatasetVersionService
{
}

public interface IDataQualityService
{
}

public interface IFeatureGenerationService
{
}

public interface IModelTrainingService
{
}

public interface IModelRegistry
{
}

public interface IPredictionService
{
}

public interface ICausalEstimationService
{
}

public interface ICounterfactualSimulationService
{
}

public interface IDriftDetectionService
{
}

public interface IModelPromotionService
{
}

public interface ILanguageModelClient
{
}

public interface IRetrievalService
{
}

public interface ILlmResponseValidator
{
}
```

Do not leave them empty once their phase is implemented.

### Data project

Contains:

* EF Core DbContext for metadata and application data.
* Entity configurations.
* repositories where useful.
* migrations.
* query services.
* Dapper analytical queries if justified.
* dataset-version persistence.
* SQL Server implementation.

Do not put business logic into EF entity configuration.

### Infrastructure project

Contains:

* filesystem artifact storage.
* hashing.
* clocks.
* external-data adapters.
* configuration.
* logging helpers.
* provider implementations not specific to ML or Gemini.

### ML project

Contains:

* feature schemas.
* training pipelines.
* splitters.
* trainers.
* evaluators.
* calibration.
* model artifact serialization.
* baseline models.
* ensemble logic.
* model-card generation.
* leakage checks.

### Causal project

Contains:

* treatment definitions.
* outcome definitions.
* causal cohort construction.
* propensity models.
* overlap diagnostics.
* covariate-balance diagnostics.
* IPTW.
* outcome regression.
* AIPW.
* bootstrap inference.
* counterfactual calculations.
* extrapolation detection.
* synthetic causal data generators for tests.

### LLM project

Contains:

* provider-agnostic contracts.
* Gemini implementation.
* structured output schemas.
* prompt templates.
* prompt versions.
* RAG retrieval.
* citation handling.
* numerical-fidelity validation.
* forbidden-claim detection.
* deterministic fallback explanations.

### Worker project

Contains background jobs for:

* data ingestion.
* validation.
* feature generation.
* candidate training.
* drift analysis.
* report generation.
* optional scheduled RAG ingestion.

### API project

Contains versioned REST endpoints.

### Web project

Use Blazor Server or another ASP.NET Core UI approach that fits the installed environment.

Prefer an architecture that lets the application remain easy to run locally.

Do not create a JavaScript-heavy frontend unless clearly justified.

## Database schemas

Prepare SQL Server schemas:

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

The migrations or SQL scripts must be idempotent where possible.

The application must never mutate files in the raw data zone.

## Required database entities

Implement or prepare migrations for the following.

### `core.Drug`

* DrugId
* NormalizedIngredient
* IngredientCombination
* DosageForm
* Route
* Strength
* RxNormId
* TherapeuticClass
* CreatedAt
* UpdatedAt

### `core.DrugProduct`

* DrugProductId
* DrugId
* OriginalNdc
* NormalizedNdc
* ProductName
* Labeler
* SourceSystem
* MappingConfidence

### `core.FirstGenericApproval`

* ApprovalId
* DrugId
* ApprovalDate
* ApplicationNumber
* Applicant
* ApprovalSource
* IsPrimaryLaunchReference

### `core.State`

* StateId
* StateCode
* StateName
* Region
* Division
* IsEligible
* ExclusionReason

### `core.CalendarQuarter`

* QuarterId
* CalendarYear
* QuarterNumber
* QuarterStartDate
* QuarterEndDate

### `core.StateDrugUtilization`

* StateDrugUtilizationId
* DrugId
* DrugProductId
* StateId
* QuarterId
* PrescriptionCount
* ReimbursementAmount
* SourceRowCount
* IsSuppressed
* DataQualityStatus
* DatasetVersionId

### `feature.DrugStateQuarter`

Prepare a model or table with:

* DrugId
* StateId
* QuarterId
* ApprovalQuarterId
* QuarterSinceApproval
* ObservedPrescriptionCount
* ObservedReimbursement
* IsPresent
* IsFirstEntryQuarter
* PreviousQuarterPrescriptionCount
* PreviousQuarterReimbursement
* StateHistoricalGenericEntryRate
* StateHistoricalMedianEntryDelay
* StateHistoricalMarketWeight
* RegionHistoricalEntryRate
* NationalStateCoveragePreviousQuarter
* NationalPrescriptionCountPreviousQuarter
* NationalReimbursementPreviousQuarter
* PreviousQuarterND
* PreviousQuarterWD
* PreviousQuarterAccessGap
* StateNeighborAdoptionRate
* StateSimilarityAdoptionRate
* LaunchCohort
* TherapeuticClass
* LabelNextQuarterEntry
* LabelFuturePrescriptionCount
* LabelQ4ND
* LabelQ4WD
* LabelQ4AccessGap
* LabelPersistentInequality
* FeatureVersion
* AvailableAsOfQuarter

### `feature.LaunchQuarterSummary`

* DrugId
* QuarterSinceApproval
* ActiveStateCount
* EligibleStateCount
* NumericDistribution
* WeightedDistribution
* AccessGap
* TotalPrescriptions
* TotalReimbursement
* ConcentrationIndex
* TopStateShare
* TopFiveStateShare
* RegionalCoverageCount

### `ml.Experiment`

* ExperimentId
* ExperimentName
* ResearchQuestion
* DatasetVersionId
* FeatureVersion
* CodeCommitHash
* ConfigurationJson
* StartedAt
* CompletedAt
* Status
* RandomSeed
* Notes

### `ml.ModelVersion`

* ModelVersionId
* ExperimentId
* ModelName
* TaskType
* Algorithm
* HyperparametersJson
* ArtifactPath
* ArtifactHash
* TrainingPeriodStart
* TrainingPeriodEnd
* ValidationPeriodStart
* ValidationPeriodEnd
* TestPeriodStart
* TestPeriodEnd
* MetricsJson
* CalibrationMetricsJson
* SubgroupMetricsJson
* IsChampion
* CreatedAt

### `ml.Prediction`

* PredictionId
* ModelVersionId
* DrugId
* StateId
* QuarterId
* PredictionType
* PredictedValue
* Probability
* LowerBound
* UpperBound
* ActualValue
* CreatedAt

### `ml.DriftReport`

* DriftReportId
* ModelVersionId
* DatasetVersionId
* FeatureDriftJson
* PredictionDriftJson
* PerformanceDriftJson
* SubgroupDriftJson
* Decision
* CreatedAt

### `causal.AnalysisDefinition`

* CausalAnalysisId
* Name
* TreatmentDefinition
* OutcomeDefinition
* ConfoundersJson
* EffectModifiersJson
* EligibilityCriteriaJson
* DagVersion
* CreatedAt

### `causal.Estimate`

* CausalEstimateId
* CausalAnalysisId
* DatasetVersionId
* Estimator
* Population
* AverageTreatmentEffect
* StandardError
* LowerConfidenceBound
* UpperConfidenceBound
* DiagnosticsJson
* AssumptionsJson
* CreatedAt

### `causal.CounterfactualScenario`

* ScenarioId
* DrugId
* ScenarioName
* BaselineJson
* InterventionJson
* ModelVersionId
* CausalEstimateId
* PredictedOutcome
* LowerBound
* UpperBound
* CreatedAt

### `rag.Document`

* DocumentId
* Title
* SourceOrganization
* SourceUrl
* PublicationDate
* RetrievalDate
* DocumentHash
* DocumentType
* TrustLevel

### `rag.DocumentChunk`

* ChunkId
* DocumentId
* ChunkIndex
* ChunkText
* Embedding or provider-neutral vector representation
* MetadataJson

### `rag.LlmInteraction`

* InteractionId
* PromptVersion
* ModelName
* Question
* StructuredContextJson
* RetrievedChunkIdsJson
* Response
* ValidationStatus
* UnsupportedClaimCount
* LatencyMs
* TokenUsageJson
* CreatedAt

## Metric definitions

Implement Numeric Distribution as:

```text
ND = active eligible states / eligible states × 100
```

Implement Weighted Distribution as:

```text
WD = sum of frozen baseline state weights for active states
     / sum of frozen baseline state weights for all eligible states
     × 100
```

Implement:

```text
Access Gap = WD − ND
```

The weight reference must use only information available before the outcome.

Create configuration options for:

* eligible states
* state-entry threshold
* weighting reference
* minimum prescription threshold
* suppression treatment
* launch-quarter calculation
* Q1 and Q4 definitions

Create unit tests with manually calculated examples.

## Data ingestion requirements

Build the ingestion pipeline in independent adapters.

Required capabilities:

* Import FDA first-generic data from a configured local source file.
* Import CMS Medicaid SDUD data from configured local source files.
* Preserve original source fields.
* Calculate file hashes.
* Record retrieval or import metadata.
* reject malformed files.
* write staging data.
* normalize NDC.
* normalize drug names.
* map drug products.
* preserve unmatched rows.
* generate data-quality reports.
* create an immutable dataset version.

Do not implement uncontrolled web scraping in the first phase.

Use local sample data for automated tests.

Create import contracts so official download automation can be added later.

## Data-quality rules

Implement validation for:

* duplicate source rows
* duplicate drug-state-quarter rows
* invalid state codes
* invalid year or quarter
* negative values
* missing NDC
* malformed NDC
* inconsistent drug mapping
* utilization before approval beyond a configured tolerance
* schema drift
* row-count anomaly
* null-rate anomaly
* missing eligible states
* impossible quarter order
* label leakage

Support severities:

```text
Info
Warning
Error
Blocking
```

Blocking errors must stop feature generation and training.

## Feature-generation requirements

Every feature must have:

* name
* description
* data type
* source
* formula
* available-as-of rule
* missing-value policy
* feature version

Create a machine-readable feature dictionary.

Feature generation must be deterministic.

Calculate:

* quarter since approval
* previous-quarter utilization
* previous-quarter national coverage
* previous-quarter ND
* previous-quarter WD
* previous-quarter Access Gap
* historical state entry rate
* historical state median entry delay
* historical state market weight
* regional prior adoption rate
* concentration index
* top-state share
* top-five-state share
* neighbor adoption rate
* similar-state adoption rate
* launch cohort
* therapeutic category where available
* missingness indicators

All historical aggregates must be calculated using only past data relative to the row’s `AvailableAsOfQuarter`.

## Labels

Create explicit label builders.

### Next-quarter entry

Only include state-drug rows where the state has not yet entered.

Label positive if the state meets the configured entry rule in the next quarter.

Handle unobservable future quarters as censored or excluded.

### Q4 ND

Use only launches with complete Q4 observation.

### Q4 WD

Use only launches with complete Q4 observation.

### Q4 Access Gap

Use Q4 WD minus Q4 ND.

### Persistent inequality

Use a configurable prespecified threshold.

Do not select the threshold based on test-set performance.

## Leakage prevention

Create an automated leakage-audit service.

It must detect:

* future feature timestamps
* outcome-derived feature names
* same observation in multiple partitions
* unintended DrugId overlap
* data preprocessing fitted on test data
* future cohort information
* label-derived aggregate values

Training must fail on a blocking leakage finding.

## Dataset splitting

Implement:

1. temporal launch-cohort split
2. grouped split
3. rolling-origin evaluation
4. optional unseen-drug evaluation
5. optional leave-one-cohort-out evaluation

Do not use random row split as the primary result.

If a random split is included, label it as an optimistic comparison and never call it the main evaluation.

Use configuration files to define:

* train range
* validation range
* test range
* grouping rule
* random seed

Never tune hyperparameters on the test set.

## Predictive models

For next-quarter state entry implement:

* prevalence baseline
* historical state-rate baseline
* logistic regression
* FastTree
* FastForest
* LightGBM

Evaluate:

* ROC AUC
* PR AUC
* log loss
* Brier score
* precision
* recall
* F1
* threshold metrics
* calibration error
* metrics by cohort
* metrics by region
* metrics by therapeutic class

For Q4 ND, WD and Access Gap implement:

* mean baseline
* previous-value baseline where valid
* linear regression
* FastTree regression
* FastForest regression
* LightGBM regression

Evaluate:

* MAE
* RMSE
* R²
* median absolute error
* subgroup MAE
* temporal-fold variability
* interval coverage once uncertainty is implemented

Create a common trainer contract.

Create experiment configuration classes.

Persist every trial and result.

## Hyperparameter tuning

Use bounded, reproducible search.

For each algorithm:

* define search ranges
* use validation data only
* record every trial
* use a fixed seed
* record duration
* record failure
* record selected parameters

Do not add uncontrolled AutoML until baseline pipelines are correct.

If AutoML is later added, treat it as a controlled challenger.

## Ensemble

Only create an ensemble after individual models work.

The ensemble must:

* use validation predictions
* learn nonnegative weights
* normalize weights
* avoid test data
* record component model versions
* compare against the strongest single model
* remain optional if it does not improve validation

## Calibration

For binary prediction implement:

* calibration bins
* reliability data
* Brier score
* Expected Calibration Error
* calibration slope and intercept where practical
* subgroup calibration

Provide a calibrated-prediction wrapper if calibration improves validation.

## Explainability

Implement:

* permutation feature importance
* model-level feature importance where available
* local perturbation-based explanation
* stability across temporal folds
* subgroup feature importance

Always label feature importance as predictive, not causal.

## Model registry

Every trained model must be registered.

Model artifact metadata must include:

* model version
* experiment
* algorithm
* hyperparameters
* feature version
* dataset version
* commit hash
* training period
* validation period
* test period
* metrics
* artifact path
* SHA-256 hash
* approval status

The application must verify artifact hashes before loading.

## Causal-analysis implementation

Create explicit classes for:

```text
PopulationDefinition
TreatmentDefinition
OutcomeDefinition
ConfounderDefinition
EffectModifierDefinition
CausalAnalysisDefinition
```

The initial primary analysis is:

* population: eligible launches with complete Q1 and Q4 observation
* treatment: high versus low Q1 Numeric Distribution
* outcome: Q4 Numeric Distribution
* estimand: Average Treatment Effect
* primary estimator: AIPW
* secondary outcome: Q4 Access Gap

The treatment threshold must be configurable and recorded.

Create a causal DAG document in Mermaid and DOT formats.

Document all assumed arrows and unmeasured variables.

## Propensity model

Implement:

* logistic propensity model as primary
* optional LightGBM sensitivity model
* probability clipping as a configurable sensitivity option
* overlap plots or plot-ready data
* common support check
* positivity warnings
* effective sample size

## Balance diagnostics

Calculate before and after adjustment:

* standardized mean difference
* variance ratio
* weighted means
* maximum absolute imbalance
* effective sample size

Export Love-plot-ready data.

Flag residual imbalance.

## Estimators

Implement and test:

### Unadjusted difference

Descriptive only.

### Outcome regression

Predict potential outcomes under treatment and control.

### IPTW

Implement stabilized and unstabilized options.

Detect extreme weights.

### AIPW

Implement a doubly robust estimator.

Use cross-fitting where feasible.

### Matching

Add later as a sensitivity method.

Every estimator must return:

* estimate
* standard error
* confidence interval
* sample size
* effective sample size
* overlap result
* diagnostics
* assumptions
* warnings

## Causal uncertainty

Implement drug-clustered bootstrap where appropriate.

Allow configured bootstrap repetitions and seed.

Use a small repetition count in tests and a larger configured count in research runs.

## Synthetic causal tests

Create synthetic datasets with known treatment effects.

Test that:

* unadjusted estimates are biased under confounding
* propensity adjustment reduces bias
* AIPW recovers the known effect within tolerance
* positivity warnings trigger
* extreme weights are detected
* invalid posttreatment confounders are rejected where identifiable

## Python research environment

Create:

```text
research/python/pyproject.toml
research/python/README.md
research/python/src/
research/python/tests/
```

Use a virtual environment.

Pin dependencies after selection.

Prepare modules for:

* loading exported research snapshots
* cohort validation
* descriptive statistics
* DoWhy analysis
* EconML DML
* heterogeneous treatment effects
* causal forest
* refutation tests
* balance plots
* publication figures
* result export

Define a strict JSON schema for Python results imported into .NET.

Do not make the ASP.NET request path depend on live notebook execution.

## Counterfactual simulator

Create a scenario request containing:

* DrugId
* baseline cutoff
* treatment definition
* observed treatment
* requested intervention
* outcome
* estimator
* model version
* dataset version

Validate:

* intervention range
* observed support
* overlap
* eligibility
* completeness
* model compatibility

Return:

* baseline expected outcome
* counterfactual expected outcome
* estimated change
* confidence interval
* extrapolation flag
* overlap status
* assumptions
* warnings
* model version
* dataset version
* causal estimate version

Persist every scenario.

Do not let Gemini produce these values.

## Governed continual learning

Implement a workflow with:

1. data ingestion
2. validation
3. dataset versioning
4. feature generation
5. champion scoring
6. drift analysis
7. challenger training
8. temporal evaluation
9. calibration comparison
10. subgroup comparison
11. promotion recommendation
12. human approval
13. promotion or rejection
14. rollback support

Create a configurable promotion policy.

The default policy should require:

* improvement in primary metric by a configurable minimum
* no material calibration degradation
* no material worst-group degradation
* no blocking quality issue
* no leakage failure
* valid artifact hash
* reproducible run

Do not automatically promote models in the initial implementation.

## Drift detection

Implement or prepare:

* Population Stability Index
* Jensen–Shannon divergence
* Kolmogorov–Smirnov statistic
* missingness drift
* category-frequency drift
* prediction drift
* performance drift
* subgroup drift

Drift should create a report and recommendation.

Drift alone must not automatically trigger promotion.

## Gemini provider abstraction

Create:

```csharp
public interface ILanguageModelClient
{
    Task<StructuredLlmResponse<TResponse>> GenerateStructuredAsync<TResponse>(
        LlmRequest request,
        CancellationToken cancellationToken);
}
```

Do not make domain or application layers depend on Gemini-specific types.

Use `HttpClientFactory`.

Use resilience policies for:

* timeout
* transient failure
* rate limiting
* retry with safe limits
* circuit breaker where appropriate

Store the API key in user secrets or environment variables.

Create `.env.example` or configuration documentation without a real key.

## Gemini responsibilities

Gemini may:

* explain predictions
* explain causal estimates
* compare baseline and counterfactual scenarios
* explain uncertainty
* answer methodology questions using RAG
* generate structured summaries

Gemini may not:

* calculate predictions
* calculate causal estimates
* execute arbitrary SQL
* promote models
* change configuration
* invent values
* provide clinical recommendations
* claim proven causality

## Structured LLM context

Only provide verified values.

Include:

* analysis type
* drug metadata
* baseline
* prediction
* counterfactual
* confidence intervals
* model version
* dataset version
* estimator
* overlap status
* assumptions
* limitations
* retrieved evidence

## Structured LLM output

Create schemas containing:

* summary
* key findings
* uncertainty explanation
* assumptions
* limitations
* evidence references
* unsupported-claim indicator

Validate every response.

## LLM response validation

Implement:

* JSON schema validation
* numerical-value comparison
* citation-ID validation
* forbidden-phrase detection
* clinical-advice detection
* causal-overclaim detection
* maximum-length validation

If validation fails:

* reject the response
* log the reason
* use a deterministic template fallback

## RAG

Create provider-neutral interfaces:

```csharp
public interface IDocumentIngestionService
{
}

public interface IEmbeddingService
{
}

public interface IRetrievalService
{
}
```

Trusted sources should include:

* official FDA documentation
* official CMS documentation
* project data dictionary
* project model cards
* causal definitions
* selected peer-reviewed methodology references

Store:

* document hash
* source
* retrieval date
* publication date
* trust level
* chunks
* embeddings
* metadata

Use metadata filtering and semantic retrieval.

Never allow document text to override system rules or tool permissions.

## LLM benchmark

Create a fixed test dataset containing:

* metric explanation questions
* prediction explanation questions
* causal explanation questions
* limitation questions
* numerical traps
* unsupported clinical questions
* prompt injection attempts
* citation questions
* ambiguous questions

Measure:

* structured-output validity
* numerical faithfulness
* unsupported-claim rate
* citation precision
* correct-refusal rate
* latency
* provider cost if available

## API endpoints

Implement versioned APIs gradually.

Plan for:

```text
GET /api/v1/drugs
GET /api/v1/drugs/{drugId}
GET /api/v1/drugs/{drugId}/trajectory
GET /api/v1/drugs/{drugId}/similar-launches

GET /api/v1/states
GET /api/v1/states/{stateCode}

POST /api/v1/predictions/next-state-entry
POST /api/v1/predictions/q4-access
GET /api/v1/predictions/{predictionId}

POST /api/v1/counterfactuals
GET /api/v1/counterfactuals/{scenarioId}

GET /api/v1/causal/analyses
GET /api/v1/causal/analyses/{id}
GET /api/v1/causal/analyses/{id}/diagnostics

POST /api/v1/explanations/prediction
POST /api/v1/explanations/counterfactual
POST /api/v1/explanations/methodology

GET /api/v1/models
GET /api/v1/models/champion
GET /api/v1/models/{modelVersionId}

POST /api/v1/admin/data/import
POST /api/v1/admin/training/run
POST /api/v1/admin/models/{id}/approve
POST /api/v1/admin/models/{id}/reject
POST /api/v1/admin/models/{id}/rollback
GET /api/v1/admin/drift
```

Use request validation and problem-details responses.

Do not expose internal database IDs or exceptions unnecessarily.

## UI pages

Plan and implement progressively:

### Dashboard

* eligible launches
* drugs
* states
* latest dataset
* champion model
* drift status
* quality warnings

### Drug explorer

* approval
* quarterly ND
* quarterly WD
* Access Gap
* state entry
* predictions
* uncertainty
* similar launches

### State explorer

* historical readiness
* entry speed
* market weight
* model performance

### Prediction page

* selected drug
* prediction cutoff
* state-entry probabilities
* calibration
* explanation
* model version

### Counterfactual page

* treatment control
* intervention selection
* baseline comparison
* confidence interval
* overlap warning
* extrapolation warning
* grounded explanation

### Model governance page

* champion
* challengers
* metrics
* calibration
* subgroup performance
* drift
* approval
* rejection
* rollback

### Research page

* cohort flow
* experiment results
* causal diagnostics
* figures
* tables
* reproducibility manifest

## Security

Implement:

* user secrets for local development
* authorization for administrative routes
* rate limiting
* input-size limits
* secure headers
* no arbitrary SQL
* no arbitrary file paths
* no secret logging
* LLM prompt-injection defenses
* model artifact hash checking

The public-facing project may use read-only anonymous access for research pages.

Administrative functionality must require authorization.

## Observability

Use structured logging.

Include correlation IDs.

Log:

* dataset version
* feature version
* model version
* experiment ID
* prediction latency
* LLM prompt version
* retrieved document IDs
* validation status

Do not log secrets.

Prepare OpenTelemetry integration if it can be added without unnecessary complexity.

## Testing

Use xUnit.

Use FluentAssertions only if compatible and justified.

Use a mocking framework if needed.

### Required unit tests

* quarter calculation
* NDC normalization
* ND
* WD
* Access Gap
* state-entry rule
* treatment assignment
* propensity weights
* AIPW
* confidence interval
* overlap detection
* counterfactual validation
* promotion policy
* LLM numerical validation
* forbidden causal claims

### Required integration tests

* SQL migration
* import sample data
* feature generation
* model training
* model loading
* prediction endpoint
* counterfactual endpoint
* LLM fallback
* promotion workflow

### Required end-to-end sample

1. Load a small synthetic or anonymized sample.
2. generate features.
3. train a baseline model.
4. register the model.
5. mark it as champion through an approved development workflow.
6. request a prediction.
7. run a counterfactual.
8. generate a deterministic explanation.
9. verify audit records.

## CI

Create a GitHub Actions workflow that:

* restores .NET
* builds
* runs tests
* optionally checks formatting
* validates Python package installation
* runs lightweight Python tests
* does not require secrets
* does not run costly full model training

## Documentation

Required documents:

```text
README.md
AGENTS.md
docs/PROJECT_BLUEPRINT.md
docs/IMPLEMENTATION_PLAN.md
docs/IMPLEMENTATION_STATUS.md
docs/architecture/SYSTEM_ARCHITECTURE.md
docs/data/DATA_DICTIONARY.md
docs/data/DATA_PROVENANCE.md
docs/data/DATA_QUALITY.md
docs/models/MODEL_TRAINING.md
docs/models/MODEL_REGISTRY.md
docs/models/MODEL_CARD_TEMPLATE.md
docs/causal/CAUSAL_ANALYSIS_PLAN.md
docs/causal/ASSUMPTIONS.md
docs/causal/DAG.md
docs/llm/GEMINI_INTEGRATION.md
docs/llm/LLM_EVALUATION.md
docs/operations/LOCAL_SETUP.md
docs/operations/RETRAINING_AND_PROMOTION.md
docs/research/PAPER_PLAN.md
docs/research/REPRODUCIBILITY.md
SECURITY.md
CONTRIBUTING.md
```

Keep documentation synchronized with code.

## Architecture Decision Records

Create ADRs for at least:

1. .NET and Clean Architecture.
2. SQL Server.
3. ML.NET for production prediction.
4. Python for causal validation.
5. temporal grouped validation.
6. Gemini provider abstraction.
7. file- or schema-based .NET/Python exchange.
8. human-approved model promotion.
9. Blazor or selected UI framework.
10. artifact storage strategy.

## Reproducibility

Every experiment must record:

* Git commit
* dataset version
* feature version
* configuration
* random seed
* .NET version
* NuGet versions
* Python version
* Python dependency versions
* split periods
* artifact hashes

Create `research_manifest.json` for research runs.

## Model cards

Generate a model card containing:

* purpose
* intended use
* prohibited use
* dataset period
* features
* task
* algorithm
* metrics
* calibration
* subgroup performance
* uncertainty
* limitations
* drift policy
* approval history

## Data card

Create a data-card template containing:

* sources
* retrieval date
* inclusion
* exclusion
* mapping
* missingness
* suppression
* bias
* intended use
* prohibited use
* version
* hash

## Paper plan

Prepare the software to export:

* cohort table
* descriptive statistics
* model comparison
* calibration metrics
* subgroup metrics
* propensity distributions
* balance diagnostics
* causal estimates
* sensitivity analysis
* counterfactual examples
* LLM benchmark results
* reproducibility manifest

Prepare figure data for:

* architecture
* cohort flow
* DAG
* trajectories
* ND/WD/Gap
* temporal split
* ROC
* PR
* calibration
* feature importance
* propensity overlap
* Love plot
* causal forest plot
* counterfactual comparison
* drift workflow
* LLM evaluation

Do not fabricate research results.

## Ethical constraints

Document:

* Medicaid data are not the full U.S. market.
* approval date may differ from commercial launch.
* claims utilization is not identical to availability.
* state differences may have unmeasured causes.
* low utilization does not prove lack of supply.
* no patient-level or clinical recommendation is made.
* model results must not be used to blame states or manufacturers.
* observational causal estimates depend on assumptions.

## Initial milestone sequence

Follow this exact sequence unless repository inspection reveals a blocking reason.

### Milestone 0

* inspect environment
* create solution
* create repository documentation
* create buildable projects
* configure references
* configure nullable and analyzers
* add CI
* create first ADRs
* build and test

### Milestone 1

* domain model
* database context
* state and quarter dimensions
* dataset version entity
* sample configuration
* migrations
* repository smoke test

### Milestone 2

* local CSV ingestion contracts
* sample FDA input
* sample Medicaid input
* NDC normalization
* mapping
* data-quality results
* immutable dataset version
* tests

### Milestone 3

* feature schemas
* ND
* WD
* Access Gap
* drug–state–quarter feature builder
* launch-quarter summary
* labels
* leakage audit
* tests

### Milestone 4

* temporal split
* baseline models
* LightGBM
* FastTree
* FastForest
* experiment registry
* model registry
* metrics
* first prediction API
* tests

### Milestone 5

* calibration
* uncertainty
* subgroup metrics
* explainability
* model card
* error analysis

### Milestone 6

* causal analysis definitions
* DAG
* propensity
* overlap
* balance
* outcome regression
* IPTW
* AIPW
* bootstrap
* synthetic causal tests

### Milestone 7

* Python environment
* DoWhy
* EconML
* exported research snapshot
* schema-validated result import
* refutation outputs

### Milestone 8

* counterfactual scenario engine
* support checks
* confidence intervals
* API
* UI
* audit

### Milestone 9

* Gemini abstraction
* Gemini client
* structured output
* RAG
* response validation
* deterministic fallback
* LLM benchmark

### Milestone 10

* drift
* challenger training
* promotion policy
* approval UI
* rollback
* audit

### Milestone 11

* research exports
* publication figures
* tables
* frozen test evaluation
* paper scaffold
* final reproducibility package

## Current execution instruction

Begin with **Milestone 0 only**.

Do not implement later milestones yet.

For Milestone 0:

1. Inspect the environment and repository.
2. Report detected .NET, Git and Python versions.
3. Select the stable .NET target compatible with the installed SDK.
4. Create the solution and foundational projects.
5. Configure project references according to Clean Architecture.
6. Create root build configuration.
7. Create `.editorconfig`, `.gitignore`, `Directory.Build.props` and package management.
8. Create the documentation structure.
9. Create the initial ADRs.
10. Create a basic health endpoint or simplest executable proof that API and Web projects run.
11. Add one basic unit test per test project or do not create a test project until it has a meaningful first test. Avoid meaningless `Assert.True(true)` tests.
12. Add CI.
13. Run restore.
14. Run build.
15. Run tests.
16. Fix all errors.
17. Show a concise completion report with:

    * files created
    * architecture selected
    * commands run
    * build result
    * test result
    * warnings
    * next milestone
18. Update `docs/IMPLEMENTATION_STATUS.md`.

Do not ask me to manually create files that you can create.

Do not ask for the production database or Gemini key during Milestone 0.

Use safe placeholders in configuration examples.

Do not claim a task succeeded unless the command actually succeeded.

Do not continue to Milestone 1 until I explicitly request it.
