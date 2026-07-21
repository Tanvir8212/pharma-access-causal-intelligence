# ADR 0044: Exact real-import reconciliation

Status: Accepted (Milestone 9)

Every input row is accepted, rejected, or duplicate exactly once. Registered/raw/normalized/database counts and deterministic aggregates must reconcile before validation. Differences block the dataset and freeze.
