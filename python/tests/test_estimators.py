import numpy as np,pytest
from pharma_access_validation.estimators import *

def test_hand_calculated_weights_effect_and_ess():
    t=np.array([1,0]);y=np.array([1,0]);p=np.array([.8,.25]);assert np.allclose(weights(t,p,"ATT"),[1,1/3]);assert np.allclose(weights(t,p,"ATE"),[1.25,4/3]);assert weighted_effect(t,y,weights(t,p,"ATT"))==1;assert effective_sample_size([1,1])==2

def test_aipw_ate_hand_calculation_uses_row_contributions():
    t=np.array([1,0]);y=np.array([1,0]);p=np.array([.8,.25]);m1=np.array([.7,.6]);m0=np.array([.2,.1])
    expected=np.array([.5+.3/.8,.5-(0-.1)/.75])
    assert expected==pytest.approx([.875,.6333333333333333])
    assert aipw_contributions(t,y,p,m1,m0,"ATE")==pytest.approx(expected)
    assert aipw(t,y,p,m1,m0,"ATE")==pytest.approx(.7541666666666667)

def test_aipw_att_is_separate_from_ate():
    t=np.array([1,0]);y=np.array([1,0]);p=np.array([.8,.25]);m1=np.array([.7,.6]);m0=np.array([.2,.1])
    assert aipw_contributions(t,y,p,m1,m0,"ATT")==pytest.approx([.8,1/30])
    assert aipw(t,y,p,m1,m0,"ATT")==pytest.approx(5/6)

def test_smd_and_invalid_propensity():
    assert standardized_mean_difference([1,2,3,4],[0,0,1,1])>0
    with pytest.raises(ValueError,match="propensity"):weights([0,1],[0,1])
