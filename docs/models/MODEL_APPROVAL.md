# Manual model approval

Training ends at `Candidate` or `ValidationSelected`. An authenticated administrative caller must later supply artifact ID, decision, reviewer, reason, environment, promotion choice and model-card version. Integrity and feature-schema checks precede approval. Corrupted/incompatible artifacts are rejected; synthetic artifacts are development-only. Champion replacement archives the prior champion within one repository transaction, and a filtered database index permits one champion per task/environment. No anonymous approval endpoint exists.
