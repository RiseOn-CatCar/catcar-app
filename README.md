# CatCar

Back-end modular monolith for a car repair shop management system — work orders, customers, vehicles, and parts/inventory control.

## Architecture

- **DDD** with 4 Bounded Contexts: ServiceOperations, CatalogInventory, Communication, IdentityAccess
- **Vertical Slice Architecture** within each BC
- **Wolverine** for in-process messaging with PostgreSQL outbox
- **EF Core** with schema-per-BC, UUID v7, optimistic concurrency via xmin
- **RiseOn.AutoInject** for automatic service registration
- **RiseOn.ResultRail** for the Result pattern (Upshot<T>)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/products/docker-desktop) (for PostgreSQL and containerized deployment)

## Quick Start

### Option 1: Aspire AppHost (recommended for development)

```bash
dotnet run --project src/Host/CatCar.AppHost.csproj
```

This starts:
- PostgreSQL container
- CatCar API
- Aspire Dashboard (monitoring, logs, traces)

### Option 2: Docker Compose

```bash
docker compose up -d
```

This starts PostgreSQL and the CatCar API. The API will be available at `http://localhost:8080`.

## Endpoints

| Endpoint | Description |
|---|---|
| `GET /health/live` | Liveness probe |
| `GET /health/ready` | Readiness probe (includes PostgreSQL) |
| `GET /swagger` | SwaggerUI (Development only) |
| `GET /openapi/v1.json` | OpenAPI document |
| `GET /api/v1/service-operations` | ServiceOperations BC |
| `GET /api/v1/catalog-inventory` | CatalogInventory BC |
| `GET /api/v1/communication` | Communication BC |
| `GET /api/v1/identity-access` | IdentityAccess BC |

## Project Structure

```
src/
├── SharedKernel/          # Tactical DDD base classes (Entity, ValueObject, etc.)
├── Contracts/             # Integration events (cross-BC contracts)
├── Contexts/
│   ├── ServiceOperations/ # Work orders, budgets, service execution
│   ├── CatalogInventory/  # Parts, inventory, stock management
│   ├── Communication/     # Notifications, customer communication
│   └── IdentityAccess/    # Authentication, authorization, user management
├── Api/                   # Composition root (Minimal APIs, Wolverine, health checks)
└── Host/                  # Aspire AppHost (orchestration)

tests/
├── SharedKernel.Tests/    # Unit tests for base classes
├── Architecture.Tests/    # NetArchTest rules (cross-BC isolation, layer purity)
├── Contexts/              # Integration tests per BC
└── E2E/                   # End-to-end tests
```

## Development

```bash
# Build
dotnet build CatCar.sln

# Run tests
dotnet test CatCar.sln

# Format check
dotnet format CatCar.sln --verify-no-changes

# Run with hot reload
dotnet watch --project src/Api/CatCar.Api.csproj
```

## CI/CD

CI pipeline runs on every PR to `main`: restore → build → format check → unit/architecture tests → integration tests → coverage → SAST → Docker build → Trivy scan.

See `.github/workflows/ci.yml`.

## License

Proprietary — all rights reserved.
