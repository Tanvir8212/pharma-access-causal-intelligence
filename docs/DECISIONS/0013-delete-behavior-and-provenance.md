# ADR 0013: Restrictive deletion for provenance

Status: Accepted (Milestone 1)

Use `Restrict` on relationships whose cascade deletion could erase research lineage: drugs to products/approvals/utilization, datasets to files/utilization/jobs, and state/quarter/product references from utilization.

Deletion must be an explicit, reviewed operation. Archival and status transitions are preferred over destructive removal for reproducible records.
