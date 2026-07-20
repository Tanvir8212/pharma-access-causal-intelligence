# ADR 0001: .NET and Clean Architecture

Status: Accepted (Milestone 0)

Use the installed stable .NET SDK 10.0.302 and target `net10.0` for all production and test projects. Pin the installed feature band in `global.json`, allow only later patches in that band, and reject preview SDKs. Organize dependencies inward around Domain and Application.

This decision supersedes the original Milestone 0 selection of SDK 2.1.402 after manual verification discovered that SDK 10.0.302 was also installed and that the obsolete pin forced unsupported tooling.
