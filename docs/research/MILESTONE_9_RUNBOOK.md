# Milestone 9 runbook

1. Set `PHARMAACCESS_PRIVATE_DATA_ROOT`; keep the tree ignored.
2. Run `discover-research-sources` separately per category into ignored `artifacts/research-audit/`.
3. Copy the sanitized example to ignored `config/research-sources.private.json`; assign files, hashes, profiles, coverage, and extraction policy.
4. Run complete dry-run validation and resolve blocking mappings.
5. Configure only `PharmaAccessCausalIntelligence_ResearchDev` through user secrets/environment configuration.
6. Run read-only database preflight and review migration plan. Do not infer permission from connectivity.
7. Set `PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE=YES` only after explicit human authorization; then use `scripts/Invoke-ResearchDatabaseMigration.ps1 -WhatIf` before any confirmed invocation. The wrapper requires the exact local server/database, an existing empty or correctly owned database, and explicit confirmation. It never creates the database.
8. Reconcile import, explicitly validate the real dataset, build frozen features/split/causal readiness bundle, and review Git safety.
9. Generate `real-{FreezeCode}` candidate artifacts. Human protocol and freeze approvals remain separate.

Without any prerequisite, run `prepare-m9 <audit-root> <report-code>` to emit a sanitized missing-input report and stop. Final predictive training and causal estimation are outside Milestone 9.
