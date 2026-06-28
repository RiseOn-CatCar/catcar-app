---
feature: fix-aspire-tests-build
kind: constraints
updated: 2026-06-27
phase: archived
---

# Constraints — Fix: Aspire Configuration, Build Errors, and Test Infrastructure

| ID      | Kind                 | Severity | Rule                                                                                                                                                                                                                       | Source                                              | Added       |
|---------|----------------------|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------|-------------|
| CON-016 | required_pattern     | block    | The Aspire AppHost project (`CatCar.AppHost.csproj`) and the Aspire ServiceDefaults project (`CatCar.ServiceDefaults.csproj`) must reside under `src/Host/` (one folder per project: `src/Host/CatCar.AppHost/`, `src/Host/CatCar.ServiceDefaults/`). The `aspire.config.json` must reside at `src/Host/aspire.config.json`. Neither project may reside under `src/Infrastructure/`. | `adrs/0001-apphost-location-alignment.md#decision`  | 2026-06-26  |