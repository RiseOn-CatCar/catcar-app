---
feature: fix-aspire-tests-build
kind: spec
status: accepted
created: 2026-06-26
updated: 2026-06-27
phase: archived
---

# Fix: Aspire Configuration, Build Errors, and Test Infrastructure

## Context

The `fundacao-arquitetural-monolito-modular` feature established the project scaffold, but several structural issues remain: the Aspire AppHost was generated from a template (`aspire new`) and retains references to non-existent scaffold projects; Central Package Management is inconsistently configured causing build failures; the AppHost and ServiceDefaults were placed under `src/Infrastructure/` instead of the constitution's canonical `src/Host/`; the API project is not wired to Aspire ServiceDefaults; and the test infrastructure lacks Aspire-based E2E testing. This feature resolves all build errors, corrects the Aspire configuration to match the constitution's modular monolith architecture, and aligns the test strategy with official Aspire testing best practices. See `[[fix-aspire-tests-build/adrs/0001-apphost-location-alignment]]` for the location alignment rationale.

## Acceptance Criteria

### Build resolution

- **AC-033** (ubiquitous): The system shall compile successfully with `dotnet build CatCar.slnx` on the entire solution, with zero errors and zero warnings related to package management or missing project references.

- **AC-034** (ubiquitous): The system shall pin all NuGet package versions centrally in `Directory.Packages.props`, with no `Version` attribute on any `PackageReference` in individual `.csproj` files, including the AppHost and ServiceDefaults projects.

### Aspire AppHost structural alignment

- **AC-035** (ubiquitous): The system shall locate the Aspire AppHost and ServiceDefaults projects under `src/Host/` (one folder per project), with `aspire.config.json` at `src/Host/aspire.config.json`, consistent with the constitution's canonical folder structure.

- **AC-036** (ubiquitous): The system shall configure the Aspire AppHost to reference only the CatCar API project and a PostgreSQL container resource, with no references to non-existent scaffold projects and no commented-out code.

- **AC-037** (ubiquitous): The system shall align all Aspire package versions to 13.4.6 across `Directory.Packages.props`, the AppHost SDK, and the ServiceDefaults project.

- **AC-038** (event): When a developer runs the Aspire AppHost, the system shall start a PostgreSQL container, the CatCar API with ServiceDefaults (OpenTelemetry, health checks, service discovery), and the Aspire Dashboard.

### API ServiceDefaults integration

- **AC-039** (ubiquitous): The system shall wire the API project to Aspire ServiceDefaults, enabling OpenTelemetry tracing, structured health checks, service discovery, and HTTP resilience handlers.

- **AC-040** (ubiquitous): The system shall expose Aspire-compatible health check endpoints at `/health` (overall) and `/alive` (liveness) via ServiceDefaults, in addition to the existing `/health/live` and `/health/ready` endpoints.

### Test infrastructure — BC isolation

- **AC-041** (ubiquitous): The system shall enforce that each BC test project references only its own BC assembly, SharedKernel, and Contracts — never another BC's assembly — consistent with the cross-BC isolation rule.

### Test infrastructure — Aspire E2E

- **AC-042** (ubiquitous): The system shall provide an E2E test project that uses the official Aspire testing pattern with `DistributedApplicationTestingBuilder` to launch the AppHost and verify the complete distributed application wiring (API health, PostgreSQL connectivity, service discovery).

- **AC-043** (ubiquitous): The system shall manage the Aspire AppHost lifecycle in E2E tests using the `IAsyncLifetime` pattern, with proper disposal to prevent resource leaks.

- **AC-044** (event): When E2E tests execute, the system shall wait for all Aspire resources to reach a healthy state before running assertions, using `WaitForResourceHealthyAsync`.

### Test infrastructure — BC integration tests

- **AC-045** (ubiquitous): The system shall retain Testcontainers PostgreSQL for BC-level integration tests, providing isolated database instances per test class without requiring the full Aspire AppHost.

## Out of scope

- Domain logic implementation for any Bounded Context (features 02–07).
- Kubernetes, Terraform, or production deployment configuration (feature 08).
- Unit tests for domain aggregates, value objects, or handlers (subsequent features).
- Migration from Testcontainers to Aspire testing for BC-level integration tests.
- Changes to the constitution or strategic design decisions.
