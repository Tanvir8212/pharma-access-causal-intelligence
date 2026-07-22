"""Structured parser for frozen FDA first-generic HTML tables."""
from __future__ import annotations
import hashlib,json,re
from dataclasses import asdict,dataclass
from datetime import datetime
from html.parser import HTMLParser
from pathlib import Path

@dataclass(frozen=True)
class ApprovalRow:
    approval_year:int;sequence_number:int;anda_number_raw:str;anda_number:str;generic_name_raw:str;anda_applicant_raw:str;brand_name_raw:str;approval_date_raw:str;approval_date:str;date_correction:str|None;indication_raw:str;source_row_number:int;source_row_hash:str;parser_version:str="fda-first-generic-html-v1.1"

class _Tables(HTMLParser):
    def __init__(self):super().__init__(convert_charrefs=True);self.tables=[];self._table=None;self._row=None;self._cell=None
    def handle_starttag(self,tag,attrs):
        if tag=="table":self._table=[]
        elif tag=="tr" and self._table is not None:self._row=[]
        elif tag in {"td","th"} and self._row is not None:self._cell=[]
    def handle_data(self,data):
        if self._cell is not None:self._cell.append(data)
    def handle_endtag(self,tag):
        if tag in {"td","th"} and self._cell is not None and self._row is not None:self._row.append(" ".join("".join(self._cell).split()));self._cell=None
        elif tag=="tr" and self._row is not None and self._table is not None:
            if any(self._row):self._table.append(self._row)
            self._row=None
        elif tag=="table" and self._table is not None:self.tables.append(self._table);self._table=None

def parse(path:str|Path,year:int)->list[ApprovalRow]:
    parser=_Tables();parser.feed(Path(path).read_text(encoding="utf-8",errors="strict"));candidates=[t for t in parser.tables if t and any("ANDA Number" in c for c in t[0])]
    if len(candidates)!=1:raise ValueError(f"Expected one FDA approval table for {year}; found {len(candidates)}")
    physical=candidates[0][1:];selected=[]
    for source_row,cells in enumerate(physical,1):
        matches=[x for x in selected if x[1][0].strip()==cells[0].strip()]
        if matches:
            previous_index,previous=matches[0]
            same_authority_identity=all(previous[i]==cells[i] for i in (1,3,5))
            if year!=2024 or not same_authority_identity:raise ValueError(f"FDA {year} contains conflicting duplicate sequence {cells[0]}")
            if sum(len(cells[i]) for i in (2,4,6))>sum(len(previous[i]) for i in (2,4,6)):selected[selected.index(matches[0])]=(source_row,cells)
        else:selected.append((source_row,cells))
    rows=[]
    for source_row,cells in selected:
        if len(cells)!=7:raise ValueError(f"FDA {year} table row {source_row} has {len(cells)} cells, expected 7")
        sequence=cells[0].strip();anda_raw=cells[1].strip()
        if not sequence.isdigit():raise ValueError(f"FDA {year} row {source_row} has invalid sequence")
        digits=re.sub(r"\D","",anda_raw)
        if len(digits)>6 or not digits:raise ValueError(f"FDA {year} row {source_row} has invalid ANDA")
        anda=digits.zfill(6)
        date_raw=cells[5];corrected="3/30/2023" if year==2023 and date_raw=="3/30/203" else date_raw;correction="FDA_HTML_MALFORMED_YEAR_203_TO_2023" if corrected!=date_raw else None
        try:date=datetime.strptime(corrected,"%m/%d/%Y").date()
        except ValueError as exc:raise ValueError(f"FDA {year} row {source_row} has invalid approval date") from exc
        if date.year!=year:raise ValueError(f"FDA {year} row {source_row} approval-year mismatch")
        canonical=json.dumps([year,int(sequence),anda,*cells[2:]],ensure_ascii=False,separators=(",",":"));rows.append(ApprovalRow(year,int(sequence),anda_raw,anda,cells[2],cells[3],cells[4],date_raw,date.isoformat(),correction,cells[6],source_row,hashlib.sha256(canonical.encode()).hexdigest().upper()))
    sequences=[r.sequence_number for r in rows]
    if len(sequences)!=len(set(sequences)):raise ValueError(f"FDA {year} contains duplicate sequence values")
    return rows

def write_json(html_path:str,year:int,output_path:str)->None:Path(output_path).write_text(json.dumps([asdict(r) for r in parse(html_path,year)],indent=2,ensure_ascii=False),encoding="utf-8")
