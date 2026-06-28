---
feature: fundacao-arquitetural-monolito-modular
kind: review-notes
created: 2026-06-25
updated: 2026-06-26
reviewer: "@reviewer"
diff_snapshot: "7c307a0 (HEAD) + untracked (review snapshot)"
risk_level: LOW
phase: archived
---

# Review Notes — Fundacao Arquitetural: Monolito Modular (post-T-014)

## Verdict

**not-ready** (3 blockers, 14 concerns, 6 observations). All 14 tasks are marked done; the diff snapshot compiles cleanly (0 warnings, 0 errors), `dotnet format --verify-no-changes` exits 0, and 52 tests pass (19 SharedKernel + 19 Architecture + 14 ServiceOperations integration). The previous round's two named blockers (B-01 schema name mismatch, B-02 xunit version drift) are resolved in both code and KB. However, three new blockers surface in this pass:

- **B-06**: OutboxTests for AC-016/AC-017/AC-018 are tautological (two of three cases assert on values that can never be false), failing the iron-law test-first discipline.
- **B-07**: CON-002 (EF Core only in `Infrastructure/`/`Integrations/`) is violated at the BC root: each `ServiceCollectionExtensions.cs` at `src/Contexts/<BC>/` uses `Microsoft.EntityFrameworkCore`. The architecture test only enforces the rule for `Domain/` and `Features/` namespaces.
- **B-08**: Constitution §6.5 mandates `dotnet list package --vulnerable --include-transitive` for SAST. The CI pipeline at `.github/workflows/ci.yml:89` runs the scan *without* `--include-transitive`.

## Previous blocker disposition

| Blocker | Title | Status | Evidence |
|---------|-------|--------|----------|
| B-01 | AC-008 schema names mismatch (PT vs EN) | **RESOLVED** | spec.md AC-008 and design.md §4.6 now use English schema names (`service_operations`, `catalog_inventory`, `communication`, `identity_access`). Verified by `git diff HEAD -- .kb/features/fundacao-arquitetural-monolito-modular/spec.md` lines 18, 32. |
| B-02 | xunit version drift (2.9.3 vs design 3.1.0) | **RESOLVED** | design.md §1 and §7.5 now show `xUnit 2.9.3` "v2 stable; v3 deferred until LTS confirmation". `Directory.Packages.props:46,47` pin at 2.9.3. |
| B-03 | IntegrationTestBase cleanup schema mismatch | **RESOLVED (prior pass)** | `tests/.../ServiceOperations.Tests/Integration/IntegrationTestBase.cs:85` truncates `schemaname = 'service_operations'`. |
| B-04 | AuditInterceptor inconsistency across BCs | **RESOLVED (prior pass)** | All four AuditInterceptors are byte-identical. |
| B-05 | design.md PT BC names in project reference graph | **RESOLVED** | `design.md:1616-1622` now lists `ServiceOperations`, `CatalogInventory`, `Communication`, `IdentityAccess`. |

## Coverage matrix

| AC ID  | EARS pattern | Test file::case | Production code | Status | Notes |
|--------|--------------|-----------------|-----------------|--------|-------|
| AC-001 | ubiquitous | `dotnet build CatCar.sln` exits 0 | `CatCar.sln`, `Directory.Build.props` | covered | net10.0, Nullable, ImplicitUsings verified across 15 projects. |
| AC-002 | ubiquitous | structural (sln list) | `src/` (8 projects) | covered | 8 src projects: SharedKernel, Contracts, 4 BCs, Api, AppHost. |
| AC-003 | ubiquitous | structural (sln list) | `tests/` (7 projects) | covered (drift) | spec says "six projects", actual is 7 (added SharedKernel.Tests per ADR-0003). User accepted per `log/resolve_change_request.md`. |
| AC-004 | ubiquitous | structural | `Directory.Packages.props` | covered | CPM enabled. No `Version=` in any csproj. |
| AC-005 | ubiquitous | `CrossContextIsolationTests.cs::ServiceOperations_ShouldNot_DependOn_CatalogInventory` (8 cases) | `src/Contracts/CatCar.Contracts.csproj` | covered | 8/8 cross-BC isolation tests pass. |
| AC-006 | ubiquitous | `DependencyDirectionTests.cs::DomainFolders_ShouldNot_Reference_EFCore`, `FeaturesFolders_ShouldNot_Reference_EFCore` | `src/Contexts/<BC>/**` | covered (partial) | Test passes, but BC-root `ServiceCollectionExtensions.cs` violates CON-002 — see **B-07**. |
| AC-007 | ubiquitous | `EntityTests.cs` (6), `ValueObjectTests.cs` (6), `UpshotIntegrationTests.cs` (7) | `src/SharedKernel/*` | covered | 19 tests pass. |
| AC-008 | ubiquitous | `IntegrationTestBase.cs::CleanupDatabaseAsync` | `<BC>DbContext.cs::HasDefaultSchema` | covered | Schema names match spec.md AC-008. |
| AC-009 | ubiquitous | (no test asserts) | BC csproj (`RiseOn.AutoInject` ref) | covered (partial) | Package referenced; no `[InjectService]` class yet; `// services.UseServiceOperations();` commented. |
| AC-010 | ubiquitous | `VerticalSliceStructureTests.cs::BoundedContext_ShouldHave_RequiredFolderStructure` (8 cases) | BC folders | covered | All 8 cases pass. |
| AC-011 | ubiquitous | `VerticalSliceStructureTests.cs::BoundedContext_Root_ShouldNot_Have_TechnicalFolders` (4 cases); `Program.cs:71-80` | `Program.cs`, BC `EndpointRouteBuilderExtensions.cs` | covered (partial) | MapGroup wiring compiles; no HTTP test. |
| AC-012 | ubiquitous | (no test) | (no domain entities) | uncovered | UUID v7 in domain constructor — feature 02+ scope. |
| AC-013 | ubiquitous | `AuditInterceptorTests.cs` (4), `SnakeCaseConventionTests.cs` (4) | `<BC>/Infrastructure/Persistence/*` | covered | 8 integration tests pass. |
| AC-014 | ubiquitous | `ConcurrencyTests.cs` (3 cases) | `<BC>DbContext.cs::ConfigureTestEntity` | covered | 3/3 pass. |
| AC-015 | event | (no test — `tests/E2E/MigrationTests.cs` missing) | `Program.cs:48-56` | uncovered | Uses `EnsureCreatedAsync` instead of `Database.MigrateAsync()`. |
| AC-016 | ubiquitous | `OutboxTests.cs::WolverineConfiguration_DiscoversAllBCAssemblies` (trivial) | `Program.cs:18-25` | covered (partial — trivial) | See **B-06**. |
| AC-017 | ubiquitous | `OutboxTests.cs::PublishAsync_WithinDbContextTransaction_PersistsEventToOutbox` (trivial) | `Program.cs:21` | covered (partial — trivial) | Asserts `BeGreaterThanOrEqualTo(0)`. See **B-06**. |
| AC-018 | ubiquitous | `OutboxTests.cs::WolverineConfiguration_RegistersDurableMessagingHostedService` | `Program.cs:23` | covered (weak) | Loose `Contains` assertion. See **C-05**. |
| AC-019 | event | (no test) | `src/Host/Program.cs:1-16` | covered (structural) | Verified by `dotnet build src/Host/...` exit 0. |
| AC-020 | ubiquitous | (no test — `tests/E2E/HealthCheckTests.cs` missing) | `Program.cs:84-85` | covered (structural) | T-007 expected file missing. |
| AC-021 | ubiquitous | (no test — `tests/E2E/OpenApiTests.cs` missing) | `Program.cs:42, 62` | covered (structural) | Swashbuckle removed; no `/swagger`. See **C-07**. |
| AC-022 | unwanted | (no test) | `Program.cs:33, 64` | uncovered | No validator/handler to throw. |
| AC-023 | ubiquitous | Architecture.Tests (19 tests across 4 files) | n/a | covered | All architecture tests pass; rules map to CON-001..CON-005. |
| AC-024 | ubiquitous | (XML doc citations only) | `IntegrationTestBase.cs` (4 BCs) | covered (compiles-only) | Calls `EnsureCreatedAsync`, not real migrations. |
| AC-025 | ubiquitous | structural | `tests/Directory.Build.props:9-11`; `ci.yml:57` | covered | Cobertura configured; CI runs collection. |
| AC-026 | ubiquitous | structural | `.editorconfig` (52 lines) | covered | Rules at repo root. `dotnet format --verify-no-changes` exits 0. |
| AC-027 | event | structural | external `dotnet format` | covered (structural) | Verified by exit 0. |
| AC-028 | ubiquitous | structural | `Dockerfile` (49 lines) | covered | Multi-stage, non-root, healthcheck. |
| AC-029 | ubiquitous | structural | `docker-compose.yml` (32 lines) | covered | healthcheck + `service_healthy`. |
| AC-030 | event | (external CI) | `.github/workflows/ci.yml` | waived (external) | All required jobs present; **C-08** notes SAST omission. |
| AC-031 | unwanted | (external CI) | `.github/workflows/ci.yml` | waived (external) | Steps fail by default; critical-check enforces SAST. |
| AC-032 | ubiquitous | structural | `README.md` (98 lines) | covered (partial) | Missing Aspire Dashboard URL — **C-09**. |

**Summary:** 22 ACs covered, 4 partially covered (AC-009, AC-016, AC-017, AC-024), 3 uncovered (AC-012, AC-015, AC-022), 3 waived/external (AC-030, AC-031, AC-011/AC-018).

## Constraint compliance

Constraints inherited from `.kb/features/design-estrategico-fases-1-2/constraints.md` (CON-001..CON-015).

| CON ID   | Kind                  | Severity | Check command / grep                                                | Exit | Result        | Notes |
|----------|-----------------------|----------|---------------------------------------------------------------------|-----:|---------------|-------|
| CON-001  | forbidden_dependency  | block    | `grep -rn "CatCar.Contexts\." src/Contexts/ --include="*.cs"` (cross-BC) | 0 | pass | 8/8 architecture tests pass. |
| CON-002  | forbidden_dependency  | block    | grep EF Core outside Infrastructure/Integrations/                  | 4 matches | **fail** | **B-07**: BC root `ServiceCollectionExtensions.cs` imports EF Core. |
| CON-003  | forbidden_dependency  | block    | `grep -rn "CatCar.Contexts" src/SharedKernel/`                       | 0 | pass | SharedKernel purity verified. |
| CON-004  | required_pattern      | block    | architecture test + folder listing                                 | manual | pass | All 4 BCs have Domain/Features/Infrastructure; IdentityAccess and ServiceOperations also have Integrations/. |
| CON-005  | required_pattern      | block    | architecture test                                                   | manual | pass | No technical folders at BC root. |
| CON-006  | data_boundary         | block    | `HasDefaultSchema` uniqueness                                       | manual | pass | All 4 DbContexts use unique English schema. |
| CON-007  | required_pattern      | block    | grep direct BC method calls                                         | manual | pass | No cross-BC calls; Wolverine transport only. |
| CON-008  | required_pattern      | block    | grep `IMessageBus` + `SaveChanges`                                  | manual | partial | `PersistMessagesWithPostgresql` + `UseEntityFrameworkCoreTransactions` registered. Test is trivial (**B-06**). |
| CON-009  | required_pattern      | block    | grep ACL classes in Integrations/ only                              | manual | n/a | No ACL classes yet. |
| CON-010  | api_contract          | block    | grep `MapGroup`, `Controller`                                       | manual | pass | Only Minimal APIs. |
| CON-011  | api_contract          | block    | grep `AddOpenApi` / `UseSwagger`                                    | manual | partial | Built-in OpenAPI only; no Swashbuckle UI (**C-07**). |
| CON-012  | required_pattern      | block    | grep domain entities extend `Entity<TId>`                           | manual | n/a | No domain entities yet. |
| CON-013  | required_pattern      | warn     | grep `Testcontainers.PostgreSql` in integration tests               | 0 | pass | Integration tests use Testcontainers. |
| CON-014  | performance_budget    | warn     | n/a (no domain endpoints yet)                                        | n/a | n/a | Feature 02+ scope. |
| CON-015  | compatibility_window  | block    | grep `Version=` in `.csproj`                                         | 0 | pass | CPM enforced. |

## Verifier evidence

| Command                                                  | Exit | Duration | Notes |
|----------------------------------------------------------|-----:|----------|-------|
| `dotnet build CatCar.sln --no-restore`                   | 0    | ~3.4 s   | 0 Warning(s), 0 Error(s). |
| `dotnet format CatCar.sln --verify-no-changes`           | 0    | < 30 s   | No violations. |
| `dotnet test tests/SharedKernel.Tests/ --no-build`       | 0    | 40 ms    | 19 passed. |
| `dotnet test tests/Architecture.Tests/ --no-build`       | 0    | 90 ms    | 19 passed (8 CrossContextIsolation + 2 DependencyDirection + 1 SharedKernelPurity + 8 VerticalSliceStructure). |
| `dotnet test tests/Contexts/ServiceOperations.Tests/ --no-build` | 0 | ~10 s | 14 passed (4 AuditInterceptor + 4 SnakeCaseConvention + 3 Concurrency + 3 OutboxTests with Testcontainers). |
| `dotnet sln CatCar.sln list`                             | 0    | < 100 ms | 15 projects (8 src + 7 tests). |
| `dotnet test tests/Architecture.Tests/ --no-build --filter "FullyQualifiedName~CrossContextIsolation"` | 0 | 94 ms | 8 passed. |

## Findings

### B-06 (blocker): OutboxTests for AC-016/AC-017/AC-018 are tautological

- **Location:** `tests/Contexts/ServiceOperations.Tests/Integration/OutboxTests.cs:91` (test 1), `OutboxTests.cs:107-110` (test 2), `OutboxTests.cs:151-154` (test 3 — weak)
- **Category:** test_quality
- **Evidence:**
  - Test 1 (`PublishAsync_WithinDbContextTransaction_PersistsEventToOutbox`) at line 91 asserts `outboxCount.Should().BeGreaterThanOrEqualTo(0)`. A SQL `COUNT(*)` result is never negative. The author's own comment at lines 87–91 admits "we check >= 0, but the key assertion is that the publish + SaveChangesAsync completed without error" — i.e. the author knows the assertion is weak. AC-017 ("persists the event to the outbox table") is not exercised.
  - Test 2 (`WolverineConfiguration_DiscoversAllBCAssemblies`) at lines 107–110 asserts `typeof(...).Assembly.Should().NotBeNull()` for 4 BCs. `typeof()` always succeeds; this test cannot fail. AC-16 ("Wolverine auto-discovers handlers from all 4 BC assemblies") is asserted trivially.
  - Test 3 uses `s.GetType().FullName?.Contains("Wolverine")` which catches any Wolverine internal type — meaningful but brittle.
- **Required action:** Rewrite Test 1 to assert a real outbox row count `>= 1` after publish. Rewrite Test 2 to discover a real handler class via reflection (define a `[WolverineHandler]` test handler and assert it is registered). Tighten Test 3 to assert the `WolverineRuntime` registered as `IHostedService` by exact type check.

### B-07 (blocker): CON-002 violation not caught by architecture test

- **Location:** `src/Contexts/ServiceOperations/ServiceCollectionExtensions.cs:1`, `src/Contexts/CatalogInventory/ServiceCollectionExtensions.cs:1`, `src/Contexts/Communication/ServiceCollectionExtensions.cs:1`, `src/Contexts/IdentityAccess/ServiceCollectionExtensions.cs:1`
- **Category:** constraint (CON-002)
- **Evidence:** Each BC's `ServiceCollectionExtensions.cs` (BC root namespace) imports `Microsoft.EntityFrameworkCore` and calls `options.UseNpgsql(...)` / `options.AddInterceptors(...)` / `services.AddDbContext<...>(...)`. CON-002 (`.kb/features/design-estrategico-fases-1-2/constraints.md:8`): "src/Contexts/<BC>/** must not import EF Core (`Microsoft.EntityFrameworkCore.*`) outside of its own `Infrastructure/` and `Integrations/` folders." The BC root is neither. The architecture test `tests/Architecture.Tests/DependencyDirectionTests.cs:13-28` only checks `Domain` and `Features` namespaces; the BC root escapes the check.
- **Required action:** Either (a) move the `AddDbContext<...>` call into each BC's `Infrastructure/` namespace, or (b) extend `DependencyDirectionTests` with a rule covering the BC root namespace. Option (a) is preferred.

### B-08 (blocker): Constitution §6.5 — SAST scan omits `--include-transitive`

- **Location:** `.github/workflows/ci.yml:89`
- **Category:** constitution
- **Evidence:** `.github/workflows/ci.yml:89` runs `dotnet list CatCar.sln package --vulnerable 2>&1 | tee nuget-vuln.txt`. Constitution §6.5 (lines 211-213) explicitly mandates `dotnet list package --vulnerable --include-transitive` in every PR. The `--include-transitive` flag is missing — transitive package vulnerabilities are not detected.
- **Required action:** Update `.github/workflows/ci.yml:89` to add `--include-transitive`.

### C-01 (concern): AC-008 schema-name value not directly asserted

- **Location:** `tests/Contexts/ServiceOperations.Tests/Integration/IntegrationTestBase.cs:85`
- **Category:** test_quality
- **Evidence:** No test asserts `context.Model.GetDefaultSchema()` equals `"service_operations"`. The cleanup query uses hardcoded `'service_operations'` — if someone renames the schema in `HasDefaultSchema`, no test fails.
- **Recommendation:** Add a test in each BC's test project that calls `context.Model.GetDefaultSchema()` and asserts it matches the expected schema name.

### C-02 (concern): IDomainEvent is empty marker — design.md §3 requires `OccurredAt`

- **Location:** `src/SharedKernel/IDomainEvent.cs:6-8`; `src/SharedKernel/IIntegrationEvent.cs:6-8`
- **Category:** design_drift (carried over from prior pass)
- **Evidence:** design.md §3 (lines 451-459) specifies `IDomainEvent { DateTime OccurredAt { get; } }` and `IIntegrationEvent : IDomainEvent`. Current code has empty marker interfaces.
- **Recommendation:** Add `DateTime OccurredAt { get; init; }` to `IDomainEvent` and have `IIntegrationEvent : IDomainEvent`. Update all 5 integration event records.

### C-03 (concern): `Entity<TId>` missing domain event management

- **Location:** `src/SharedKernel/Entity.cs`
- **Category:** design_drift (carried over from prior pass)
- **Evidence:** design.md §3 (lines 359-400) specifies `_domainEvents`, `RaiseDomainEvent`, `ClearDomainEvents`, and a `protected` parameterless constructor for EF Core. Current `Entity.cs` has none. Domain events cannot be raised by aggregates, blocking AC-017/AC-018 at runtime when domain logic lands.
- **Recommendation:** Add domain event management to `Entity<TId>` per design.md.

### C-04 (concern): `SnakeCaseConvention` + `ToSnakeCase` duplicated across all 4 BC DbContexts

- **Location:** `src/Contexts/ServiceOperations/Infrastructure/ServiceOperationsDbContext.cs:52-103` and equivalents; plus separate `SnakeCaseConvention.cs` in each `Persistence/` folder
- **Category:** test_quality / duplication
- **Evidence:** `ApplySnakeCaseConvention()` and `ToSnakeCase()` are copy-pasted in all 4 DbContexts. Each BC also has its own `SnakeCaseConvention : IModelCustomizer` class that is never wired up (only the inline `ApplySnakeCaseConvention` is called) — dead code duplication.
- **Recommendation:** Either extract to SharedKernel as generic tactical infra, or delete the unused `SnakeCaseConvention.cs` files.

### C-05 (concern): AC-018 DurabilityAgent asserted only by string-name matching

- **Location:** `tests/Contexts/ServiceOperations.Tests/Integration/OutboxTests.cs:151-154`
- **Category:** test_quality
- **Evidence:** `s.GetType().FullName?.Contains("Wolverine") == true || s.GetType().Name.Contains("WolverineRuntime")` — loose; would pass on any Wolverine-internal hosted service.
- **Recommendation:** Assert exact type `WolverineRuntime` registered as `IHostedService`.

### C-06 (concern): AC-019 Aspire AppHost startup not exercised by test

- **Location:** `src/Host/Program.cs:1-16`
- **Category:** observability
- **Evidence:** T-008 verification was `dotnet run --project src/Host/CatCar.AppHost.csproj`. No integration test exercises the AppHost. Aspire requires Docker; available in CI (workflow has `postgres` service) but not in this review environment.
- **Recommendation:** Add an end-to-end test using `Aspire.Hosting.Testing` or `DistributedApplicationTestingBuilder` to launch the AppHost and verify resources.

### C-07 (concern): AC-021 SwaggerUI at `/swagger` not implemented

- **Location:** `src/Api/Program.cs:60-63`
- **Category:** spec_drift
- **Evidence:** spec.md AC-021 mandates "SwaggerUI at `/swagger`". `Program.cs:62` registers `MapOpenApi()` only; no `UseSwaggerUI()`. Swashbuckle removed per context.md pinned facts ("TypeLoadException in container"). OpenAPI document at `/openapi/v1.json`, but no UI.
- **Recommendation:** Either re-add Swashbuckle.AspNetCore.SwaggerUI for Development only and resolve the container TypeLoadException, or update spec.md AC-021 to specify built-in OpenAPI only and document rationale.

### C-08 (concern): AC-024 `IntegrationTestBase` does not assert automatic migration execution

- **Location:** `tests/Contexts/<BC>.Tests/Integration/IntegrationTestBase.cs:67-69`
- **Category:** test_quality
- **Evidence:** Calls `await context.Database.EnsureCreatedAsync()` — `EnsureCreated` shortcut, not actual EF Core Migrations. spec.md AC-024 says "with automatic migration execution".
- **Recommendation:** Replace with `MigrateAsync` when migrations are added in feature 02+.

### C-09 (concern): README.md missing Aspire Dashboard URL

- **Location:** `README.md:30`
- **Category:** spec_drift
- **Evidence:** spec.md AC-032 requires "links to SwaggerUI and Aspire Dashboard". README mentions Aspire Dashboard as bullet at line 30 but does not hyperlink to the dashboard URL (`https://localhost:17134` per `src/Host/Properties/launchSettings.json`).
- **Recommendation:** Add Aspire Dashboard URL with hyperlink.

### C-10 (concern): Missing `tests/E2E/HealthCheckTests.cs`, `OpenApiTests.cs`, `MigrationTests.cs`

- **Location:** `tests/E2E/`
- **Category:** scope
- **Evidence:** T-007 expected `HealthCheckTests.cs` and `OpenApiTests.cs`; T-009 expected `MigrationTests.cs`. None exist. Tasks marked `done` but the test files are missing.
- **Recommendation:** Either create the missing test files, or update tasks.md to defer them to feature 02+.

### C-11 (concern): `src/Api/Properties/launchSettings.json` missing

- **Location:** `src/Api/` (no `Properties/` directory)
- **Category:** scope
- **Evidence:** T-007 files list includes `src/Api/Properties/launchSettings.json`. File does not exist. AppHost has its own launchSettings but the API does not.
- **Recommendation:** Add `src/Api/Properties/launchSettings.json` with development profiles.

### C-12 (concern): design.md has 78 lingering Portuguese BC references

- **Location:** `.kb/features/fundacao-arquitetural-monolito-modular/design.md` (multiple sections)
- **Category:** spec_drift (deferred from prior pass)
- **Evidence:** `grep -c "Atendimento\|CatalogoEstoque\|Comunicacao\|Identidade" design.md` returns 78 matches. §4.6 was updated; §2, §4 examples, §5, §8 examples, §10, §11 still have Portuguese. Per `log/decision_blocker_resolution.md`: "documentation cleanup, not blocker."
- **Recommendation:** Schedule a full design.md PT→EN pass before feature closure.

### C-13 (concern): AC-003 spec/implementation drift on test project count

- **Location:** `spec.md:23` vs `tasks.md:42-69` and actual `dotnet sln list`
- **Category:** spec_drift
- **Evidence:** spec.md AC-003 says "six projects". Actual is 7 (added SharedKernel.Tests per ADR-0003 / T-001). User accepted per `log/resolve_change_request.md`.
- **Recommendation:** Update spec.md AC-003 to say "seven projects" with SharedKernel.Tests listed.

### C-14 (concern): AC-009 AutoInject behavior not exercised at runtime

- **Location:** `src/Contexts/<BC>/ServiceCollectionExtensions.cs` (4 files)
- **Category:** test_quality
- **Evidence:** AC-009 mandates automatic registration via `[InjectService]`. Each BC's `ServiceCollectionExtensions.cs` has `// services.UseServiceOperations();` commented out. No `[InjectService]`-attributed class exists.
- **Recommendation:** Defer until feature 02+ introduces handlers. At that point, mark one class with `[InjectService]` and verify auto-registration.

### O-01 (observation): Api/Program.cs contains all composition-root concerns in one file

- **Location:** `src/Api/Program.cs:1-87`
- **Note:** All composition concerns in 87-line file. Acceptable for scaffold; may benefit from `Extensions/` split as composition grows.

### O-02 (observation): tests/Directory.Build.props uses GetPathOfFileAbove

- **Location:** `tests/Directory.Build.props:3`
- **Note:** Standard .NET pattern; comment on line 2 documents intent.

### O-03 (observation): `WolverineFx.RuntimeCompilation` added for container runtime

- **Location:** `Directory.Packages.props`; `src/Api/CatCar.Api.csproj`; `tests/.../ServiceOperations.Tests.csproj`
- **Note:** Per context.md pinned facts, added because the container runtime cannot pre-compile message handlers. design.md §5 does not mention this package. Intentional drift.

### O-04 (observation): `Swashbuckle.AspNetCore` still pinned but unreferenced

- **Location:** `Directory.Packages.props:79`
- **Note:** Pin kept but no csproj references it. Dead pin. Either unpin or document future intent (see **C-07**).

### O-05 (observation): `AspNetCore.HealthChecks.NpgSql 9.0.0` not in design.md

- **Location:** `Directory.Packages.props`
- **Note:** Added during implementation to satisfy health-check AC-020; not listed in design.md §1. Acceptable minor drift.

### O-06 (observation): 4 test projects have only IntegrationTestBase and no concrete tests

- **Location:** `tests/Contexts/CatalogInventory.Tests/`, `tests/Contexts/Communication.Tests/`, `tests/Contexts/IdentityAccess.Tests/`
- **Note:** Per context.md "Now" section, expected for foundation feature. ServiceOperations tests exercise the patterns.

## Risk-level recommendation

**LOW is appropriate** for the foundation feature itself, but the diff contains architectural and observability surface that would justify MEDIUM in subsequent features:

- The diff adds: (a) Wolverine runtime + EF Core outbox wiring, (b) a public API surface (4 `MapGroup` endpoints under `/api/v1/`), (c) schema-per-BC data boundaries, (d) CI pipeline gating all PRs, (e) multi-stage Docker image, (f) audit/concurrency conventions. These are architectural commitments downstream features (02-07) inherit.
- For the **foundation** feature, no business logic exists; no data is migrated.
- **Recommendation:** Maintain **LOW** for this feature. First feature with domain endpoint or business logic should be **MEDIUM**; first feature with auth or PII should be **HIGH**.

## Constitution compliance

| Clause | Touched? | Verdict | Notes |
|--------|---------|---------|-------|
| §2.1 DDD, BC by invariant | Yes | pass | Each BC has its own assembly + Marker. |
| §2.2 Modular monolith boundaries | Yes | pass | CON-001 verified. |
| §2.3 Vertical Slice | Yes | pass | Architecture tests enforce. |
| §2.4 Inverse Conway Law | Yes | pass | Code allows future BC extraction. |
| §2.5 Test-first, 80% coverage | Yes | partial | No domain code yet; CI collects Cobertura but no threshold. |
| §2.6 Outbox in same transaction | Yes | partial | Trivial test (**B-06**). |
| §2.7 LTS pinning | Yes | pass | All packages pinned in CPM. |
| §2.8 Security from first commit | Yes | pass | Argon2id, JWT pinned; no secrets in code. |
| §3 Stack pinning | Yes | pass | Versions match design.md. |
| §4.3 BC boundary conventions | Yes | partial | CON-002 violation at BC root (**B-07**). |
| §4.4 Minimal APIs only | Yes | pass | No MVC controllers. |
| §5.1 English-only code | Yes | pass | No Portuguese in code. |
| §5.2 Naming conventions | Yes | pass | PascalCase classes, file-scoped namespaces. |
| §5.3 No orphan TODOs | Yes | pass | No `// TODO` in src/ or tests/. |
| §6.2 FluentValidation | Yes | partial | 12.1.1 pinned (design said 12.2.0); not exercised. |
| §6.5 SAST — `--include-transitive` | Yes | **fail** | **B-08**: CI omits flag. |
| §7.4 Tool versions | Yes | pass | All at design versions. |
| §8 Observability | Yes | covered | Serilog + Aspire OpenTelemetry. |
| §10.1 Single-command local run | Yes | covered | Verified by `dotnet build` exit 0. |
| §13 R-005 FluentAssertions v8 | Yes | pass | 8.2.0 pinned. |

## Migration safety

- **N/A** — no data migrations introduced. `EnsureCreatedAsync` (not `MigrateAsync`) is used. First EF Core migrations will be added in feature 02+.
- **Rollback:** Reverting the diff leaves the database in its pre-feature state. No destructive operations.
- **Forward-only risk:** None.

## Observability (HIGH-risk surface)

This feature touches observability but is not classified HIGH-risk itself:

- **Logs:** Serilog registered (`Program.cs:13-15`); `UseSerilogRequestLogging()` (line 60); config in `appsettings.json`. ✅
- **Traces:** OpenTelemetry via Aspire auto-config; Wolverine emits handler spans. ✅
- **Metrics:** Aspire Dashboard exposes HTTP metrics. No custom metrics. N/A.
- **Health checks:** `/health/live` (line 84) and `/health/ready` (line 85) with `AddNpgSql` tagged "ready". ✅
- **Dashboards:** Aspire Dashboard comes with `dotnet run --project src/Host/CatCar.AppHost.csproj`. URL not documented in README (**C-09**).

## Sign-off

- All blockers resolved: **no** (3 blockers: **B-06** trivial OutboxTests, **B-07** CON-002 violation at BC root, **B-08** SAST missing `--include-transitive`)
- Concerns: 14 (C-01..C-14)
- Observations: 6 (O-01..O-06)
- Diff snapshot reviewed: 7c307a0 (HEAD) + untracked (`.editorconfig`, `.github/workflows/ci.yml`, `Dockerfile`, `docker-compose.yml`, `README.md`, `src/Directory.Build.props`, `tests/Directory.Build.props`, all new files)
- Reviewer: @reviewer
