# Model artifact storage

Development artifacts use configurable `artifacts/models/`. Version directories combine task, experiment, dataset, feature set, trainer, and artifact-hash prefix. Paths are canonicalized under the configured root; versions permit safe characters only. Models are immutable, size-limited, SHA-256 verified on load, and accompanied by JSON manifest, Markdown card, and reproducibility JSON. Generated binaries are ignored by Git.

LightGBM includes a native runtime. Availability and tiny numerical differences can vary by OS/CPU; the current Windows synthetic verification succeeded, but cross-platform reproducibility must be checked in CI.
