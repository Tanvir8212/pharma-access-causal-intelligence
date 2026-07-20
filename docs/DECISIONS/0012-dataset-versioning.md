# ADR 0012: Dataset version lifecycle and source lineage

Status: Accepted (Milestone 1)

Each reproducible snapshot has a unique controlled version code and explicit validation lifecycle. Finalization requires successful validation; rejection blocks finalization; finalization records a UTC timestamp; archival follows finalization. Source files belong to one version and are identified within it by SHA-256.

This foundation records provenance but performs no ingestion. Dataset hashing, immutable raw storage, and transactional registration arrive in Milestone 2.
