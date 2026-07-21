# Cohort definition

The grain is `GenericLaunchId × StateCode × CalendarQuarter`. The versioned definition records population, start/end, FDA approval proxy, minimum follow-up, eligible-state and state-entry policies, mapping/suppression/missing/duplicate policies, exclusion hierarchy, outcome availability and causal eligibility. Deterministic manifests retain included and excluded keys, controlled reasons/counts, source hashes and cohort hash. Excluded rows are never silently discarded.
