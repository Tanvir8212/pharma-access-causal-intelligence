import json
from pharma_access_validation.loaders import load_bundle
from pharma_access_validation.parity import formula_parity,classify

def test_formula_parity_and_status(bundle):
    _,f,n=load_bundle(bundle);r=formula_parity(f,n,json.loads((bundle/"csharp_estimates.json").read_text()));assert r["passed"];assert classify(1,1)=="ExactMatch";assert classify(1,-1)=="MateriallyDifferent";assert classify(1,2,comparable=False)=="Incomparable"
