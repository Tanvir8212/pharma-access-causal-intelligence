# EconML validation

Independent validation uses `DRLearner` for binary treatment/outcome with logistic propensity and conservative random-forest stages. Cross-fitting uses exported deterministic `GenericLaunchId` folds. ATE is primary; averaging conditional effects over treated rows is exploratory ATT. Heterogeneous effects are exploratory.
