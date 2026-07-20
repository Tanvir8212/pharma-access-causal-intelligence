# ADR 0015: Feature availability and leakage audit

Status: Accepted (Milestone 3)

C# calculations are authoritative. Every input carries an as-of rule and cutoff; exact-quarter lags do not bridge gaps. A dedicated audit produces machine-readable findings, and blocking findings prevent persistence or finalization.
