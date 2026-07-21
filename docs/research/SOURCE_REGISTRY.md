# Source registry

Required source types are FDA first-generic approval; CMS Medicaid State Drug Utilization; state/territory reference; region/adjacency; optional RxNorm normalization; and manually curated mappings. Registration is local-file-only beneath configured import roots. It validates canonical paths, existence, extension (`csv`, `tsv`, `txt`, `json`), encoding, SHA-256 and duplicate source codes. No URL is fetched and contents are never logged. Any changed file requires a new registration and freeze.
