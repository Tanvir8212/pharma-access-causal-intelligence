from __future__ import annotations
import argparse, hashlib, json
from pathlib import Path
import numpy as np
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import GroupKFold

FEATURES=["quarter_since_approval","state_weight","previous_nd","previous_wd","previous_gap","lag1_prescription_log"]

def cross_fitted_att(frame:pd.DataFrame, folds:int=5)->dict:
    x=frame[FEATURES].to_numpy(float);t=frame.treatment.to_numpy(int);y=frame.outcome.to_numpy(int);groups=frame.generic_launch_id.to_numpy()
    propensity=np.zeros(len(frame));m0=np.zeros(len(frame));m1=np.zeros(len(frame));splitter=GroupKFold(n_splits=folds)
    for train,test in splitter.split(x,y,groups):
        ps=LogisticRegression(max_iter=2000,random_state=17).fit(x[train],t[train]);propensity[test]=ps.predict_proba(x[test])[:,1]
        outcome=LogisticRegression(max_iter=2000,random_state=17).fit(np.column_stack([t[train],x[train]]),y[train])
        m0[test]=outcome.predict_proba(np.column_stack([np.zeros(len(test)),x[test]]))[:,1]
        m1[test]=outcome.predict_proba(np.column_stack([np.ones(len(test)),x[test]]))[:,1]
    p=np.clip(propensity,.02,.98);n1=t.sum();att=(t*(y-m0)-(1-t)*p/(1-p)*(y-m0)).sum()/n1
    return {"estimator":"cross-fitted AIPW ATT risk difference","estimate":float(att),"rows":len(frame),"treated":int(n1),"control":int(len(frame)-n1),"folds":folds,"grouping":"GenericLaunchId","propensityMinimum":float(p.min()),"propensityMaximum":float(p.max()),"clipping":[.02,.98]}

def main()->int:
    p=argparse.ArgumentParser();p.add_argument("input");p.add_argument("dotnet");p.add_argument("output");a=p.parse_args()
    source=Path(a.input);frame=pd.read_csv(source);result=cross_fitted_att(frame);governed=json.loads(Path(a.dotnet).read_text(encoding="utf-8-sig"));primary=next(x for x in governed["Estimates"] if x["Estimator"]==3)
    result.update({"dotnetEstimate":primary["Estimate"],"absoluteDifference":abs(result["estimate"]-primary["Estimate"]),"inputSha256":hashlib.sha256(source.read_bytes()).hexdigest().upper(),"role":"independent Python methodological validation; .NET remains governed production pipeline","observationalNotice":"Association interpreted through the approved causal model under stated identification assumptions; not proof of causation."})
    Path(a.output).write_text(json.dumps(result,indent=2),encoding="utf-8");print(json.dumps(result));return 0
if __name__=="__main__":raise SystemExit(main())
