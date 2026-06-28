---
feature: fix-aspire-tests-build
kind: tasks
status: accepted
created: 2026-06-26
updated: 2026-06-27
phase: archived
---

# Tasks — Fix: Aspire Configuration, Build Errors, and Test Infrastructure

## T-001: Fix Central Package Management — add Aspire 13.4.6 and ServiceDefaults packages to Directory.Packages.props
- covers: [AC-034, AC-037]
- files:
  - Directory.Packages.props
- depends_on: []
- verification: `dotnet build CatCar.slnx` no longer produces NU1008 or NU1009 errors. All Aspire packages pinned at 13.4.6. ServiceDefaults packages (Microsoft.Extensions.Http.Resilience, Microsoft.Extensions.ServiceDiscovery, OpenTelemetry.*) present with pinned versions. `Aspire.Hosting.Testing` at 13.4.6 is present. The `Aspire.Hosting.AppHost` entry is removed (it is implicitly provided by the SDK).
- effort: small
- status: done
- note: Directory.Packages.props updated correctly. Full solution build verification deferred to T-010 because T-003/T-004 must first remove inline Version attributes from AppHost and ServiceDefaults .csproj files.

## T-002: Move AppHost and ServiceDefaults to canonical src/Host/ location
- covers: [AC-035]
- files:
  - src/Host/CatCar.AppHost/CatCar.AppHost.csproj (moved from src/Infrastructure/CatCar.AppHost/)
  - src/Host/CatCar.AppHost/AppHost.cs (moved)
  - src/Host/CatCar.AppHost/appsettings.json (moved)
  - src/Host/CatCar.AppHost/appsettings.Development.json (moved)
  - src/Host/CatCar.AppHost/Properties/launchSettings.json (moved)
  - src/Host/CatCar.ServiceDefaults/CatCar.ServiceDefaults.csproj (moved from src/Infrastructure/CatCar.ServiceDefaults/)
  - src/Host/CatCar.ServiceDefaults/Extensions.cs (moved)
  - src/Host/aspire.config.json (moved from src/Infrastructure/)
  - CatCar.slnx
- depends_on: [T-001]
- verification: `dotnet build CatCar.slnx` exits 0. `src/Host/CatCar.AppHost/` and `src/Host/CatCar.ServiceDefaults/` exist with all files. `src/Infrastructure/` no longer contains AppHost or ServiceDefaults. `CatCar.slnx` has a `/Host/` solution folder (not `/Infrastructure/`) containing both projects. `aspire.config.json` is at `src/Host/aspire.config.json` with the AppHost path updated to `CatCar.AppHost/CatCar.AppHost.csproj`.
- effort: medium
- constraints: [CON-016]
- status: done
- note: Files moved successfully. Build verification deferred to T-010 because inline versions and phantom references remain to be fixed by T-003/T-004.

## T-003: Clean up AppHost.csproj — remove phantom project references and inline versions
- covers: [AC-034, AC-036]
- files:
  - src/Host/CatCar.AppHost/CatCar.AppHost.csproj
- depends_on: [T-002]
- verification: `dotnet build src/Host/CatCar.AppHost/CatCar.AppHost.csproj` exits 0. No references to `CatCar.ApiService` or `CatCar.Web` remain. No `Aspire.Hosting.Redis` PackageReference. No inline `Version` attributes on any PackageReference. The `Aspire.AppHost.Sdk` version in the Sdk attribute matches 13.4.6.
- effort: small
- status: done
- note: Also added `Aspire.Hosting.PostgreSQL` PackageReference (required by AppHost.cs). Full AppHost build deferred to T-005 which fixes AppHost.cs compilation errors.

## T-004: Fix ServiceDefaults.csproj — move inline versions to Directory.Packages.props
- covers: [AC-034]
- files:
  - src/Host/CatCar.ServiceDefaults/CatCar.ServiceDefaults.csproj
- depends_on: [T-002]
- verification: `dotnet build src/Host/CatCar.ServiceDefaults/CatCar.ServiceDefaults.csproj` exits 0. No `Version` attributes on any PackageReference — all versions resolved from Directory.Packages.props.
- effort: small
- status: done
- note: Also required fixing OpenTelemetry package versions in Directory.Packages.props (1.16.0 → 1.15.x) and removing an unused using in Extensions.cs to satisfy StyleCop.

## T-005: Fix AppHost.cs — correct the API project type reference and remove scaffold code
- covers: [AC-036, AC-038]
- files:
  - src/Host/CatCar.AppHost/AppHost.cs
- depends_on: [T-003]
- verification: `dotnet build src/Host/CatCar.AppHost/CatCar.AppHost.csproj` exits 0. The `AddProject` call uses the correct Aspire-generated type reference for the CatCar.Api project. No commented-out scaffold code remains. The AppHost references only the API project and PostgreSQL container.
- effort: small
- status: done

## T-006: Wire API project to Aspire ServiceDefaults
- covers: [AC-038, AC-039, AC-040]
- files:
  - src/Api/CatCar.Api.csproj
  - src/Api/Program.cs
- depends_on: [T-004, T-005]
- verification: `dotnet build CatCar.slnx` exits 0. `CatCar.Api.csproj` references `CatCar.ServiceDefaults.csproj`. `Program.cs` calls `builder.AddServiceDefaults()` before `builder.Build()` and `app.MapDefaultEndpoints()` after `builder.Build()`. Health endpoints `/health` and `/alive` are mapped via ServiceDefaults.
- effort: small
- status: done

## T-007: Fix ServiceOperations.Tests cross-BC reference isolation
- covers: [AC-041]
- files:
  - tests/Contexts/ServiceOperations.Tests/CatCar.Contexts.ServiceOperations.Tests.csproj
  - tests/Contexts/ServiceOperations.Tests/Integration/OutboxTests.cs
- depends_on: [T-006]
- verification: `dotnet build tests/Contexts/ServiceOperations.Tests/CatCar.Contexts.ServiceOperations.Tests.csproj` exits 0. The project references only `CatCar.Contexts.ServiceOperations.csproj` and `CatCar.Contracts.csproj` — no references to CatalogInventory, Communication, or IdentityAccess projects. `dotnet test tests/Architecture.Tests/ --filter CrossContextIsolation` exits 0.
- effort: small
- constraints: [CON-001]
- status: done
- note: Amended scope to include OutboxTests.cs to remove cross-BC assembly discovery references.

## T-008: Set up E2E test project with Aspire.Hosting.Testing
- covers: [AC-042, AC-043]
- files:
  - tests/E2E/CatCar.E2E.Tests.csproj
  - tests/E2E/GlobalUsings.cs
  - tests/E2E/AppHostFixture.cs
- depends_on: [T-006]
- verification: `dotnet build tests/E2E/CatCar.E2E.Tests.csproj` exits 0. The project references `Aspire.Hosting.Testing` and the AppHost project. `AppHostFixture` implements `IAsyncLifetime` using `DistributedApplicationTestingBuilder.CreateAsync` with proper disposal. `GlobalUsings.cs` includes `Aspire.Hosting.Testing` and `Xunit` usings.
- effort: medium
- status: done

## T-009: Write E2E tests verifying AppHost wiring
- covers: [AC-042, AC-043, AC-044]
- files:
  - tests/E2E/AppHostWiringTests.cs
- depends_on: [T-008]
- verification: `dotnet test tests/E2E/CatCar.E2E.Tests.csproj` exits 0. Tests verify: (1) the AppHost starts successfully and all resources reach healthy state; (2) the API `/health` endpoint returns 200 via `CreateHttpClient("api")`; (3) the PostgreSQL resource is running and reachable. Tests use `WaitForResourceHealthyAsync` before assertions.
- effort: medium
- status: done
- note: Tests use `ResourceNotifications.WaitForResourceAsync` with `KnownResourceStates.Running` instead of `WaitForResourceHealthyAsync` (not available in Aspire 13.4.6 API). Added `WithHttpEndpoint()` to AppHost.cs API project to make `CreateHttpClient` work.

## T-010: Verify full solution build and all tests pass
- covers: [AC-033, AC-038, AC-041, AC-042, AC-044, AC-045]
- files: []
- depends_on: [T-007, T-009]
- note: Verification-only task — no file changes. Confirms the entire solution compiles and all test suites pass after all fixes.
- verification: `dotnet build CatCar.slnx` exits 0 with zero errors and zero NU warnings. `dotnet test tests/Architecture.Tests/` exits 0. `dotnet test tests/Contexts/ServiceOperations.Tests/` exits 0. `dotnet test tests/E2E/CatCar.E2E.Tests.csproj` exits 0.
- effort: small
- status: done
