from pharma_access_validation.loaders import load_bundle
from pharma_access_validation.graph_validation import validate_graph
from pharma_access_validation.dowhy_validation import run_dowhy
from pharma_access_validation.refutations import run_refutations

def test_required_refuters_are_reported(bundle):
    _,f,_=load_bundle(bundle);_,g=validate_graph(bundle);m,e,_=run_dowhy(bundle,f,g);identified=m.identify_effect();r=run_refutations(m,identified,e);assert {x["refuter"] for x in r}=={"placebo_treatment_refuter","random_common_cause","data_subset_refuter","bootstrap_refuter"}
