# Import reconciliation

For every file, registered rows must equal accepted plus rejected plus duplicate rows; raw rows must equal registered rows; database normalized rows must equal accepted rows. Deterministic aggregates and per-file/quarter/state totals are compared. Every row belongs to exactly one category. Any difference is blocking and prevents validation.
