import json,platform,sys
from importlib.metadata import version
from . import NOTICE
from .hashing import write_hashes

def write_reports(root,formula,dowhy,refutations,econml,dag,manifest):
    root.mkdir(parents=True,exist_ok=True);independent={"notice":NOTICE,"mode":"IndependentImplementationValidation","dowhyEstimate":dowhy["estimate"],"econmlAte":econml["ate"],"warning":"Different estimands are incomparable; agreement is not identification proof."};validation={"contractVersion":"1.1","validationRunCode":manifest.validation_run_code,"synthetic":True,"notice":NOTICE,"formulaParityPassed":formula["passed"]}
    docs={"formula_parity.json":formula,"independent_estimator_comparison.json":independent,"dowhy_identification.json":dowhy,"dowhy_refutations.json":{"diagnostics":refutations},"econml_results.json":econml,"dag_parity.json":dag,"adjustment_set_parity.json":{"status":dag["status"],"variables":dag["adjustmentVariables"]},"environment_manifest.json":{"python":sys.version,"platform":platform.platform(),"packages":{x:version(x) for x in ["dowhy","econml","pandas","numpy","scikit-learn","statsmodels"]}},"validation_manifest.json":validation}
    for name,value in docs.items():(root/name).write_text(json.dumps(value,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    (root/"validation_summary.md").write_text(f"# Milestone 7 validation summary\n\n> **{NOTICE}**\n\nFormula parity: {'passed' if formula['passed'] else 'failed'}. DoWhy identification and refutation outputs are diagnostics under the exported graph. EconML is independently fitted. Agreement does not prove causal identification.\n",encoding="utf-8")
    names=list(docs)+["validation_summary.md"];write_hashes(root,names);return names+["file_hashes.json"]
