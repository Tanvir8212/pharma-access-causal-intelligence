# Private data layout

Real inputs live only below the configured `PHARMAACCESS_PRIVATE_DATA_ROOT`, normally `data/private/`. Recommended children are `fda`, `medicaid`, `reference`, `mappings`, and `working`. The tree is Git-ignored. Paths in manifests are root-relative; URLs, UNC paths, traversal, and reparse-point escapes are rejected. Extracted and normalized files remain beneath a new private `working` directory and are never placed in `samples`.

No command logs file contents or exports private absolute paths. `config/research-sources.example.json` is illustrative; the actual `config/research-sources.private.json` is ignored.
