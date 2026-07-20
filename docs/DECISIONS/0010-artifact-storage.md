# ADR 0010: Filesystem-first artifact storage

Status: Accepted for initial implementation

Store generated model and experiment artifacts under configured filesystem roots, with metadata and SHA-256 hashes persisted separately in later milestones. Generated artifacts are ignored by Git; immutable, hashed storage and provider alternatives can be added without changing core contracts.
