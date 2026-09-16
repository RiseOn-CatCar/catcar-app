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

## Local Development

Start the API and PostgreSQL with Docker Compose:

```bash
docker compose up -d
```

The API is available at `http://localhost:8080`. Stop the local stack with
`docker compose down`.

## Kubernetes Deployment

The production workflow materializes `catcar-secret` from Azure Key Vault and
does not commit credentials. For a deployment with that Secret already created:

```bash
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/migration-job.yaml
kubectl wait --for=condition=complete job/catcar-db-migration --timeout=300s
kubectl apply -f k8s/deployment.yaml -f k8s/service.yaml -f k8s/hpa.yaml
kubectl rollout status deployment/catcar-api --timeout=300s
```

## Infrastructure as Code

Terraform in `infra/` provisions the Azure resource group, virtual network,
private ACR and PostgreSQL connectivity, AKS, PostgreSQL Flexible Server, and
Key Vault. Production deployment runs on a self-hosted runner within that VNet.
Copy the example values to a local ignored `terraform.tfvars` file and supply
strong secrets before provisioning:

```bash
cd infra
terraform init
terraform apply
```

The configured `azurerm` backend receives its storage settings at
initialization time, typically from CI backend configuration.

## Phase 2 Architecture

During Phase 2 / Wave 2, CatCar remains a Modular Monolith in a single
repository. Wave 3 will split resource state and ownership into four
independent repositories, as defined in the roadmap.

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

CI validates every pull request to `main`. Pushes to `main` additionally use
GitHub OIDC to provision and deploy the production environment:
- **Build & Test:** restore → build → format check → unit, architecture, integration, and E2E tests with OpenCover coverage.
- **Terraform:** provider initialization and validation on every change; remote-backend plan and apply for production pushes.
- **Container Delivery:** immutable API image build and push to Azure Container Registry.
- **Kubernetes Delivery:** Key Vault-backed manifest materialization, migration Job completion, `kubectl apply -f k8s/`, and rollout verification.
- **Security:** Opengrep SAST, NuGet SCA, and Trivy image scans.

### Report Visibility & Security Integration

- **Actions Summaries & Artifacts:** Every workflow run publishes step summaries and downloadable 90-day reports (`opengrep-security-report`, `nuget-vulnerability-report`, `trivy-vulnerability-report`, `coverage-report`, `test-results`).
- **GitHub Code Scanning:** When GitHub Code Security is enabled, SARIF reports from Opengrep (category `opengrep`) and Trivy (category `trivy-image`) are automatically uploaded to the **Security → Code scanning** dashboard.
- **SonarQube Cloud:** Results are published to SonarQube Cloud. The pipeline requires the GitHub Actions secret `SONAR_TOKEN` and repository variables `SONAR_ORGANIZATION` and `SONAR_PROJECT_KEY`. CI intentionally blocks preflight if any of these three configuration values are missing.

See `.github/workflows/ci.yml`.
## License

Proprietary — all rights reserved.
