from pathlib import Path
import pytest
from pharma_access_validation.fda_first_generic_html import parse


def test_structured_table_parser_preserves_fields(tmp_path: Path):
    html="""<table><tr><th>&nbsp;</th><th>ANDA Number</th><th>Generic Name</th><th>ANDA Applicant</th><th>Brand Name</th><th>ANDA Approval Date</th><th>ANDA Indication</th></tr><tr><td>1</td><td>78479</td><td>Drug, 1 mg</td><td>Applicant</td><td>Brand</td><td>01/31/2022</td><td>Indication</td></tr></table>"""
    path=tmp_path/"page.html";path.write_text(html,encoding="utf-8")
    rows=parse(path,2022)
    assert len(rows)==1 and rows[0].anda_number=="078479" and rows[0].sequence_number==1

def test_parser_applies_only_the_documented_2023_date_correction(tmp_path: Path):
    html="""<table><tr><th></th><th>ANDA Number</th><th>Generic Name</th><th>ANDA Applicant</th><th>Brand Name</th><th>ANDA Approval Date</th><th>ANDA Indication</th></tr><tr><td>78</td><td>123456</td><td>Drug</td><td>Applicant</td><td>Brand</td><td>3/30/203</td><td>Use</td></tr></table>"""
    path=tmp_path/"page.html";path.write_text(html,encoding="utf-8");row=parse(path,2023)[0]
    assert row.approval_date_raw=="3/30/203" and row.approval_date=="2023-03-30" and row.date_correction


def test_parser_rejects_wrapped_non_table_rows(tmp_path: Path):
    path=tmp_path/"page.html";path.write_text("<p>1 123456 wrapped text</p>",encoding="utf-8")
    with pytest.raises(ValueError,match="Expected one FDA approval table"): parse(path,2022)

def test_2024_duplicate_representation_keeps_richer_actual_row(tmp_path: Path):
    head="<tr><th></th><th>ANDA Number</th><th>Generic Name</th><th>ANDA Applicant</th><th>Brand Name</th><th>ANDA Approval Date</th><th>ANDA Indication</th></tr>"
    a="<tr><td>1</td><td>203640</td><td>Nilotinib Capsules</td><td>A</td><td>B</td><td>01/02/2024</td><td>I</td></tr>"
    b="<tr><td>1</td><td>203640</td><td>Nilotinib Capsules, 50 mg</td><td>A</td><td>B</td><td>01/02/2024</td><td>I</td></tr>"
    path=tmp_path/"page.html";path.write_text(f"<table>{head}{a}{b}</table>",encoding="utf-8");rows=parse(path,2024)
    assert len(rows)==1 and rows[0].generic_name_raw.endswith("50 mg") and rows[0].source_row_number==2
