---
feature: fundacao-arquitetural-monolito-modular
kind: context
stage: implementation
updated: 2026-06-26
phase: archived
---

## Now
- All 14 tasks (T-001 through T-014) completed
- Build: 0 errors, 0 warnings
- Format: `dotnet format --verify-no-changes` exits 0
- Tests: 52 passing (19 SharedKernel + 19 Architecture + 14 ServiceOperations integration)
- 4 test projects have no tests yet (CatalogInventory, Communication, IdentityAccess, E2E) — expected, they contain only IntegrationTestBase/placeholders

## Completed
- T-001: Solution scaffold and project structure
- T-002: SharedKernel tactical DDD base classes with RiseOn.ResultRail
- T-003: Contracts assembly with integration event markers
- T-004: Bounded Context folder structures with Marker classes and RiseOn.AutoInject
- T-005: EF Core DbContexts with schema-per-BC, audit interceptor, UUID v7, xmin concurrency
- T-006: Wolverine with in-process transport, EF Core outbox, and RiseOn.AutoInject
- T-007: API composition root with MapGroup per BC, OpenAPI, ProblemDetails, health checks
- T-008: Aspire AppHost with PostgreSQL container and API project reference
- T-009: Automatic EF Core migrations at startup in Development
- T-010: Test project structure with IntegrationTestBase, NetArchTest rules, coverage configuration
- T-011: Code quality tooling (.editorconfig, Roslyn analyzers, dotnet format)
- T-012: Multi-stage Dockerfile and docker-compose.yml
- T-013: GitHub Actions CI pipeline
- T-014: README.md with setup instructions

## Open questions
- B-01: spec.md AC-008 and design.md still reference Portuguese BC/schema names — requires @specificator/@architector

## Pinned facts
- last passed: T-014
- risk-level: HIGH for T-006 surface (completed successfully)
- Code uses English BC names; KB docs still have Portuguese names in places
- concerns C-01..C-04 from review-notes.md deferred
- xunit.runner.visualstudio 2.8.2, MessagePack 2.5.301 pinned
- Wolverine API: UseEntityFrameworkCoreTransactions(), PersistMessagesWithPostgresql(), Policies.UseDurableLocalQueues()
- Swashbuckle removed from API (TypeLoadException in container); OpenAPI via AddOpenApi() + MapOpenApi()
- WolverineFx.RuntimeCompilation added for container runtime
- tests/Directory.Build.props imports parent via MSBuild GetPathOfFileAbove
- NoWarn suppressions: CS1591, CA1707, CA2007, NU1902
