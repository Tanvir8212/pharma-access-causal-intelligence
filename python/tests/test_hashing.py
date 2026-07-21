import pytest
from pharma_access_validation.hashing import validate_hashes

def test_hashes_validate_and_modification_is_rejected(bundle,tmp_path):
    validate_hashes(bundle);target=tmp_path/"bundle";import shutil;shutil.copytree(bundle,target);(target/"schema.json").write_text("{}")
    with pytest.raises(ValueError,match="modified"):validate_hashes(target)
