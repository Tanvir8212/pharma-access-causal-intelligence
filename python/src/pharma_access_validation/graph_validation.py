from __future__ import annotations
import json
from pathlib import Path
import networkx as nx

def validate_graph(root: Path):
    dag=json.loads((root/"dag.json").read_text("utf-8")); adj=json.loads((root/"adjustment_set.json").read_text("utf-8")); nodes={n.get("name") or n.get("Name"):n.get("role") or n.get("Role") for n in dag.get("nodes",dag.get("Nodes",[]))}; edges=[(e.get("from") or e.get("From"),e.get("to") or e.get("To")) for e in dag.get("edges",dag.get("Edges",[]))]; g=nx.DiGraph(edges); g.add_nodes_from(nodes)
    if not nx.is_directed_acyclic_graph(g): raise ValueError("graph is cyclic")
    treatment=[n for n,r in nodes.items() if str(r).lower() in {"treatment","0"}]; outcome=[n for n,r in nodes.items() if str(r).lower() in {"outcome","1"}]
    if len(treatment)!=1 or len(outcome)!=1: raise ValueError("graph requires one treatment and outcome")
    variables=adj["variables"]; missing=set(variables)-set(nodes)
    descendants=nx.descendants(g,treatment[0]); invalid=set(variables)&descendants
    result={"treatmentNode":treatment[0],"outcomeNode":outcome[0],"missingNodes":sorted(missing),"descendantAdjustments":sorted(invalid),"adjustmentVariables":variables,"status":"ExactMatch" if not missing and not invalid else "Failed"}
    if result["status"]=="Failed": raise ValueError(f"DAG/adjustment mismatch: {result}")
    return result,g
