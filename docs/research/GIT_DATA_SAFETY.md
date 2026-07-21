# Git data safety

The read-only safety scan reports tracked or staged raw-data paths, non-synthetic CSVs, backups/databases, model binaries, `.env`, virtual environments and generated bundles. It reports paths and remediation only; it never deletes files or prints secret values. Real raw data and generated freeze packages are ignored by default.

`data/private/**`, private source/database manifests, `artifacts/research-audit/**`, and real freeze outputs are ignored. Before real execution, tracked/staged scans must be empty; a dirty repository blocks a real freeze unless an explicit reviewed exception is recorded.
