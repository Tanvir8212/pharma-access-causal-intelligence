# Research database safety

The only default-approved development name is `PharmaAccessCausalIntelligence_ResearchDev`; project ownership ID is `PharmaAccessCausalIntelligence`. Credentials come from user secrets, environment variables, or another ignored provider. Checked-in settings remain empty.

Read-only preflight verifies SQL Server, sanitized server metadata, exact database/current-database names, migration history, missing/unexpected migrations, the `research.ResearchDatabaseOwnership` marker, foreign tables, and the explicit write gate `PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE=YES`. A connection string never implies write approval. Normal startup never migrates. Any failed check blocks migration and import.
