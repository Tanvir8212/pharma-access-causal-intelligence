# PharmaAccess Python validation

This isolated Python 3.12 package validates deterministic synthetic C# causal bundles. It never connects to SQL Server and is not the production runtime. Formula parity reuses exported nuisance predictions; independent validation refits separate DoWhy/EconML models. Agreement is not proof of causal identification.

From the repository root, run `scripts/setup-python-validation.ps1`, then `scripts/run-cross-language-parity.ps1 -StudyCode synthetic-peer-exposure -ValidationRunCode m7-synthetic-001`.
