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
dotnet run --project src/Host/CatCar.AppHost/CatCar.AppHost.csproj
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
| `GET /scalar/v1` | Scalar API documentation (Development only) |
| `GET /openapi/v1.json` | OpenAPI document |
| `GET /api/v1/service-operations` | ServiceOperations BC |
| `GET /api/v1/catalog-inventory` | CatalogInventory BC |
| `GET /api/v1/communication` | Communication BC |
| `GET /api/v1/identity-access` | IdentityAccess BC |

## API Documentation (Scalar & OpenAPI)

When running in `Development` environment, interactive API documentation is available:

- **Scalar UI:** [http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1) — Interactive exploration and testing of all Bounded Context endpoints.
- **OpenAPI Specification:** [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json) — OpenAPI v3 JSON document.

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
dotnet build CatCar.slnx

# Run tests
dotnet test CatCar.slnx

# Format check
dotnet format CatCar.slnx --verify-no-changes

# Run with hot reload
dotnet watch --project src/Api/CatCar.Api.csproj
```

## CI/CD

CI pipeline runs on every PR to `main` and via `workflow_dispatch`:
- **Build & Test:** restore → SonarQube Cloud Begin → build → format check → unit/architecture tests → integration tests → OpenCover coverage → SonarQube Cloud End.
- **Opengrep SAST Scan:** static analysis for C# and security audit, producing JSON/SARIF reports.
- **Security Scan (SCA):** structured NuGet package vulnerability scan (`dotnet package list --vulnerable --format json`).
- **Docker Build & Scan:** container image build and Trivy vulnerability scan (`HIGH,CRITICAL`).

### Report Visibility & Security Integration

- **Actions Summaries & Artifacts:** Every workflow run publishes step summaries and downloadable 90-day reports (`opengrep-security-report`, `nuget-vulnerability-report`, `trivy-vulnerability-report`, `coverage-report`, `test-results`).
- **GitHub Code Scanning:** When GitHub Code Security is enabled, SARIF reports from Opengrep (category `opengrep`) and Trivy (category `trivy-image`) are automatically uploaded to the **Security → Code scanning** dashboard.
- **SonarQube Cloud:** Results are published to SonarQube Cloud. The pipeline requires the GitHub Actions secret `SONAR_TOKEN` and repository variables `SONAR_ORGANIZATION` and `SONAR_PROJECT_KEY`. CI intentionally blocks preflight if any of these three configuration values are missing.

See `.github/workflows/ci.yml`.
## License

Proprietary — all rights reserved.
