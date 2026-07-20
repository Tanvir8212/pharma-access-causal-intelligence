# Milestone 1 data-quality rules

Domain constructors and transition methods reject invalid text lengths, non-UTC timestamps, invalid quarter/state/version formats, invalid percentages, negative prescription or source-row counts, negative byte/file row counts, malformed SHA-256 values, rejected rows exceeding known rows, and illegal dataset/job transitions.

Reimbursement is stored as `decimal(19,4)`. Negative reimbursement is deliberately preserved because claims corrections may be represented that way; Milestone 2 must classify and document source-specific interpretation rather than silently discard or clamp it.

`DataQualityStatus` provides Unchecked, Valid, Warning, Error, and Blocking states. This milestone does not implement validation execution, thresholds, source parsing, or a claim that any real dataset passed quality checks.
