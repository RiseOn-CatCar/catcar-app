---
feature: fundacao-arquitetural-monolito-modular
kind: tasks
status: accepted
created: 2026-06-10
updated: 2026-06-26
phase: archived
---

# Tasks — Fundacao Arquitetural: Monolito Modular

## Test-first discipline note

Constitution §7.3 requires Red → Green → Refactor for all production code. This feature is predominantly scaffold/infrastructure (project files, folder structure, configuration). The test-first discipline applies to tasks that produce domain or infrastructure logic:

- **T-002** (SharedKernel base classes): Explicit Red/Green/Refactor steps in task body — write unit tests for `Entity<TId>` equality, `ValueObject` component equality, and `Upshot<T>` integration (from RiseOn.ResultRail) before implementation.
- **T-005** (EF Core DbContexts): Explicit Red/Green/Refactor steps in task body — write integration tests for `AuditInterceptor`, `SnakeCaseConvention`, and `xmin` mapping via Testcontainers before implementation.
- **T-006** (Wolverine + RiseOn.AutoInject configuration): Explicit Red/Green/Refactor steps in task body — write integration tests verifying outbox persistence in the same transaction as aggregate mutation, and automatic service registration via RiseOn.AutoInject.
- **T-007** (API composition root): Explicit Red/Green/Refactor steps in task body — write integration tests for health check endpoints, OpenAPI document generation, and ProblemDetails responses.
- **T-009** (Automatic migrations): Explicit Red/Green/Refactor steps in task body — write integration tests verifying migrations run automatically in Development but NOT in Production.

Scaffold-only tasks (T-001, T-003, T-004, T-008, T-011–T-014) create project files, folder structures, or configuration — no domain logic to test-first. Their `verification` steps confirm structural correctness via build commands and observable conditions.

## Central Package Management note

Constitution §3.2 references `versions.props` as the central pinning file. In .NET, the canonical filename for Central Package Management is `Directory.Packages.props`. This task uses `Directory.Packages.props` as the implementation of the `versions.props` concept. Both names refer to the same mechanism: a single file at the solution root that pins all NuGet package versions, with no version specified in individual `.csproj` files.

## Drift amendment — English-only code naming

User-requested drift correction (2026-06-10): all code artifacts must use English names per constitution §5.1.

- Atendimento → ServiceOperations
- CatalogoEstoque → CatalogInventory
- Comunicacao → Communication
- Identidade → IdentityAccess

This mapping applies to folder names, project names, namespaces, test project names, API route slugs, and infrastructure identifiers introduced by this feature.

---

## T-001: Create solution scaffold and project structure
- covers: [AC-001, AC-002, AC-003, AC-004]
- files:
  - CatCar.sln
  - Directory.Packages.props
  - Directory.Build.props
  - .gitignore
  - src/SharedKernel/CatCar.SharedKernel.csproj
  - src/Contracts/CatCar.Contracts.csproj
  - src/Contexts/ServiceOperations/CatCar.Contexts.ServiceOperations.csproj
  - src/Contexts/CatalogInventory/CatCar.Contexts.CatalogInventory.csproj
  - src/Contexts/Communication/CatCar.Contexts.Communication.csproj
  - src/Contexts/IdentityAccess/CatCar.Contexts.IdentityAccess.csproj
  - src/Contexts/Atendimento/CatCar.Contexts.Atendimento.csproj (legacy rename/delete)
  - src/Contexts/CatalogoEstoque/CatCar.Contexts.CatalogoEstoque.csproj (legacy rename/delete)
  - src/Contexts/Comunicacao/CatCar.Contexts.Comunicacao.csproj (legacy rename/delete)
  - src/Contexts/Identidade/CatCar.Contexts.Identidade.csproj (legacy rename/delete)
  - src/Api/CatCar.Api.csproj
  - src/Host/CatCar.AppHost.csproj
  - tests/Contexts/ServiceOperations.Tests/CatCar.Contexts.ServiceOperations.Tests.csproj
  - tests/Contexts/CatalogInventory.Tests/CatCar.Contexts.CatalogInventory.Tests.csproj
  - tests/Contexts/Communication.Tests/CatCar.Contexts.Communication.Tests.csproj
  - tests/Contexts/IdentityAccess.Tests/CatCar.Contexts.IdentityAccess.Tests.csproj
  - tests/Contexts/Atendimento.Tests/CatCar.Contexts.Atendimento.Tests.csproj (legacy rename/delete)
  - tests/Contexts/CatalogoEstoque.Tests/CatCar.Contexts.CatalogoEstoque.Tests.csproj (legacy rename/delete)
  - tests/Contexts/Comunicacao.Tests/CatCar.Contexts.Comunicacao.Tests.csproj (legacy rename/delete)
  - tests/Contexts/Identidade.Tests/CatCar.Contexts.Identidade.Tests.csproj (legacy rename/delete)
  - tests/Architecture.Tests/CatCar.Architecture.Tests.csproj
  - tests/E2E/CatCar.E2E.Tests.csproj
  - tests/SharedKernel.Tests/CatCar.SharedKernel.Tests.csproj
- depends_on: []
- verification: `dotnet build CatCar.sln` exits 0. All 15 projects appear in `dotnet sln list` (8 src + 7 tests) using English BC names. `Directory.Packages.props` contains all pinned versions from design.md §1, including RiseOn.ResultRail 1.1.1 and RiseOn.AutoInject 1.0.1-beta. `rg -n "Atendimento|CatalogoEstoque|Comunicacao|Identidade" CatCar.sln src tests --glob "*.csproj"` returns no matches.
- effort: large
- constraints: [CON-004, CON-015]
- status: done

## T-002: Implement SharedKernel tactical DDD base classes with RiseOn.ResultRail
- covers: [AC-007]
- files:
  - src/SharedKernel/Entity.cs
  - src/SharedKernel/ValueObject.cs
  - src/SharedKernel/IAggregateRoot.cs
  - src/SharedKernel/IDomainEvent.cs
  - src/SharedKernel/IIntegrationEvent.cs
  - src/SharedKernel/BusinessRuleViolatedException.cs
  - tests/SharedKernel.Tests/CatCar.SharedKernel.Tests.csproj
  - tests/SharedKernel.Tests/EntityTests.cs
  - tests/SharedKernel.Tests/ValueObjectTests.cs
  - tests/SharedKernel.Tests/UpshotIntegrationTests.cs
- depends_on: [T-001]
- verification: `dotnet build src/SharedKernel/CatCar.SharedKernel.csproj` exits 0. `dotnet test tests/SharedKernel.Tests/` exits 0 with all unit tests passing. `rg -n "CatCar\.Contexts" src/SharedKernel --glob "*.cs"` returns no matches (purity guard until T-010 architecture tests exist). SharedKernel references RiseOn.ResultRail package.
- effort: medium
- constraints: [CON-003]
- **RED:** Write failing unit tests in `tests/SharedKernel.Tests/` for: `Entity<TId>` identity-based equality (same Id → equal, different Id → not equal, null → not equal), `ValueObject` component-based equality (all components equal → equal, any component different → not equal), and `Upshot<T>` integration (verify that `Upshot<T>.Success()` and `Upshot<T>.Fail()` work as expected from RiseOn.ResultRail). Run `dotnet test tests/SharedKernel.Tests/` — confirm tests fail (Red).
- **GREEN:** Implement `Entity<TId>`, `ValueObject`, marker interfaces, and `BusinessRuleViolatedException` in `src/SharedKernel/`. Add `RiseOn.ResultRail` package reference to `CatCar.SharedKernel.csproj`. Do NOT implement custom `Result<T>` — use `Upshot<T>` from RiseOn.ResultRail. Run `dotnet test tests/SharedKernel.Tests/` — confirm all tests pass (Green).
- **REFACTOR:** Review implementations for clarity, remove duplication, ensure XML doc comments on public APIs. Run `dotnet format` and `dotnet test` — confirm no regressions.
- status: done

## T-003: Create Contracts assembly with integration event markers
- covers: [AC-005]
- files:
  - src/Contracts/ServiceOperations/BudgetIssuedIntegrationEvent.cs
  - src/Contracts/ServiceOperations/BudgetApprovedIntegrationEvent.cs
  - src/Contracts/ServiceOperations/WorkOrderStatusChangedIntegrationEvent.cs
  - src/Contracts/CatalogInventory/InventoryReservedIntegrationEvent.cs
  - src/Contracts/CatalogInventory/InventoryConsumedIntegrationEvent.cs
- depends_on: [T-002]
- verification: `dotnet build src/Contracts/CatCar.Contracts.csproj` exits 0. Integration event records implement `IIntegrationEvent` from SharedKernel.
- effort: small
- status: done

## T-004: Scaffold Bounded Context folder structures with Marker classes and RiseOn.AutoInject
- covers: [AC-008, AC-009, AC-010]
- files:
  - src/Contexts/ServiceOperations/Marker.cs
  - src/Contexts/ServiceOperations/CatCar.Contexts.ServiceOperations.csproj
  - src/Contexts/ServiceOperations/Domain/.gitkeep
  - src/Contexts/ServiceOperations/Features/.gitkeep
  - src/Contexts/ServiceOperations/Infrastructure/.gitkeep
  - src/Contexts/ServiceOperations/Integrations/.gitkeep
  - src/Contexts/ServiceOperations/ServiceCollectionExtensions.cs
  - src/Contexts/ServiceOperations/EndpointRouteBuilderExtensions.cs
  - src/Contexts/CatalogInventory/Marker.cs
  - src/Contexts/CatalogInventory/CatCar.Contexts.CatalogInventory.csproj
  - src/Contexts/CatalogInventory/Domain/.gitkeep
  - src/Contexts/CatalogInventory/Features/.gitkeep
  - src/Contexts/CatalogInventory/Infrastructure/.gitkeep
  - src/Contexts/CatalogInventory/ServiceCollectionExtensions.cs
  - src/Contexts/CatalogInventory/EndpointRouteBuilderExtensions.cs
  - src/Contexts/Communication/Marker.cs
  - src/Contexts/Communication/CatCar.Contexts.Communication.csproj
  - src/Contexts/Communication/Domain/.gitkeep
  - src/Contexts/Communication/Features/.gitkeep
  - src/Contexts/Communication/Infrastructure/.gitkeep
  - src/Contexts/Communication/ServiceCollectionExtensions.cs
  - src/Contexts/Communication/EndpointRouteBuilderExtensions.cs
  - src/Contexts/IdentityAccess/Marker.cs
  - src/Contexts/IdentityAccess/CatCar.Contexts.IdentityAccess.csproj
  - src/Contexts/IdentityAccess/Domain/.gitkeep
  - src/Contexts/IdentityAccess/Features/.gitkeep
  - src/Contexts/IdentityAccess/Infrastructure/.gitkeep
  - src/Contexts/IdentityAccess/Integrations/.gitkeep
  - src/Contexts/IdentityAccess/ServiceCollectionExtensions.cs
  - src/Contexts/IdentityAccess/EndpointRouteBuilderExtensions.cs
  - Directory.Packages.props
- depends_on: [T-003]
- verification: `dotnet build CatCar.sln` exits 0. Each BC project has Domain/, Features/, Infrastructure/ subfolders. ServiceOperations and IdentityAccess also have Integrations/. Each BC has a Marker class and ServiceCollectionExtensions/EndpointRouteBuilderExtensions stubs. Each BC .csproj references RiseOn.AutoInject package.
- effort: medium
- constraints: [CON-004, CON-005]
- status: done

## T-005: Implement EF Core DbContexts with schema-per-BC, audit interceptor, UUID v7, and xmin concurrency
- covers: [AC-008, AC-012, AC-013, AC-014]
- files:
  - src/Contexts/ServiceOperations/Infrastructure/ServiceOperationsDbContext.cs
  - src/Contexts/ServiceOperations/Infrastructure/Persistence/AuditInterceptor.cs
  - src/Contexts/ServiceOperations/Infrastructure/Persistence/SnakeCaseConvention.cs
  - src/Contexts/CatalogInventory/Infrastructure/CatalogInventoryDbContext.cs
  - src/Contexts/CatalogInventory/Infrastructure/Persistence/AuditInterceptor.cs
  - src/Contexts/CatalogInventory/Infrastructure/Persistence/SnakeCaseConvention.cs
  - src/Contexts/Communication/Infrastructure/CommunicationDbContext.cs
  - src/Contexts/Communication/Infrastructure/Persistence/AuditInterceptor.cs
  - src/Contexts/Communication/Infrastructure/Persistence/SnakeCaseConvention.cs
  - src/Contexts/IdentityAccess/Infrastructure/IdentityAccessDbContext.cs
  - src/Contexts/IdentityAccess/Infrastructure/Persistence/AuditInterceptor.cs
  - src/Contexts/IdentityAccess/Infrastructure/Persistence/SnakeCaseConvention.cs
  - Directory.Packages.props (drift cleanup: remove EF InMemory pin added out-of-scope)
  - tests/Contexts/ServiceOperations.Tests/CatCar.Contexts.ServiceOperations.Tests.csproj
  - tests/Contexts/ServiceOperations.Tests/Integration/IntegrationTestBase.cs
  - tests/Contexts/ServiceOperations.Tests/Integration/AuditInterceptorTests.cs
  - tests/Contexts/ServiceOperations.Tests/Integration/SnakeCaseConventionTests.cs
  - tests/Contexts/ServiceOperations.Tests/Integration/ConcurrencyTests.cs
- depends_on: [T-004]
- verification: `dotnet build CatCar.sln` exits 0. Each DbContext sets `HasDefaultSchema` to its BC schema. AuditInterceptor populates `created_at`/`updated_at` shadow properties. SnakeCaseConvention converts table/column names. Each DbContext maps `xmin` as `uint RowVersion` with `IsRowVersion()`. `dotnet test tests/Contexts/ServiceOperations.Tests/ --filter Integration` exits 0 with all integration tests passing.
- effort: large
- constraints: [CON-002, CON-006]
- **RED:** Write failing integration tests in `tests/Contexts/ServiceOperations.Tests/Integration/` using Testcontainers PostgreSQL: (1) `AuditInterceptorTests` — verify that inserting and updating an entity populates `created_at` and `updated_at` shadow properties automatically; (2) `SnakeCaseConventionTests` — verify that a test entity maps to `snake_case` table and column names in the database; (3) `ConcurrencyTests` — verify that concurrent updates to the same row trigger `DbUpdateConcurrencyException` via `xmin` mapping. Run `dotnet test tests/Contexts/ServiceOperations.Tests/ --filter Integration` — confirm tests fail (Red).
- **GREEN:** Implement `ServiceOperationsDbContext`, `CatalogInventoryDbContext`, `CommunicationDbContext`, `IdentityAccessDbContext` with `HasDefaultSchema`, `AuditInterceptor`, `SnakeCaseConvention`, and `xmin` row version mapping. Run `dotnet test tests/Contexts/ServiceOperations.Tests/ --filter Integration` — confirm all tests pass (Green).
- **REFACTOR:** Review DbContext configurations for consistency across all 4 BCs, extract shared conventions if appropriate, ensure XML doc comments. Run `dotnet format` and `dotnet test` — confirm no regressions.
- post_review_remediation (2026-06-11):
  - reviewer blockers in-scope to this task: B-02 (package pins drift vs design), B-03 (IntegrationTestBase cleanup schema mismatch), B-04 (AuditInterceptor behavior inconsistency across BCs)
  - reviewer blockers out-of-scope for code task: B-01/B-05 require spec/design canonical-name alignment (user decision: keep EN canonical)
- status: done

## T-006: Configure Wolverine with in-process transport, EF Core outbox, and RiseOn.AutoInject
- covers: [AC-009, AC-016, AC-017, AC-018]
- files:
  - src/Api/Program.cs
  - src/Contexts/ServiceOperations/ServiceCollectionExtensions.cs
  - src/Contexts/CatalogInventory/ServiceCollectionExtensions.cs
  - src/Contexts/Communication/ServiceCollectionExtensions.cs
  - src/Contexts/IdentityAccess/ServiceCollectionExtensions.cs
  - tests/Contexts/ServiceOperations.Tests/Integration/OutboxTests.cs
- depends_on: [T-005]
- verification: `dotnet build CatCar.sln` exits 0. `Program.cs` calls `UseWolverine` with `UseInProcessTransport()`, `Discovery.IncludeAssembly` for all 4 BC Marker assemblies, and `UseEntityFrameworkCoreOutbox()`. Each BC's `ServiceCollectionExtensions` registers its DbContext with Npgsql and calls the generated `Use{BC}Services()` extension from RiseOn.AutoInject. `dotnet test tests/Contexts/ServiceOperations.Tests/ --filter OutboxTests` exits 0 with all outbox integration tests passing.
- effort: medium
- constraints: [CON-007, CON-008]
- **RED:** Write a failing integration test in `tests/Contexts/ServiceOperations.Tests/Integration/OutboxTests.cs` using Testcontainers PostgreSQL: verify that publishing an integration event via Wolverine's `IMessageBus.PublishAsync` within the same transaction as an aggregate mutation persists the event to the `outbox_messages` table in the `service_operations` schema. Run `dotnet test tests/Contexts/ServiceOperations.Tests/ --filter OutboxTests` — confirm test fails (Red).
- **GREEN:** Configure Wolverine in `Program.cs` with `UseWolverine`, `UseInProcessTransport()`, `Discovery.IncludeAssembly` for all 4 BC Marker assemblies, and `UseEntityFrameworkCoreOutbox()`. Update each BC's `ServiceCollectionExtensions` to register its DbContext with Npgsql, enroll in the outbox, and call the generated `Use{BC}Services()` extension from RiseOn.AutoInject for automatic handler/validator registration. Run `dotnet test tests/Contexts/ServiceOperations.Tests/ --filter OutboxTests` — confirm test passes (Green).
- **REFACTOR:** Review Wolverine configuration for clarity, ensure all 4 BC assemblies are discovered, verify outbox table creation in all 4 schemas. Run `dotnet format` and `dotnet test` — confirm no regressions.
- implementation_plan (2026-06-11, pending user review):
  1. RED: add failing integration test `OutboxTests.cs` in `tests/Contexts/ServiceOperations.Tests/Integration/` validating `IMessageBus.PublishAsync` + aggregate mutation persist to `service_operations.outbox_messages` in same transaction.
  2. GREEN: configure `src/Api/Program.cs` with `UseWolverine`, `UseInProcessTransport()`, `Discovery.IncludeAssembly(...)` for 4 BC Marker assemblies, and `UseEntityFrameworkCoreOutbox()`.
  3. GREEN: update each BC `ServiceCollectionExtensions.cs` to register its DbContext with Npgsql and call generated `Use{BC}Services()` from RiseOn.AutoInject.
  4. VERIFY: run `dotnet build CatCar.sln` and `dotnet test tests/Contexts/ServiceOperations.Tests/ --filter OutboxTests` expecting exit 0.
- status: done

## T-007: Wire API composition root with MapGroup per BC, OpenAPI, ProblemDetails, and health checks
- covers: [AC-011, AC-020, AC-021, AC-022]
- files:
  - src/Api/Program.cs
  - src/Api/appsettings.json
  - src/Api/appsettings.Development.json
  - src/Api/Properties/launchSettings.json
  - src/Contexts/ServiceOperations/EndpointRouteBuilderExtensions.cs
  - src/Contexts/CatalogInventory/EndpointRouteBuilderExtensions.cs
  - src/Contexts/Communication/EndpointRouteBuilderExtensions.cs
  - src/Contexts/IdentityAccess/EndpointRouteBuilderExtensions.cs
  - tests/E2E/HealthCheckTests.cs
  - tests/E2E/OpenApiTests.cs
- depends_on: [T-006]
- verification: `dotnet run --project src/Api/CatCar.Api.csproj` starts the API. `curl http://localhost:5134/health/live` returns 200. `curl http://localhost:5134/swagger` returns the SwaggerUI page. `curl http://localhost:5134/openapi/v1.json` returns a valid OpenAPI document. BC MapGroups exist at `/api/v1/service-operations`, `/api/v1/catalog-inventory`, `/api/v1/communication`, and `/api/v1/identity-access`. `dotnet test tests/E2E/ --filter "HealthCheck|OpenApi"` exits 0 with all tests passing.
- effort: medium
- constraints: [CON-010, CON-011]
- **RED:** Write failing integration tests in `tests/E2E/`: (1) `HealthCheckTests` — verify that `GET /health/live` returns 200 and `GET /health/ready` returns 200 when PostgreSQL is healthy; (2) `OpenApiTests` — verify that `GET /openapi/v1.json` returns a valid OpenAPI 3.0 document and `GET /swagger` returns the SwaggerUI HTML page. Use `WebApplicationFactory` or Testcontainers to spin up the API. Run `dotnet test tests/E2E/` — confirm tests fail (Red).
- **GREEN:** Implement the API composition root in `Program.cs`: configure Serilog, Wolverine, BC module registration, OpenAPI (`AddOpenApi` + Swashbuckle), ProblemDetails (`AddProblemDetails`), health checks (`AddHealthChecks`), MapGroup per BC (`/api/v1/service-operations`, `/api/v1/catalog-inventory`, `/api/v1/communication`, `/api/v1/identity-access`), and endpoint mapping. Create stub `EndpointRouteBuilderExtensions` for each BC. Run `dotnet test tests/E2E/` — confirm all tests pass (Green).
- **REFACTOR:** Review Program.cs for clarity, ensure all middleware is ordered correctly (Serilog → ExceptionHandler → HTTPS → OpenAPI → endpoints), verify ProblemDetails responses include `traceId`. Run `dotnet format` and `dotnet test` — confirm no regressions.
- status: done

## T-008: Configure Aspire AppHost with PostgreSQL container and API project reference
- covers: [AC-019]
- files:
  - src/Host/Program.cs
  - src/Host/appsettings.json
  - src/Host/Properties/launchSettings.json
- depends_on: [T-007]
- verification: `dotnet run --project src/Host/CatCar.AppHost.csproj` starts PostgreSQL container, API, and Aspire Dashboard. The API waits for PostgreSQL health. Aspire Dashboard is accessible. `docker ps` shows the Postgres container running.
- effort: medium
- status: done

## T-009: Implement automatic EF Core migrations at startup in Development
- covers: [AC-015]
- files:
  - src/Api/Program.cs
  - tests/E2E/MigrationTests.cs
- depends_on: [T-008]
- verification: Start the AppHost. Check PostgreSQL: all 4 BC schemas exist with `__ef_migrations_history` tables. Migrations are applied only in Development environment. `dotnet test tests/E2E/ --filter MigrationTests` exits 0 with all tests passing.
- effort: small
- **RED:** Write a failing integration test in `tests/E2E/MigrationTests.cs` using Testcontainers PostgreSQL: verify that starting the API in Development environment automatically applies EF Core migrations for all 4 BC DbContexts, creating the expected schemas and `__ef_migrations_history` tables. Verify that starting in Production environment does NOT apply migrations automatically. Run `dotnet test tests/E2E/ --filter MigrationTests` — confirm tests fail (Red).
- **GREEN:** Add migration execution logic to `Program.cs` after `builder.Build()`: in Development environment, iterate over all registered `DbContext` types and call `Database.MigrateAsync()`. Run `dotnet test tests/E2E/ --filter MigrationTests` — confirm tests pass (Green).
- **REFACTOR:** Review migration logic for clarity, ensure it only runs in Development, add logging for migration progress. Run `dotnet format` and `dotnet test` — confirm no regressions.
- status: done

## T-010: Create test project structure with IntegrationTestBase, NetArchTest rules, and coverage configuration
- covers: [AC-006, AC-023, AC-024, AC-025]
- files:
  - tests/Architecture.Tests/CrossContextIsolationTests.cs
  - tests/Architecture.Tests/DependencyDirectionTests.cs
  - tests/Architecture.Tests/VerticalSliceStructureTests.cs
  - tests/Architecture.Tests/SharedKernelPurityTests.cs
  - tests/Architecture.Tests/GlobalUsings.cs
  - tests/Contexts/ServiceOperations.Tests/Integration/IntegrationTestBase.cs
  - tests/Contexts/ServiceOperations.Tests/GlobalUsings.cs
  - tests/Contexts/CatalogInventory.Tests/Integration/IntegrationTestBase.cs
  - tests/Contexts/CatalogInventory.Tests/GlobalUsings.cs
  - tests/Contexts/Communication.Tests/Integration/IntegrationTestBase.cs
  - tests/Contexts/Communication.Tests/GlobalUsings.cs
  - tests/Contexts/IdentityAccess.Tests/Integration/IntegrationTestBase.cs
  - tests/Contexts/IdentityAccess.Tests/GlobalUsings.cs
  - tests/E2E/GlobalUsings.cs
  - Directory.Build.props
  - .github/workflows/ci.yml
- depends_on: [T-009]
- verification: `dotnet test tests/Architecture.Tests/` exits 0 with all NetArchTest rules passing: no cross-BC imports (CON-001), no EF Core in Domain OR Features folders (CON-002), SharedKernel purity (CON-003), BC folder structure (CON-004), Vertical Slice structure (CON-005). `IntegrationTestBase` compiles and can start/stop a Testcontainers PostgreSQL instance. `dotnet test --collect:"XPlat Code Coverage"` generates Cobertura XML reports. `Directory.Build.props` contains coverage configuration (coverlet.collector settings, output format, coverage thresholds). `rg -n "Atendimento|CatalogoEstoque|Comunicacao|Identidade" src tests --glob "*.cs" --glob "*.csproj"` returns no matches.
- effort: large
- constraints: [CON-001, CON-002, CON-003, CON-004, CON-005, CON-013]
- status: done

## T-011: Configure code quality tooling (.editorconfig, Roslyn analyzers, dotnet format)
- covers: [AC-026, AC-027]
- files:
  - .editorconfig
  - src/Directory.Build.props
  - tests/Directory.Build.props
- depends_on: [T-010]
- verification: `dotnet format --verify-no-changes` exits 0 on the entire solution. `.editorconfig` enforces file-scoped namespaces, var preferences, brace style. StyleCop analyzers are configured.
- effort: small
- status: done

## T-012: Create multi-stage Dockerfile and docker-compose.yml
- covers: [AC-028, AC-029]
- files:
  - Dockerfile
  - docker-compose.yml
- depends_on: [T-011]
- verification: `docker build -t catcar:test .` exits 0. `docker-compose up -d` starts PostgreSQL and API. `curl http://localhost:8080/health/live` returns 200. The container runs as non-root user.
- effort: medium
- status: done

## T-013: Create GitHub Actions CI pipeline
- covers: [AC-030, AC-031]
- files:
  - .github/workflows/ci.yml
- depends_on: [T-012]
- verification: Push to a branch and open a PR. The CI pipeline runs: restore, build, format check, unit tests, architecture tests, integration tests, coverage report, SAST, Docker build, Trivy scan. All steps pass (green). A deliberate format violation causes the pipeline to fail.
- effort: medium
- status: done

## T-014: Write README.md with setup instructions
- covers: [AC-032]
- files:
  - README.md
- depends_on: [T-013]
- verification: README.md contains: prerequisites (.NET 10 SDK, Docker), Aspire AppHost setup instructions, docker-compose alternative, links to SwaggerUI and Aspire Dashboard, project structure overview.
- effort: small
- status: done
