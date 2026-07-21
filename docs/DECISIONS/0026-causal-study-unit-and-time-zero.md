# ADR 0026: Causal unit and time zero

Status: Accepted (Milestone 6)

Use eligible Drug × State × ObservationQuarter rows. Time zero is the observation-quarter start; baseline and treatment information must precede the next-quarter outcome. Censored or incomplete follow-up is excluded.
