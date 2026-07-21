from __future__ import annotations
import numpy as np

def _arrays(t,y,p=None):
    t=np.asarray(t,dtype=int); y=np.asarray(y,dtype=float)
    if len(t)==0 or len(t)!=len(y) or set(np.unique(t))!={0,1}: raise ValueError("treated and control rows required")
    if not np.isfinite(y).all(): raise ValueError("nonfinite outcome")
    if p is not None:
        p=np.asarray(p,dtype=float)
        if len(p)!=len(t) or not np.isfinite(p).all() or np.any((p<=0)|(p>=1)): raise ValueError("invalid propensity score")
    return t,y,p

def unadjusted(t,y):
    t,y,_=_arrays(t,y); return float(y[t==1].mean()-y[t==0].mean())

def weights(t,p,estimand="ATT",stabilized=False):
    if estimand not in {"ATE","ATT"}: raise ValueError(f"unsupported estimand: {estimand}")
    t=np.asarray(t,dtype=int); p=np.asarray(p,dtype=float); _arrays(t,np.zeros(len(t)),p)
    w=np.where(t==1,1.0,p/(1-p)) if estimand=="ATT" else np.where(t==1,1/p,1/(1-p))
    if stabilized: w*=np.where(t==1,t.mean(),1-t.mean())
    if not np.isfinite(w).all(): raise ValueError("infinite weights")
    return w

def effective_sample_size(w):
    w=np.asarray(w,dtype=float)
    if len(w)==0 or np.any(w<0) or not np.isfinite(w).all() or np.sum(w*w)==0: raise ValueError("invalid weights")
    return float(w.sum()**2/np.sum(w*w))

def weighted_effect(t,y,w):
    t,y,_=_arrays(t,y); w=np.asarray(w,float)
    if len(w)!=len(t) or np.any(w<0) or not np.isfinite(w).all(): raise ValueError("invalid weights")
    return float(np.average(y[t==1],weights=w[t==1])-np.average(y[t==0],weights=w[t==0]))

def aipw_contributions(t,y,p,m1,m0,estimand="ATT"):
    if estimand not in {"ATE","ATT"}: raise ValueError(f"unsupported estimand: {estimand}")
    t,y,p=_arrays(t,y,p); m1=np.asarray(m1,float); m0=np.asarray(m0,float)
    if len(m1)!=len(t) or len(m0)!=len(t) or not np.isfinite(np.r_[m1,m0]).all(): raise ValueError("invalid nuisance prediction")
    if estimand=="ATE": return m1-m0+t*(y-m1)/p-(1-t)*(y-m0)/(1-p)
    n1=t.sum(); return np.where(t==1,(y-m0)/n1,-p/(1-p)*(y-m0)/n1)

def aipw(t,y,p,m1,m0,estimand="ATT"):
    c=aipw_contributions(t,y,p,m1,m0,estimand); return float(c.mean() if estimand=="ATE" else c.sum())

def standardized_mean_difference(x,t,w=None):
    x=np.asarray(x,float); t=np.asarray(t,int)
    if not np.isfinite(x).all(): raise ValueError("nonfinite covariate")
    def moments(mask):
        ww=np.ones(mask.sum()) if w is None else np.asarray(w,float)[mask]; xx=x[mask]; mean=np.average(xx,weights=ww); return mean,np.average((xx-mean)**2,weights=ww)
    mt,vt=moments(t==1); mc,vc=moments(t==0); pooled=np.sqrt((vt+vc)/2)
    return 0.0 if pooled==0 else float((mt-mc)/pooled)

def grouped_bootstrap(values,groups,statistic,repetitions=100,seed=17):
    values=np.asarray(values); groups=np.asarray(groups); unique=np.unique(groups); rng=np.random.default_rng(seed); out=[]
    for _ in range(repetitions):
        chosen=rng.choice(unique,len(unique),replace=True); idx=np.concatenate([np.flatnonzero(groups==g) for g in chosen]); out.append(statistic(values[idx]))
    return {"standardError":float(np.std(out)),"lower":float(np.quantile(out,.025)),"upper":float(np.quantile(out,.975)),"repetitions":repetitions}
