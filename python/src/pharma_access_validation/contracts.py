from __future__ import annotations
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field, StrictStr, model_validator

class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid", populate_by_name=True)

class Window(StrictModel):
    start: int
    end: int

class Seeds(StrictModel):
    nuisance: int
    folds: int

class StudyManifest(StrictModel):
    contract_version: str = Field(alias="contractVersion")
    validation_run_code: str = Field(alias="validationRunCode")
    causal_study_id: int = Field(alias="causalStudyId")
    causal_study_code: str = Field(alias="causalStudyCode")
    dataset_version: str = Field(alias="datasetVersion")
    feature_set_version: str = Field(alias="featureSetVersion")
    dag_version: str = Field(alias="dagVersion")
    adjustment_set_version: str = Field(alias="adjustmentSetVersion")
    treatment_definition_version: str = Field(alias="treatmentDefinitionVersion")
    outcome_definition_version: str = Field(alias="outcomeDefinitionVersion")
    estimand: str
    effect_scale: str = Field(alias="effectScale")
    observation_window: Window = Field(alias="observationWindow")
    row_count: int = Field(alias="rowCount")
    treated_count: int = Field(alias="treatedCount")
    control_count: int = Field(alias="controlCount")
    excluded_row_counts: dict[str, int] = Field(alias="excludedRowCounts")
    random_seeds: Seeds = Field(alias="randomSeeds")
    numeric_precision_policy: str = Field(alias="numericPrecisionPolicy")
    source_code_commit: str = Field(alias="sourceCodeCommit")
    generated_timestamp_utc: str = Field(alias="generatedTimestampUtc")
    synthetic: bool

    @model_validator(mode="after")
    def supported(self):
        if self.contract_version != "1.1": raise ValueError("unsupported contract version")
        if not self.synthetic: raise ValueError("Milestone 7 automation accepts synthetic data only")
        if self.row_count != self.treated_count + self.control_count: raise ValueError("sample counts disagree")
        return self

class HashManifest(StrictModel):
    contract_version: str = Field(alias="contractVersion")
    files: dict[str, str]
    reproducibility_hash: str = Field(alias="reproducibilityHash")

EstimatorIdentifier = Literal["UnadjustedDifferenceInMeans", "PropensityScoreWeighting", "OutcomeRegression", "AugmentedInverseProbabilityWeighting"]
EstimandIdentifier = Literal["ATE", "ATT"]
EffectScaleIdentifier = Literal["RiskDifference", "RiskRatio", "OddsRatio"]
REQUIRED_ESTIMATORS = {"UnadjustedDifferenceInMeans", "PropensityScoreWeighting", "OutcomeRegression", "AugmentedInverseProbabilityWeighting"}

class CausalEstimate(StrictModel):
    estimator: EstimatorIdentifier
    estimand: EstimandIdentifier
    effect_scale: EffectScaleIdentifier = Field(alias="effectScale")
    estimate: float
    standard_error: float | None = Field(alias="standardError")
    confidence_lower: float | None = Field(alias="confidenceLower")
    confidence_upper: float | None = Field(alias="confidenceUpper")
    treated_count: int = Field(alias="treatedCount")
    control_count: int = Field(alias="controlCount")
    effective_sample_size: float | None = Field(alias="effectiveSampleSize")

class CSharpEstimatesDocument(StrictModel):
    contract_version: StrictStr = Field(alias="contractVersion")
    estimand: EstimandIdentifier
    effect_scale: EffectScaleIdentifier = Field(alias="effectScale")
    estimates: list[CausalEstimate]

    @model_validator(mode="after")
    def validate_exchange(self):
        if self.contract_version != "1.1": raise ValueError("unsupported C# estimate contract version")
        identifiers = [x.estimator for x in self.estimates]
        duplicates = sorted({x for x in identifiers if identifiers.count(x) > 1})
        if duplicates: raise ValueError(f"duplicate estimator identifiers: {duplicates}")
        missing = sorted(REQUIRED_ESTIMATORS - set(identifiers))
        if missing: raise ValueError(f"missing required estimators: {missing}")
        if any(x.estimand != self.estimand or x.effect_scale != self.effect_scale for x in self.estimates):
            raise ValueError("estimate lineage disagrees with document estimand or effect scale")
        return self
