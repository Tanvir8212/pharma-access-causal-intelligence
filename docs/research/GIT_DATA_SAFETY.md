# Git data safety

The read-only safety scan reports tracked or staged raw-data paths, non-synthetic CSVs, backups/databases, model binaries, `.env`, virtual environments and generated bundles. It reports paths and remediation only; it never deletes files or prints secret values. Real raw data and generated freeze packages are ignored by default.
