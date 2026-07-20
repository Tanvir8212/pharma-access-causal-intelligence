# Dataset provenance foundation

Milestone 1 defines provenance records but ingests no data. Every future source file belongs to a `DatasetVersion` and preserves the original filename, source category, optional URL/retrieval time, import time, reporting period, uppercase 64-character SHA-256, byte size, row counts, schema version, license note, status, and error details.

Dataset versions move explicitly through Draft → Validating → Validated → Finalized → Archived. Draft or validating versions may be rejected. Rejected versions cannot be finalized, validation is required before finalization, and only finalization sets `FinalizedAtUtc`.

Source-file deletion is restricted at the database relationship. A duplicate SHA-256 is disallowed within the same dataset version. Raw-file immutability, actual hashing, source parsing, and import transactions belong to Milestone 2 and later; no raw input was created or modified here.
