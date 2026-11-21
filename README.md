# CatCar OS Service

The OS service owns customers, vehicles, work orders and administrative identity.
Its work-order saga coordinates budgeting and execution with the billing and
execution services through RabbitMQ. Catalog information is read from the
execution service over HTTP. The Aspire AppHost for the complete topology is
maintained in the platform repository.

## Configuration

- `ConnectionStrings:catcar`: PostgreSQL connection string for `catcar_os`.
- `ConnectionStrings:messaging`: RabbitMQ AMQP URI.
- `Services:Execution`: execution service base address; defaults to
  `https+http://execution-service` with Aspire service discovery.
- `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience` and `Jwt:ExpirationMinutes`:
  administrative JWT settings.
- `InternalApi:Key`: shared secret for the internal customer lookup. Missing or
  empty keys disable access; configure the same secret in the Auth Function.
- `BootstrapAdmin:Email` and `BootstrapAdmin:Password`: credentials used with
  `--migrate --bootstrap-admin`.
- `Saga:ApprovalTimeout` and `Saga:PaymentTimeout`: saga timeout durations.

Never commit real credentials. Database migrations can be applied by running
the API with `--migrate`; the Kubernetes base includes a migration job.

## Development

Use the .NET SDK pinned in `global.json`. From this repository:

```bash
dotnet restore CatCar.slnx
dotnet build CatCar.slnx -c Release
dotnet test CatCar.slnx -c Release
dotnet format whitespace CatCar.slnx --verify-no-changes
docker compose up --build
```

The CI Kubernetes overlay contains ephemeral PostgreSQL and RabbitMQ instances:
`kubectl apply -k k8s/overlays/ci`. It is for testing only.

## Endpoints

- `/api/v1/service-operations`: customers, vehicles and work orders.
- `/api/v1/identity-access`: administrative authentication and authorization.
- `GET /internal/v1/customers/by-document/{documentNumber}`: Auth Function
  customer authentication lookup, protected by `X-Internal-Api-Key`. Returns
  only the customer ID and active status instead of permitting direct database access.
- `/health/live` and `/health/ready`: liveness and readiness probes.
- `/docs` and `/openapi/v1.json`: API documentation in Development and
  LocalKubernetes environments.

The phase 4 messaging contracts and cross-service architecture are documented
in the platform repository at `docs/architecture/messaging-contracts.md`.
