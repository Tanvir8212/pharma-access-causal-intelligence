import hashlib,json,shutil
from pharma_access_validation.cli import main

def _rehash(root):
    path=root/"file_hashes.json";doc=json.loads(path.read_text());doc["files"]["csharp_estimates.json"]=hashlib.sha256((root/"csharp_estimates.json").read_bytes()).hexdigest();canonical="\n".join(f"{k}:{v}" for k,v in sorted(doc["files"].items())).encode();doc["reproducibilityHash"]=hashlib.sha256(canonical).hexdigest();path.write_text(json.dumps(doc,indent=2)+"\n")

def test_cli_exit_codes(bundle,tmp_path):
    assert main(["--bundle",str(bundle),"--reports",str(tmp_path/"reports")])==0
    assert main(["--bundle",str(tmp_path/"missing"),"--reports",str(tmp_path/"bad")])==2

def test_cli_formula_mismatch_returns_documented_nonzero(bundle,tmp_path):
    altered=tmp_path/"altered";shutil.copytree(bundle,altered);doc=json.loads((altered/"csharp_estimates.json").read_text());doc["estimates"][0]["estimate"]+=.25;(altered/"csharp_estimates.json").write_text(json.dumps(doc,indent=2)+"\n");_rehash(altered)
    assert main(["--bundle",str(altered),"--reports",str(tmp_path/"mismatch-reports")])==3

def test_cli_malformed_estimate_contract_prints_clear_error(bundle,tmp_path,capsys):
    altered=tmp_path/"malformed";shutil.copytree(bundle,altered);doc=json.loads((altered/"csharp_estimates.json").read_text());doc["estimates"].pop();(altered/"csharp_estimates.json").write_text(json.dumps(doc,indent=2)+"\n");_rehash(altered)
    assert main(["--bundle",str(altered),"--reports",str(tmp_path/"malformed-reports")])==2
    assert "missing required estimators" in capsys.readouterr().err
