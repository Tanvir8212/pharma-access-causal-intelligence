from __future__ import annotations
import hashlib, json
from pathlib import Path
from .contracts import HashManifest

def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def validate_hashes(root: Path) -> HashManifest:
    manifest = HashManifest.model_validate_json((root / "file_hashes.json").read_text("utf-8"))
    for name, expected in manifest.files.items():
        path = root / name
        if not path.is_file(): raise ValueError(f"missing required hashed file: {name}")
        if sha256(path) != expected.lower(): raise ValueError(f"modified file: {name}")
    canonical = "\n".join(f"{k}:{v}" for k, v in sorted(manifest.files.items())).encode()
    if hashlib.sha256(canonical).hexdigest() != manifest.reproducibility_hash: raise ValueError("reproducibility hash mismatch")
    return manifest

def write_hashes(root: Path, names: list[str]) -> None:
    files = {name: sha256(root / name) for name in sorted(names)}
    reproducibility = hashlib.sha256("\n".join(f"{k}:{v}" for k,v in files.items()).encode()).hexdigest()
    (root / "file_hashes.json").write_text(json.dumps({"contractVersion":"1.1","files":files,"reproducibilityHash":reproducibility}, indent=2)+"\n", encoding="utf-8")
