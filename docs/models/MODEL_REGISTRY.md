# Model registry

Experiment metadata uses the `ml` schema. Artifacts remain filesystem binaries with database metadata only. Statuses are Candidate, ValidationSelected, PendingApproval, Approved, Rejected, Archived, and Corrupted. Milestone 4 creates Candidate and ValidationSelected records only. Prediction requires an explicitly selected or registry-resolved scoreable artifact, matching feature-set/schema hashes, and successful artifact SHA-256 verification.
# Milestone 5 registry lifecycle

Statuses are Candidate, ValidationSelected, PendingApproval, Approved, Champion, Challenger, Rejected, Archived and Corrupted. Approval is explicit and audited. Artifact integrity and schema compatibility are mandatory; one champion is enforced per task/environment and replacement archives the previous champion transactionally.
