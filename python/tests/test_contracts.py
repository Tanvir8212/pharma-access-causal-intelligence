import copy,json,pytest
from pydantic import ValidationError
from pharma_access_validation.loaders import load_bundle
from pharma_access_validation.contracts import CSharpEstimatesDocument

def test_manifest_and_cohort_contract(bundle):
    m,f,n=load_bundle(bundle);assert m.contract_version=="1.1";assert len(f)==m.row_count;assert f.feature_row_id.is_unique

def test_unsupported_contract_version(bundle):
    doc=json.loads((bundle/"study_manifest.json").read_text());doc["contractVersion"]="9"
    from pharma_access_validation.contracts import StudyManifest
    with pytest.raises(ValueError,match="unsupported"):StudyManifest.model_validate(doc)

def _estimates(bundle):
    return json.loads((bundle/"csharp_estimates.json").read_text())

def test_required_string_estimator_contract(bundle):
    parsed=CSharpEstimatesDocument.model_validate(_estimates(bundle))
    assert {x.estimator for x in parsed.estimates}=={"UnadjustedDifferenceInMeans","PropensityScoreWeighting","OutcomeRegression","AugmentedInverseProbabilityWeighting"}
    assert parsed.estimand=="ATT" and parsed.effect_scale=="RiskDifference"

@pytest.mark.parametrize(("mutation","message"),[
    (lambda d:d["estimates"].append(copy.deepcopy(d["estimates"][0])),"duplicate estimator"),
    (lambda d:d["estimates"].pop(),"missing required estimators"),
    (lambda d:d["estimates"][0].update(estimator=0),"estimator"),
    (lambda d:d.update(estimand=1),"estimand"),
    (lambda d:d.update(effectScale=0),"effectScale"),
    (lambda d:d["estimates"][0].update(estimator="UnknownEstimator"),"estimator"),
])
def test_invalid_estimate_contract_is_rejected_clearly(bundle,mutation,message):
    doc=_estimates(bundle);mutation(doc)
    with pytest.raises((ValueError,ValidationError),match=message):CSharpEstimatesDocument.model_validate(doc)
