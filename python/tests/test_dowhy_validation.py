from pharma_access_validation.loaders import load_bundle
from pharma_access_validation.graph_validation import validate_graph
from pharma_access_validation.dowhy_validation import run_dowhy

def test_dowhy_identifies_exported_graph(bundle):
    _,f,_=load_bundle(bundle);_,g=validate_graph(bundle);_,_,r=run_dowhy(bundle,f,g);assert r["identified"]
