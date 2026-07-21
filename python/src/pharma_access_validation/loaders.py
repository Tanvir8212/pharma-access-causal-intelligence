from __future__ import annotations
import json
from pathlib import Path
import numpy as np
import pandas as pd
from .contracts import StudyManifest
from .hashing import validate_hashes

REQUIRED = {"analysis_rows.csv","schema.json","study_manifest.json","dag.json","adjustment_set.json","treatment_definition.json","outcome_definition.json","csharp_estimates.json","csharp_diagnostics.json","nuisance_predictions.csv","fold_manifest.json","file_hashes.json"}

def load_bundle(root: Path):
    root = root.resolve()
    missing = REQUIRED - {p.name for p in root.iterdir()} if root.is_dir() else REQUIRED
    if missing: raise ValueError(f"missing required files: {sorted(missing)}")
    validate_hashes(root)
    manifest = StudyManifest.model_validate_json((root/"study_manifest.json").read_text("utf-8"))
    schema = json.loads((root/"schema.json").read_text("utf-8"))
    frame = pd.read_csv(root/"analysis_rows.csv")
    if list(frame.columns) != schema["columns"]: raise ValueError("unexpected columns or ordering")
    if frame["feature_row_id"].duplicated().any(): raise ValueError("duplicate analytical key")
    for col in ("treatment","outcome"):
        if not set(frame[col].unique()).issubset({0,1}): raise ValueError(f"invalid {col} domain")
    numeric = frame.select_dtypes(include=[np.number]).to_numpy()
    if not np.isfinite(numeric).all(): raise ValueError("nonfinite numeric value")
    if len(frame)!=manifest.row_count or int(frame.treatment.sum())!=manifest.treated_count: raise ValueError("cohort counts disagree")
    nuisance = pd.read_csv(root/"nuisance_predictions.csv")
    if nuisance.feature_row_id.tolist()!=frame.feature_row_id.tolist(): raise ValueError("nuisance row alignment mismatch")
    if ((nuisance.propensity_score<=0)|(nuisance.propensity_score>=1)).any(): raise ValueError("invalid propensity score")
    return manifest, frame, nuisance

def validate_folds(root: Path, frame: pd.DataFrame) -> np.ndarray:
    fold = json.loads((root/"fold_manifest.json").read_text("utf-8")); mapping={x["genericLaunchId"]:x["fold"] for x in fold["groups"]}
    if len(mapping)!=len(fold["groups"]) or set(frame.generic_launch_id)-set(mapping): raise ValueError("invalid fold membership")
    result=frame.generic_launch_id.map(mapping).to_numpy()
    if any(frame.assign(fold=result).groupby("generic_launch_id").fold.nunique()!=1): raise ValueError("launch group crosses folds")
    return result
