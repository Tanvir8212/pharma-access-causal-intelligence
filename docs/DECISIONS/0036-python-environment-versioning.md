# ADR 0036: Pin the Python 3.12 validation environment

Status: Accepted (Milestone 7)

Use CPython 3.12.10, released stable packages, root `.venv`, exact direct pins, and complete `pip freeze --all` after successful resolution. Never install globally or from Git branches.
