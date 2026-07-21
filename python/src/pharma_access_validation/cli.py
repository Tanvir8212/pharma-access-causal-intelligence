import argparse,json,shutil,sys
from pathlib import Path
from .loaders import load_bundle,validate_folds
from .graph_validation import validate_graph
from .parity import formula_parity
from .dowhy_validation import run_dowhy
from .refutations import run_refutations
from .econml_validation import run_econml
from .reporting import write_reports

def run(bundle:Path,reports:Path):
    manifest,frame,nuisance=load_bundle(bundle);dag,graph=validate_graph(bundle);folds=validate_folds(bundle,frame);csharp=json.loads((bundle/"csharp_estimates.json").read_text("utf-8"));formula=formula_parity(frame,nuisance,csharp)
    model,estimate,dowhy=run_dowhy(bundle,frame,graph);identified=model.identify_effect(proceed_when_unidentifiable=False);refs=run_refutations(model,identified,estimate,manifest.random_seeds.nuisance);econml=run_econml(frame,dag["adjustmentVariables"],folds,manifest.random_seeds.folds);write_reports(reports,formula,dowhy,refs,econml,dag,manifest)
    for name in ["python_estimates.json","dowhy_identification.json","dowhy_refutations.json","econml_estimates.json","parity_report.json","validation_summary.md","validation_manifest.json"]:
        source={"python_estimates.json":"independent_estimator_comparison.json","econml_estimates.json":"econml_results.json","parity_report.json":"formula_parity.json"}.get(name,name);shutil.copyfile(reports/source,bundle/name)
    return 0 if formula["passed"] else 3

def main(argv=None):
    p=argparse.ArgumentParser();p.add_argument("--bundle",type=Path,required=True);p.add_argument("--reports",type=Path,required=True);a=p.parse_args(argv)
    try:return run(a.bundle,a.reports)
    except Exception as exc:print(f"validation failed: {exc}",file=sys.stderr);return 2

if __name__=="__main__":raise SystemExit(main())
