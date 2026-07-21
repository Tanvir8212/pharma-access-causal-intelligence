# ADR 0018: ML.NET predictive modeling

Status: Accepted (Milestone 4)

Use stable ML.NET 5.0.0 inside `PharmaAccess.ML` for production-compatible predictive pipelines. Application/API contracts remain free of `IDataView`, transformers, and trainer types. Predictive output is not causal evidence.
