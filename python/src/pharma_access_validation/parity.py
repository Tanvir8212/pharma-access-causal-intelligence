from __future__ import annotations
import numpy as np
from .estimators import unadjusted,weights,effective_sample_size,weighted_effect,aipw,aipw_contributions
from .contracts import CSharpEstimatesDocument

STATUSES=("ExactMatch","WithinTolerance","DirectionallyConsistent","MateriallyDifferent","Incomparable","Failed")
def classify(csharp,python,abs_tol=1e-9,rel_tol=1e-8,comparable=True):
    if not comparable:return "Incomparable"
    diff=abs(csharp-python)
    if diff==0:return "ExactMatch"
    if diff<=abs_tol+rel_tol*abs(csharp):return "WithinTolerance"
    if np.sign(csharp)==np.sign(python):return "DirectionallyConsistent"
    return "MateriallyDifferent"

def formula_parity(frame,nuisance,csharp_doc,abs_tol=1e-9,rel_tol=1e-8):
    contract=CSharpEstimatesDocument.model_validate(csharp_doc)
    t=nuisance.treatment.to_numpy(); y=nuisance.outcome.to_numpy(); p=nuisance.propensity_score.to_numpy(); m1=nuisance.outcome_prediction_treated.to_numpy(); m0=nuisance.outcome_prediction_control.to_numpy(); exported=nuisance.final_weight.to_numpy(); computed=weights(t,p,"ATT")
    py={"UnadjustedDifferenceInMeans":unadjusted(t,y),"PropensityScoreWeighting":weighted_effect(t,y,computed),"OutcomeRegression":float(np.mean((m1-m0)[t==1])),"AugmentedInverseProbabilityWeighting":aipw(t,y,p,m1,m0,"ATT")}
    cs={x.estimator:x.estimate for x in contract.estimates}; comparisons=[]
    for estimator,value in py.items():
        c=cs[estimator]; comparisons.append({"estimator":estimator,"estimand":"ATT","effectScale":"RiskDifference","csharpEstimate":c,"pythonEstimate":value,"absoluteDifference":abs(c-value),"relativeDifference":abs(c-value)/max(abs(c),1e-300),"tolerance":{"absolute":abs_tol,"relative":rel_tol},"status":classify(c,value,abs_tol,rel_tol),"explanation":"Formula parity reuses exported nuisance predictions.","warnings":[]})
    row_diff=np.abs(exported-computed); contrib=aipw_contributions(t,y,p,m1,m0,"ATT")
    return {"mode":"FormulaParity","comparisons":comparisons,"rowLevel":{"maxWeightAbsoluteDifference":float(row_diff.max()),"firstMismatchingRow":None if np.allclose(exported,computed,atol=abs_tol,rtol=rel_tol) else int(np.flatnonzero(~np.isclose(exported,computed,atol=abs_tol,rtol=rel_tol))[0]),"aipwContributionSum":float(contrib.sum())},"effectiveSampleSize":effective_sample_size(computed),"passed":all(x["status"] in {"ExactMatch","WithinTolerance"} for x in comparisons) and np.allclose(exported,computed,atol=abs_tol,rtol=rel_tol)}
