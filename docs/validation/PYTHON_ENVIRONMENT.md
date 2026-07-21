# Python environment

Milestone 7 selects 64-bit CPython 3.12.10 and creates repository-root `.venv` through `scripts/setup-python-validation.ps1`. The project is `python/pyproject.toml`; a successful resolution is frozen in `python/requirements.lock`. Global installation, Git dependencies, notebooks, cloud/database drivers, AutoML/Ray extras, and live services are prohibited. Setup checks Python, installs released pins, freezes transitive dependencies, and runs `pip check`.
