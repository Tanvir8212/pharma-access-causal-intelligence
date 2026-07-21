import subprocess
from pathlib import Path
import pytest

@pytest.fixture(scope="session")
def bundle(tmp_path_factory):
    root=tmp_path_factory.mktemp("m7-bundles");run="pytest-synthetic"
    repo=Path(__file__).resolve().parents[2]
    subprocess.run(["dotnet","run","--no-build","--project",str(repo/"src/PharmaAccess.Worker/PharmaAccess.Worker.csproj"),"--","export-m7-synthetic",str(root),run],cwd=repo,check=True)
    return root/run
