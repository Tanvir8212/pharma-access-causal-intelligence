def run_refutations(model,identified,estimate,seed=17):
    specs=[("placebo_treatment_refuter",{"placebo_type":"permute","random_seed":seed}),("random_common_cause",{"random_seed":seed}),("data_subset_refuter",{"subset_fraction":.8,"random_seed":seed}),("bootstrap_refuter",{"num_simulations":20,"random_seed":seed})]
    out=[]
    for name,kwargs in specs:
        try:
            r=model.refute_estimate(identified,estimate,method_name=name,**kwargs);out.append({"refuter":name,"supported":True,"newEffect":float(r.new_effect),"result":str(r),"interpretation":"No strong contradiction detected by this diagnostic; this does not prove identification."})
        except Exception as exc:out.append({"refuter":name,"supported":False,"error":type(exc).__name__,"interpretation":"Refuter unsupported or failed; no causal conclusion is drawn."})
    return out
