# Repository Guidelines

CatCar is a back-end modular monolith for car repair shop management built on .NET 10 LTS. It manages work orders, customers, vehicles, budgets, cataloged services, parts/inventory control, customer notifications, and administrative authentication.

---

## Project Overview

CatCar enforces Domain-Driven Design (DDD) and Vertical Slice Architecture within a Modular Monolith structure. The system consists of four Bounded Contexts (BCs):

- **ServiceOperations** (Core): Work Orders (`WorkOrder`), Budgets (`Budget`), Customers (`Customer`), and Vehicles (`Vehicle`).
- **CatalogInventory** (Supporting): Cataloged services (`CatalogedService`) and Inventory stock items (`InventoryItem`).
- **Communication** (Supporting): Customer notifications and single-use external access tokens (`ExternalAccessToken`).
- **IdentityAccess** (Generic): Administrative users (`AdministrativeUser`), roles, JWT authentication, and Argon2id password hashing.

---

## Architecture & Data Flow

```
                               ┌────────────────────────────────────────┐
                               │           CatCar.Api (HTTP)            │
                               └───────────────────┬────────────────────┘
                                                   │
      ┌───────────────────────────┬────────────────┴───────────┬───────────────────────────┐
      ▼                           ▼                            ▼                           ▼
┌───────────┐             ┌───────────────┐           ┌─────────────────┐         ┌────────────────┐
│ Identity  │             │ ServiceOps    │           │ CatalogInvent.  │         │ Communication  │
│ Access BC │             │ (Core BC)     │           │ BC              │         │ BC             │
└─────┬─────┘             └───────┬───────┘           └────────┬────────┘         └───────┬────────┘
      │                           │                            │                          │
      ▼                           ▼                            ▼                          ▼
┌───────────┐             ┌───────────────┐           ┌─────────────────┐         ┌────────────────┐
│ Schema:   │             │ Schema:       │           │ Schema:         │         │ Schema:        │
│ identity_ │             │ service_      │           │ catalog_        │         │ communication  │
│ access    │             │ operations    │           │ inventory       │         │                │
└───────────┘             └───────────────┘           └─────────────────┘         └────────────────┘
```

### Key Architectural Rules

1. **Modular Boundary Enforcement**: Each Bounded Context is an isolated C# project under `src/Contexts/`. Projects must **never** reference another Bounded Context directly.
2. **Cross-BC Communication**: Communication across contexts is strictly asynchronous, using serialized integration events (`IIntegrationEvent`) defined in `src/Contracts/` delivered through Wolverine and the PostgreSQL Outbox pattern (`outbox_messages`).
3. **Database Isolation**: EF Core is configured with **one DbContext per Bounded Context**. Each context maps to its own PostgreSQL database schema (`service_operations`, `catalog_inventory`, `communication`, `identity_access`).
4. **Vertical Slices**: Inside each BC, use cases are organized by feature folders under `Features/<UseCase>/` containing `{Command,Handler,Validator,Endpoint}.cs`. No technical root folders (`Services/`, `Controllers/`, `Repositories/`) are allowed.
5. **Tactical Shared Kernel**: `src/SharedKernel` contains only pure tactical DDD primitives (`Entity<TId>`, `ValueObject`, `IAggregateRoot`, `IDomainEvent`, `IIntegrationEvent`, `BusinessRuleViolatedException`). It must **never** contain domain business logic or external package dependencies.

---

## Key Directories

```
CatCar.slnx                       # Canonical solution manifest (.NET 10)
Directory.Build.props             # Global MSBuild properties and code-style settings
Directory.Packages.props          # Central Package Management (CPM) definitions
dotnet-tools.json                 # Local .NET tools manifest (dotnet-ef, dotnet-sonarscanner)

src/
├── SharedKernel/                 # Tactical DDD base classes & primitives
├── Contracts/                    # Published Language integration events and cross-BC contracts
├── Contexts/
│   ├── ServiceOperations/        # Work orders, budgets, customers, vehicles
│   ├── CatalogInventory/         # Parts catalog, stock items, stock movements
│   ├── Communication/            # Notifications, external client tokens
│   └── IdentityAccess/           # Auth, admin users, roles, JWT & Argon2id
├── Api/                          # Composition Root: Minimal APIs, Serilog, Wolverine, OpenAPI
└── Host/
    ├── CatCar.AppHost/           # .NET Aspire AppHost orchestration model
    └── CatCar.ServiceDefaults/   # Shared OpenTelemetry, health checks, resilience policies

tests/
├── SharedKernel.Tests/           # Tactical DDD primitive unit tests
├── Architecture.Tests/           # NetArchTest structural enforcement suite
├── Contexts/                     # Unit and integration tests per Bounded Context
└── E2E/                          # Aspire AppHost End-to-End integration tests
```

---

## Development Commands

### Building & Running

```bash
# Restore local .NET tools (dotnet-ef 10.0.12, dotnet-sonarscanner 11.3.0)
dotnet tool restore

# Restore dependencies for the canonical solution
dotnet restore CatCar.slnx

# Build the solution in Release mode
dotnet build CatCar.slnx -c Release

# Run local development environment via Aspire AppHost (PostgreSQL + API + Aspire Dashboard)
dotnet run --project src/Host/CatCar.AppHost/CatCar.AppHost.csproj

# Run local development environment via Docker Compose
docker compose up -d

# Build production Docker image locally
docker build -t catcar:ci .
```

### Testing & Quality Assurance

```bash
# Run all unit, architecture, integration, and E2E tests
dotnet test CatCar.slnx --no-build -c Release

# Filter test execution by context or layer
dotnet test CatCar.slnx -c Release --filter "FullyQualifiedName~Architecture"
dotnet test CatCar.slnx -c Release --filter "FullyQualifiedName~ServiceOperations"

# Run tests with OpenCover code coverage output (matches CI)
dotnet test CatCar.slnx --no-build -c Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults/Coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# Verify C# code formatting without making changes
dotnet format CatCar.slnx --verify-no-changes
```

---

## Code Conventions & Common Patterns

### Language & Documentation Policy

- **English Only**: All code, identifiers, comments, log messages, unit test names, and error keys **must be in English**.
- **Ubiquitous Language Mapping**: Portuguese domain terms from business requirements translate to standardized English identifiers in code:
  - `OrdemDeServico` → `WorkOrder`
  - `Orcamento` → `Budget`
  - `Cliente` → `Customer`
  - `Veiculo` → `Vehicle`
  - `Peca/Item` → `InventoryItem`
  - `Servico` → `CatalogedService`
  - `UsuarioAdministrativo` → `AdministrativeUser`

### Naming Conventions

| Element | Format | Example |
|---|---|---|
| Feature Folders | `PascalCase` | `src/Contexts/ServiceOperations/Features/OpenWorkOrder/` |
| Feature Handlers | `<Feature>Handler` | `OpenWorkOrderHandler` |
| Commands / Queries | `<Feature>Command` / `<Feature>Query` | `OpenWorkOrderCommand` |
| Minimal API Endpoints | `Map<Feature>Endpoint` | `MapOpenWorkOrderEndpoint` |
| Integration Events | `<Noun><PastTenseAction>IntegrationEvent` | `WorkOrderBudgetApprovedIntegrationEvent` |
| Test Methods | `MethodName_StateUnderTest_ExpectedBehavior` | `Handle_WithValidCommand_ShouldCreateWorkOrder` |

### Error Handling & Result Pattern

- Aggregate root state mutations return `Upshot` or `Upshot<T>` (`RiseOn.ResultRail`).
- Do **not** throw exceptions for ordinary validation or domain rule failures. Use `Upshot.Fail(...)`.
- Throw `BusinessRuleViolatedException` only when an internal domain invariant is violated unexpectedly.
- Minimal API endpoints map `Upshot.Failure` to RFC 7807 `ProblemDetails` via ASP.NET Core `Results.Problem(...)`.

### Dependency Injection Pattern

- Services use `RiseOn.AutoInject` attributes:
  ```csharp
  [InjectService(ServiceLifetime.Scoped, CollectionName = "ServiceOperations")]
  public class WorkOrderRepository : IWorkOrderRepository { ... }
  ```
- Each Bounded Context exposes an `IServiceCollection` extension method (`AddServiceOperationsContext(configuration)`) to register its infrastructure dependencies and DbContext.

### Async & Concurrency Rules

- All I/O operations must be `async` and accept a `CancellationToken`.
- Append `.ConfigureAwait(false)` on internal handler and library `await` calls.
- Optimistic concurrency is enforced on PostgreSQL via `xmin` shadow property mapping in EF Core DbContexts.

---

## Important Files

- `CatCar.slnx`: Canonical solution file grouping all 15 projects.
- `Directory.Packages.props`: Central Package Management (CPM) manifest — all NuGet package versions are pinned here.
- `Directory.Build.props`: Shared MSBuild settings (`net10.0`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
- `dotnet-tools.json`: Configures pinned local tools `dotnet-ef` and `dotnet-sonarscanner`.
- `src/Api/Program.cs`: ASP.NET Core Minimal API composition root, Serilog setup, Wolverine discovery, health check mapping, OpenAPI/Scalar UI.
- `src/Host/CatCar.AppHost/AppHost.cs`: .NET Aspire orchestration specification for PostgreSQL container and API instance.
- `.github/workflows/ci.yml`: GitHub Actions pipeline running build/test, SonarQube Cloud Quality Gate, Opengrep SAST, structured NuGet SCA, and Trivy image scanning.

---

## Runtime & Tooling Preferences

- **Target Framework**: .NET 10.0 (`net10.0`), C# 14.
- **Package Management**: Central Package Management (`Directory.Packages.props`). Do **not** add `Version="..."` attributes to `<PackageReference>` items in individual `.csproj` files.
- **Containerization**: Multi-stage `Dockerfile` executing as non-root user `appuser:appgroup` on port `8080`.
- **API Documentation**: Scalar UI is accessible at `/scalar/v1` and OpenAPI specification at `/openapi/v1.json` during `Development`.

---

## Testing & QA Expectations

- **Framework Stack**: xUnit (`2.9.3`), FluentAssertions (`8.11.0`), NSubstitute (`6.2.0`), `Testcontainers.PostgreSql` (`4.15.0`), `NetArchTest.Rules` (`1.3.2`).
- **Domain Coverage Expectation**: Maintain **>= 80% line coverage** across critical domain classes (`SharedKernel` and `Contexts/*/Domain`).
- **Architecture Enforcement**: `Architecture.Tests` runs NetArchTest rules to automatically fail builds if:
  - Any Bounded Context imports another Bounded Context's namespace.
  - Domain projects depend on EF Core or Infrastructure assemblies.
  - `SharedKernel` references business domain code.
