from dowhy import CausalModel

def run_dowhy(root, frame, graph):
    import json
    adj=json.loads((root/"adjustment_set.json").read_text("utf-8"))["variables"]
    dot="digraph {"+";".join(f'\"{a}\" -> \"treatment\"; \"{a}\" -> \"outcome\"' for a in adj)+'; "treatment" -> "outcome";}'
    model=CausalModel(data=frame,treatment="treatment",outcome="outcome",common_causes=adj,graph=dot)
    identified=model.identify_effect(proceed_when_unidentifiable=False)
    estimate=model.estimate_effect(identified,method_name="backdoor.linear_regression",target_units="att")
    return model,estimate,{"identified":True,"estimand":str(identified),"declaredCommonCauses":adj,"estimate":float(estimate.value),"effectScale":"RiskDifference","interpretation":"Identification under the exported graph; not proof that assumptions hold."}
