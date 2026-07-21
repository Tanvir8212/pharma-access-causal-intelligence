# ADR 0043: Explicit real-data write gate

Status: Accepted (Milestone 9)

Database migration, real-row persistence, dataset finalization, and real-freeze readiness require `PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE=YES` in addition to all safety checks. The application never sets it and a connection string is not approval.
