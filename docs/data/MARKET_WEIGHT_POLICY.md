# Frozen market-weight policy

Weights are deterministic nonnegative state prescription totals from a named historical reference window ending before the analytical observation period. The window and version are recorded. Future/outcome-quarter observations are prohibited, negative weights are rejected, and a zero total fails explicitly. Weighted Distribution delegates to the Domain `DistributionMetrics` implementation without rounding.
