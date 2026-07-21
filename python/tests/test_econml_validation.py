import json
from pharma_access_validation.loaders import load_bundle,validate_folds
from pharma_access_validation.econml_validation import run_econml

def test_econml_is_deterministic_and_grouped(bundle):
    _,f,_=load_bundle(bundle);vars=json.loads((bundle/"adjustment_set.json").read_text())["variables"];folds=validate_folds(bundle,f);a=run_econml(f,vars,folds);b=run_econml(f,vars,folds);assert a["ate"]==b["ate"]
