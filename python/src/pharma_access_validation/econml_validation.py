import numpy as np
from econml.dr import DRLearner
from sklearn.linear_model import LogisticRegression
from sklearn.ensemble import RandomForestRegressor

def run_econml(frame,variables,folds,seed=17):
    x=frame[variables].to_numpy(float);t=frame.treatment.to_numpy(int);y=frame.outcome.to_numpy(float);splits=[]
    for fold in sorted(np.unique(folds)):
        test=np.flatnonzero(folds==fold);train=np.flatnonzero(folds!=fold)
        if len(np.unique(t[train]))==2 and len(test):splits.append((train,test))
    est=DRLearner(model_propensity=LogisticRegression(max_iter=2000,random_state=seed),model_regression=RandomForestRegressor(n_estimators=50,min_samples_leaf=5,random_state=seed,n_jobs=1),model_final=RandomForestRegressor(n_estimators=50,min_samples_leaf=5,random_state=seed,n_jobs=1),cv=splits,random_state=seed)
    est.fit(y,t,X=x);effects=np.asarray(est.effect(x),float)
    return {"estimator":"DRLearner","estimand":"ATE","effectScale":"RiskDifference","ate":float(effects.mean()),"exploratoryAtt":float(effects[t==1].mean()),"treatedCount":int(t.sum()),"controlCount":int((1-t).sum()),"folds":"precomputed deterministic GenericLaunchId groups","nuisanceModels":["LogisticRegression","RandomForestRegressor"],"inference":"point estimates; heterogeneous effects exploratory","warning":"Independent estimate under observational assumptions; not proof of causation."}
