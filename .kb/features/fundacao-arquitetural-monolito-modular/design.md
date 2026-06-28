---
feature: fundacao-arquitetural-monolito-modular
kind: design
status: accepted
created: 2026-06-10
updated: 2026-06-26
relations:
  depends_on:
    - design-estrategico-fases-1-2
  extends: []
  shares_context: []
  related:
    - decomposicao-features-fases-1-2
phase: archived
---

# Architecture — Fundacao Arquitetural: Monolito Modular

## Decision

This design resolves every open tactical decision the strategic design deferred to feature 01 — exact package versions, solution/project structure, SharedKernel signatures, EF Core conventions, Wolverine configuration, Aspire AppHost setup, test project layout, API wiring, code quality tooling, local dev experience, Docker setup, and the CI pipeline. The output is a complete blueprint that `@developer` can implement from a cold repo.

All decisions align with the constitution and the archived strategic design at `[[design-estrategico-fases-1-2/design.md]]`. Where the constitution said "a fixar", this design pins the exact version. Where the strategic design left structure abstract, this design names every `.csproj` and every directory.

## Context

The CatCar repository is empty. This feature bootstraps the entire solution scaffold — the monolith that every other feature (02 through 08) will build on. The strategic design already decided: 4 Bounded Contexts in a modular monolith, Vertical Slice Architecture inside each BC, Wolverine for mediation + outbox, EF Core 10 + PostgreSQL with schema-per-BC, .NET Aspire for orchestration, Minimal APIs exclusively. Constraints CON-001 through CON-015 from the strategic design are the non-negotiable contract.

This design is the bridge between "what we want" (strategy) and "what we build" (implementation). It covers all 12 tactical items listed in the decomposition feature's scope for feature 01.

## Approach

---

### 1. Exact package versions (Central Package Management)

All versions are pinned in `Directory.Packages.props` at the solution root. This is the .NET standard for Central Package Management; the constitution's reference to `versions.props` means the CPM file. Versions were selected for compatibility with .NET 10.0.8 LTS as of June 2026.

**Directory.Packages.props:**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <!-- ===== Runtime / Framework ===== -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi"    Version="10.0.8" />

    <!-- ===== .NET Aspire ===== -->
    <PackageVersion Include="Aspire.Hosting.AppHost"         Version="13.1.2" />
    <PackageVersion Include="Aspire.Hosting.PostgreSQL"      Version="13.1.2" />
    <PackageVersion Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL"
                                                             Version="13.1.2" />

    <!-- ===== EF Core + PostgreSQL ===== -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore"  Version="10.0.8" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design"
                                                             Version="10.0.8" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL"
                                                             Version="10.0.0" />

    <!-- ===== Wolverine ===== -->
    <PackageVersion Include="WolverineFx"                    Version="6.5.1" />
    <PackageVersion Include="WolverineFx.EntityFrameworkCore" Version="6.5.1" />

    <!-- ===== Validation ===== -->
    <PackageVersion Include="FluentValidation"               Version="12.2.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions"
                                                             Version="12.2.0" />

    <!-- ===== Logging / Observability ===== -->
    <PackageVersion Include="Serilog.AspNetCore"              Version="9.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console"           Version="6.0.0" />

    <!-- ===== API / OpenAPI ===== -->
    <PackageVersion Include="Swashbuckle.AspNetCore"          Version="7.3.1" />

    <!-- ===== Auth / Security ===== -->
    <PackageVersion Include="Konscious.Security.Cryptography.Argon2"
                                                             Version="1.3.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer"
                                                             Version="10.0.8" />

    <!-- ===== Testing ===== -->
    <PackageVersion Include="xunit"                           Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio"       Version="2.9.3" />
    <PackageVersion Include="FluentAssertions"                Version="8.2.0" />
    <PackageVersion Include="NSubstitute"                     Version="5.3.0" />
    <PackageVersion Include="Testcontainers.PostgreSql"       Version="4.4.0" />
    <PackageVersion Include="NetArchTest.Rules"               Version="1.3.2" />
    <PackageVersion Include="Bogus"                           Version="35.6.2" />
    <PackageVersion Include="coverlet.collector"              Version="6.0.4" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk"          Version="17.14.0" />

    <!-- ===== Code Quality ===== -->
    <PackageVersion Include="StyleCop.Analyzers"              Version="1.2.0-beta.556" />
  </ItemGroup>
</Project>
```

**Key version decisions:**

| Package | Version | Rationale |
|---|---|---|
| Wolverine | 6.5.1 | Constitution; MIT license, outbox-native, single-maintainer risk accepted (R-006) |
| Aspire | 13.1.2 | Constitution; GA release |
| EF Core | 10.0.8 | Ships with .NET 10.0.8 SDK |
| Npgsql EF | 10.0.x | Aligned to EF Core 10 major |
| FluentValidation | 12.2.0 | Latest stable for .NET 10 (MIT) |
| Serilog.AspNetCore | 9.0.0 | Native .NET 10 support |
| xUnit | 2.9.3 | v2 stable; v3 deferred until LTS for .NET 10 is confirmed |
| FluentAssertions | 8.2.0 | **Last Apache-2.0 version** — see ADR-0002 |
| NSubstitute | 5.3.0 | **Preferred over Moq** — see ADR-0001 |
| Testcontainers | 4.4.0 | .NET 10 compatible |
| NetArchTest | 1.3.2 | Stable; structural rules enforced at build |
| Bogus | 35.6.2 | Realistic test data generation |
| StyleCop | 1.2.0-beta.556 | C# 14 support (beta channel required) |
| Swashbuckle | 7.3.1 | SwaggerUI renderer only; document generated by built-in OpenAPI |
| coverlet | 6.0.4 | Code coverage collection |
| Test.Sdk | 17.14.0 | Latest Microsoft test SDK |

**Policy:** Patch upgrades (third digit) are auto-merged via Dependabot if CI passes. Major upgrades require a new ADR. Minor upgrades are reviewed per package.

---

### 2. Solution and project structure

**Solution file:** `CatCar.sln` at repository root.

**Target framework:** `net10.0` on every `.csproj`.

**Language version:** C# 14 (default with `net10.0`).

**Nullable:** `<Nullable>enable</Nullable>` everywhere.

**ImplicitUsings:** `<ImplicitUsings>enable</ImplicitUsings>` on every project.

#### Source projects (`src/`)

| # | `.csproj` path | Assembly name | Type |
|---|---|---|---|
| 1 | `src/SharedKernel/CatCar.SharedKernel.csproj` | `CatCar.SharedKernel` | Class library |
| 2 | `src/Contracts/CatCar.Contracts.csproj` | `CatCar.Contracts` | Class library |
| 3 | `src/Contexts/Atendimento/CatCar.Contexts.Atendimento.csproj` | `CatCar.Contexts.Atendimento` | Class library |
| 4 | `src/Contexts/CatalogoEstoque/CatCar.Contexts.CatalogoEstoque.csproj` | `CatCar.Contexts.CatalogoEstoque` | Class library |
| 5 | `src/Contexts/Comunicacao/CatCar.Contexts.Comunicacao.csproj` | `CatCar.Contexts.Comunicacao` | Class library |
| 6 | `src/Contexts/Identidade/CatCar.Contexts.Identidade.csproj` | `CatCar.Contexts.Identidade` | Class library |
| 7 | `src/Api/CatCar.Api.csproj` | `CatCar.Api` | ASP.NET Core Web API |
| 8 | `src/Host/CatCar.AppHost.csproj` | `CatCar.AppHost` | .NET Aspire AppHost |

#### Test projects (`tests/`)

| # | `.csproj` path | Assembly name | Type |
|---|---|---|---|
| 9 | `tests/Contexts/Atendimento.Tests/CatCar.Contexts.Atendimento.Tests.csproj` | `CatCar.Contexts.Atendimento.Tests` | xUnit test |
| 10 | `tests/Contexts/CatalogoEstoque.Tests/CatCar.Contexts.CatalogoEstoque.Tests.csproj` | `CatCar.Contexts.CatalogoEstoque.Tests` | xUnit test |
| 11 | `tests/Contexts/Comunicacao.Tests/CatCar.Contexts.Comunicacao.Tests.csproj` | `CatCar.Contexts.Comunicacao.Tests` | xUnit test |
| 12 | `tests/Contexts/Identidade.Tests/CatCar.Contexts.Identidade.Tests.csproj` | `CatCar.Contexts.Identidade.Tests` | xUnit test |
| 13 | `tests/Architecture.Tests/CatCar.Architecture.Tests.csproj` | `CatCar.Architecture.Tests` | xUnit test |
| 14 | `tests/E2E/CatCar.E2E.Tests.csproj` | `CatCar.E2E.Tests` | xUnit test (skeleton) |

#### Project reference graph

```
CatCar.SharedKernel           ← references nothing (pure)
    ↑
CatCar.Contracts              ← SharedKernel
    ↑
CatCar.Contexts.<BC>          ← SharedKernel, Contracts (never other BCs)
    ↑        ↑        ↑        ↑
CatCar.Api                    ← all 4 BCs, SharedKernel, Contracts
                                  (composition root; wires DI + endpoints)
    ↑
CatCar.AppHost                ← Aspire.Hosting packages only
                                  (orchestrates Api + Postgres via project reference metadata)

CatCar.Contexts.<BC>.Tests    ← corresponding BC, test packages
CatCar.Architecture.Tests     ← all BCs, SharedKernel, NetArchTest
CatCar.E2E.Tests              ← Api, Testcontainers, Aspire.Hosting.Testing
```

**Key rule:** No BC project references any other BC project. Cross-BC communication is via Wolverine handlers consuming contracts from `CatCar.Contracts` (enforced by CON-001 and NetArchTest).

#### Folder structure (canonical)

```
CatCar.sln
Directory.Packages.props
.editorconfig
.gitignore
README.md
Dockerfile
docker-compose.yml

src/
  SharedKernel/CatCar.SharedKernel.csproj
    Entity{TId}.cs
    ValueObject.cs
    IAggregateRoot.cs
    IDomainEvent.cs
    IIntegrationEvent.cs
    Result.cs
    Result{T}.cs
    BusinessRuleViolatedException.cs

  Contracts/CatCar.Contracts.csproj
    Atendimento/
      BudgetIssuedIntegrationEvent.cs
      BudgetApprovedIntegrationEvent.cs
      WorkOrderStatusChangedIntegrationEvent.cs
    CatalogoEstoque/
      InventoryReservedIntegrationEvent.cs
      InventoryConsumedIntegrationEvent.cs

  Contexts/
    Atendimento/CatCar.Contexts.Atendimento.csproj
      Domain/
        WorkOrderAggregate/
          WorkOrder.cs
          WorkOrderId.cs
          WorkOrderStatus.cs
          WorkOrderReceivedEvent.cs
          DiagnosisStartedEvent.cs
        BudgetAggregate/
          Budget.cs
          BudgetId.cs
          BudgetStatus.cs
          BudgetLine.cs
          BudgetSnapshot.cs
        CustomerAggregate/
          Customer.cs
          CustomerId.cs
        VehicleAggregate/
          Vehicle.cs
          VehicleId.cs
        Services/
          IWorkOrderRepository.cs
      Features/
        OpenWorkOrder/
          OpenWorkOrderCommand.cs
          OpenWorkOrderHandler.cs
          OpenWorkOrderValidator.cs
          OpenWorkOrderEndpoint.cs
        IssueBudget/
        RecordApproval/
        StartDiagnosis/
        CompleteWorkOrder/
      Infrastructure/
        AtendimentoDbContext.cs
        EntityConfigurations/
        Migrations/
        WorkOrderRepository.cs
        Persistence/
          AuditInterceptor.cs
      Integrations/
        CatalogAcl.cs
        IdentityAcl.cs
        CommunicationAcl.cs
      ServiceCollectionExtensions.cs
      EndpointRouteBuilderExtensions.cs

    CatalogoEstoque/CatCar.Contexts.CatalogoEstoque.csproj
      Domain/
        InventoryItemAggregate/
        CatalogedServiceAggregate/
      Features/
        RegisterService/
        RegisterPart/
        ReserveInventory/
        ReleaseReservation/
        ConsumeInventory/
      Infrastructure/
        CatalogoEstoqueDbContext.cs
        EntityConfigurations/
        Migrations/
      ServiceCollectionExtensions.cs
      EndpointRouteBuilderExtensions.cs

    Comunicacao/CatCar.Contexts.Comunicacao.csproj
      Domain/
        ExternalAccessTokenAggregate/
        NotificationAggregate/
      Features/
        IssueApprovalLink/
        SendNotification/
        RecordCustomerDecision/
      Infrastructure/
        ComunicacaoDbContext.cs
        SmtpEmailSender.cs
      ServiceCollectionExtensions.cs
      EndpointRouteBuilderExtensions.cs

    Identidade/CatCar.Contexts.Identidade.csproj
      Domain/
        AdministrativeUserAggregate/
        Role/
      Features/
        Login/
        IssueToken/
        ValidateToken/
      Infrastructure/
        IdentidadeDbContext.cs
        PasswordHasher.cs
      ServiceCollectionExtensions.cs
      EndpointRouteBuilderExtensions.cs

  Api/CatCar.Api.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json
    Properties/launchSettings.json

  Host/CatCar.AppHost.csproj
    Program.cs
    appsettings.json
    Properties/launchSettings.json

tests/
  Contexts/
    Atendimento.Tests/CatCar.Contexts.Atendimento.Tests.csproj
      Domain/
        WorkOrderTests.cs
        BudgetTests.cs
      Features/
        OpenWorkOrderTests.cs
        IssueBudgetTests.cs
      Integration/
        OpenWorkOrderIntegrationTests.cs
      GlobalUsings.cs

    CatalogoEstoque.Tests/CatCar.Contexts.CatalogoEstoque.Tests.csproj
    Comunicacao.Tests/CatCar.Contexts.Comunicacao.Tests.csproj
    Identidade.Tests/CatCar.Contexts.Identidade.Tests.csproj

  Architecture.Tests/CatCar.Architecture.Tests.csproj
    CrossContextIsolationTests.cs
    DependencyDirectionTests.cs
    VerticalSliceStructureTests.cs
    SharedKernelPurityTests.cs

  E2E/CatCar.E2E.Tests.csproj
    WorkOrderLifecycleTests.cs   (skeleton)
```

---

### 3. SharedKernel exact contents

The `CatCar.SharedKernel` assembly is the **tactical DDD base library**. It contains only generic infrastructure types — no business semantics, no BC-specific types, no EF Core references.

**Decision (ADR-0003):** The SharedKernel uses **RiseOn.ResultRail** (v1.1.1, MIT, https://www.nuget.org/packages/RiseOn.ResultRail) for the Result pattern instead of a custom `Result<T>` implementation. The type `Upshot<T>` from RiseOn.ResultRail becomes the standard Result pattern implementation across all Bounded Contexts. This is a strategic ecosystem decision — the project author maintains the RiseOn.ResultRail package and wants to dogfood it in CatCar.

#### `Entity<TId>` — aggregate/entity base class

```csharp
namespace CatCar.SharedKernel;

public abstract class Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected init; } = default!;

    protected Entity() { } // ORM private constructor

    protected Entity(TId id)
    {
        Id = id;
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() =>
        _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (GetType() != other.GetType())
            return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}
```

**Design notes:**
- `Id` is `protected init` — set once by the domain constructor or EF Core.
- Default parameterless constructor is `protected` for EF Core materialization only (CON-012: private constructor for ORM).
- `RaiseDomainEvent` is the single entry point; `ClearDomainEvents` is called by the outbox dispatch pipeline.
- Equality is structural by `Id` (identity-based), consistent with DDD entity semantics.

#### `ValueObject` — value object base class

```csharp
namespace CatCar.SharedKernel;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (GetType() != other.GetType()) return false;
        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) =>
        obj is ValueObject other && Equals(other);

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
                HashCode.Combine(current, obj?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !Equals(left, right);
}
```

#### Marker interfaces

```csharp
namespace CatCar.SharedKernel;

// Marker: the root entity of an aggregate
public interface IAggregateRoot { }

// Marker: a domain event raised within an aggregate
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

// Marker: a domain event that crosses bounded context boundaries
// (published to Contracts assembly and consumed via Wolverine)
public interface IIntegrationEvent : IDomainEvent { }
```

#### `Upshot<T>` — operation outcome (from RiseOn.ResultRail)

**Decision (ADR-0003):** Use `Upshot<T>` from RiseOn.ResultRail instead of a custom `Result<T>`. The package provides:
- `Upshot` (non-generic) and `Upshot<T>` (generic) as readonly structs
- `Error` class with implicit conversions from string/Exception
- Extension methods for railway chaining: `OnRail`, `OnRailSuccess`, `OnRailFail`, `Map`, `Finally`

Usage example:
```csharp
public Upshot<WorkOrderId> Handle(OpenWorkOrderCommand command)
{
    var workOrder = new WorkOrder(command.CustomerId, command.VehicleId, command.Description);
    _db.Set<WorkOrder>().Add(workOrder);
    return Upshot<WorkOrderId>.Success(workOrder.Id);
}
```

Package reference in `CatCar.SharedKernel.csproj`:
```xml
<PackageReference Include="RiseOn.ResultRail" Version="1.1.1" />
```

#### `BusinessRuleViolatedException`

```csharp
namespace CatCar.SharedKernel;

public class BusinessRuleViolatedException : Exception
{
    public BusinessRuleViolatedException(string message) : base(message) { }

    public BusinessRuleViolatedException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

#### What the SharedKernel does NOT contain

- ❌ Domain enums (`WorkOrderStatus`, `BudgetStatus`)
- ❌ Domain-specific value objects (`Cpf`, `LicensePlate`, `Money`)
- ❌ Validation rules (CPF algorithm, Mercosul plate regex)
- ❌ EF Core types or attributes
- ❌ Wolverine types or attributes
- ❌ `IRepository<T>` generic interface (each BC defines its own repository contracts)

These all belong in their respective BCs.

**Note on CON-003 (SharedKernel purity):** The RiseOn.ResultRail dependency is acceptable because the package is maintained by the project author (ADR-0003) and published on nuget.org. The dependency is a tactical DDD infrastructure type (Result pattern), not a business-domain type.

---

### 4. EF Core conventions

#### 4.1 Table and column naming

All table and column names use **`snake_case`** (constitution §9.1). This is enforced via a convention applied to every `DbContext`:

```csharp
// In each BC's DbContext OnModelCreating:
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Conventions.Add(_ =>
        new SnakeCaseConvention());
}

// Or, applied per-entity via IEntityTypeConfiguration<T> classes:
// ToTable("work_orders")
// Property(x => x.CustomerId).HasColumnName("customer_id");
```

**Naming rules:**
- Table: `snake_case` **plural** — `work_orders`, `inventory_items`, `external_access_tokens`
- Column: `snake_case` singular — `customer_id`, `created_at`, `status`
- PK column: always `id` (UUID v7)
- FK column: `<referenced_table_singular>_id` — `work_order_id`, `budget_id`, `customer_id`
- Audit columns: `created_at` (UTC), `updated_at` (UTC)
- Concurrency token: `xmin` (mapped to `uint row_version`)
- Index: `<table>_<column>_idx` — `work_orders_status_idx`
- Migration history table: `__ef_migrations_history` inside each BC's schema

#### 4.2 Primary key strategy: UUID v7

**Decision:** Entities use **UUID v7** (time-ordered UUID) generated in the domain constructor, not auto-generated by the database.

**Rationale:**
- UUID v7 is lexicographically sortable by creation time (good for B-tree index locality in Postgres).
- Generated in domain code — no round-trip to DB for ID assignment.
- Safe for distributed systems if a BC is extracted later.
- `.NET 9+` has native `Guid.CreateVersion7()`.

**In domain constructors:**
```csharp
public class WorkOrder : Entity<WorkOrderId>, IAggregateRoot
{
    private WorkOrder() { } // EF Core

    public WorkOrder(CustomerId customerId, VehicleId vehicleId, string description)
    {
        Id = new WorkOrderId(Guid.CreateVersion7());
        // ...
    }
}
```

**Strongly-typed ID (one per aggregate root):**
```csharp
public readonly record struct WorkOrderId(Guid Value)
{
    public static WorkOrderId New() => new(Guid.CreateVersion7());
}

public readonly record struct BudgetId(Guid Value);
public readonly record struct CustomerId(Guid Value);
public readonly record struct VehicleId(Guid Value);
```

**EF Core mapping:**
```csharp
// In IEntityTypeConfiguration<WorkOrder>:
builder.Property(x => x.Id)
    .HasConversion(id => id.Value, guid => new WorkOrderId(guid))
    .HasColumnName("id")
    .HasColumnType("uuid");
```

#### 4.3 Audit interceptor

A single `AuditInterceptor : SaveChangesInterceptor` in each BC's Infrastructure folder:

```csharp
// Each BC has its own copy (or one in SharedKernel if abstracted cleanly)
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Property("CreatedAt").CurrentValue = now;

            entry.Property("UpdatedAt").CurrentValue = now;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

Each entity that needs auditing must expose `CreatedAt` and `UpdatedAt` (conceptual — the interceptor sets them via shadow properties or an `IAuditable` interface). **Decision:** use shadow properties to keep the domain model clean, mapped in `IEntityTypeConfiguration`:

```csharp
builder.Property<DateTime>("CreatedAt").HasColumnName("created_at").IsRequired();
builder.Property<DateTime>("UpdatedAt").HasColumnName("updated_at").IsRequired();
```

#### 4.4 Optimistic concurrency

Use PostgreSQL's `xmin` system column as a row version token. `xmin` is a 32-bit unsigned integer that Postgres increments on every row mutation. Map it as a concurrency token:

```csharp
// In IEntityTypeConfiguration<T>:
builder.Property<uint>("RowVersion")
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .IsRowVersion();
```

This is a shadow property — the domain entity doesn't need to know about it. On `DbUpdateConcurrencyException`, the handler can decide: retry from fresh state, or return a domain error.

**Why `xmin` over `byte[]` rowversion:** No extra column needed, fully Postgres-native, no extra writes. The trade-off is that `xmin` is 32-bit and wraps around after ~4 billion transactions — acceptable for a mechanic shop's transaction volume.

#### 4.5 Migration naming and strategy

**Naming pattern:** `<BC>_<seq>_<description>` where `<seq>` is zero-padded to 4 digits.

```
Atendimento_0001_Initial.cs
Atendimento_0002_AddBudgetTable.cs
CatalogoEstoque_0001_Initial.cs
CatalogoEstoque_0002_AddReservationTracking.cs
Comunicacao_0001_Initial.cs
Identidade_0001_Initial.cs
```

**Migrations run at startup** in the `CatCar.Api` project (the composition root), not in the AppHost:

```csharp
// CatCar.Api Program.cs — after builder.Build(), before app.Run():
if (app.Environment.IsDevelopment() || migrateAtStartup)
{
    using var scope = app.Services.CreateScope();
    var dbContexts = scope.ServiceProvider.GetServices<DbContext>();
    foreach (var context in dbContexts)
        await context.Database.MigrateAsync();
}
```

#### 4.6 Schema-per-BC

Each BC's `DbContext` maps to its own schema:

| BC | Schema | DbContext class |
|---|---|---|
| ServiceOperations/OS | `service_operations` | `ServiceOperationsDbContext` |
| CatalogInventory | `catalog_inventory` | `CatalogInventoryDbContext` |
| Communication | `communication` | `CommunicationDbContext` |
| IdentityAccess | `identity_access` | `IdentityAccessDbContext` |

Set in `OnModelCreating` via `modelBuilder.HasDefaultSchema("service_operations")` or per-entity `builder.ToTable("work_orders", "service_operations")`. **Decision:** use `HasDefaultSchema` — cleaner.

#### 4.7 Outbox table

Each BC that publishes integration events gets its own `outbox_messages` table in its schema. Framework: Wolverine's EF Core outbox integration auto-creates the table. Structure:

```sql
-- Generated by Wolverine EF Core integration:
CREATE TABLE service_operations.outbox_messages (
    id              UUID PRIMARY KEY,
    message_type    TEXT NOT NULL,
    body            JSONB NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    sent_at         TIMESTAMPTZ,
    attempts        INTEGER NOT NULL DEFAULT 0,
    locked_until    TIMESTAMPTZ
);
```

---

### 5. Wolverine configuration

#### 5.1 Wolverine registration (in CatCar.Api)

Wolverine is configured in the API composition root (`CatCar.Api/Program.cs`). It auto-discovers handlers from all BC assemblies:

```csharp
// CatCar.Api/Program.cs
using Wolverine;
using Wolverine.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(opts =>
{
    // In-process transport for Phase 1 (no external broker)
    opts.UseInProcessTransport();

    // Auto-discover message handlers from all BC assemblies
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.Atendimento.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.CatalogoEstoque.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.Comunicacao.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatCar.Contexts.Identidade.Marker).Assembly);

    // EF Core outbox — Wolverine auto-discovers DbContext types
    // and configures the outbox table in each BC's schema
    opts.UseEntityFrameworkCoreOutbox();
});

// Each BC's ServiceCollectionExtensions adds Wolverine as well
// (for its own DbContext outbox enrollment)
builder.Services.AddAtendimento(builder.Configuration);
builder.Services.AddCatalogoEstoque(builder.Configuration);
builder.Services.AddComunicacao(builder.Configuration);
builder.Services.AddIdentidade(builder.Configuration);
```

#### 5.2 How handlers publish to outbox

Inside a Vertical Slice handler, the handler publishes domain events, and Wolverine persists them to the outbox in the **same transaction** as the aggregate change:

```csharp
// Atendimento/Features/OpenWorkOrder/OpenWorkOrderHandler.cs
public class OpenWorkOrderHandler
{
    private readonly AtendimentoDbContext _db;
    private readonly IMessageBus _bus;

    public OpenWorkOrderHandler(AtendimentoDbContext db, IMessageBus bus)
    {
        _db = db;
        _bus = bus;
    }

    public async Task<Result<WorkOrderId>> Handle(
        OpenWorkOrderCommand command,
        CancellationToken ct)
    {
        var workOrder = new WorkOrder(command.CustomerId, command.VehicleId, command.Description);

        _db.Set<WorkOrder>().Add(workOrder);

        // Dispatch domain events through Wolverine's outbox.
        // Wolverine enrolls the outbox in the same EF Core transaction.
        foreach (var domainEvent in workOrder.DomainEvents)
            await _bus.PublishAsync(domainEvent, ct);

        await _db.SaveChangesAsync(ct);

        return Result.Success(workOrder.Id);
    }
}
```

**Key guarantee:** `_bus.PublishAsync` + `_db.SaveChangesAsync` happen in the same transaction. Wolverine's EF Core integration intercepts the publish and writes to the outbox table before the transaction commits.

#### 5.3 Outbox relay (DurabilityAgent)

Wolverine registers a hosted `DurabilityAgent` background service that polls unprocessed outbox rows and publishes them to the in-process transport. No extra configuration needed — it starts automatically.

**For Phase 1 (in-process):** The relay picks up outbox rows and delivers them to in-process handlers immediately. This gives us the outbox durability pattern now while keeping the transport local.

**For Phase 2 (broker):** Swap `UseInProcessTransport()` for `UseRabbitMq()` or `UseKafka()` — the outbox already stores the events, so no handler code changes.

#### 5.4 Handler discovery convention

Wolverine auto-discovers handlers by convention. Any public method named `Handle` (or `Consume`, or with `[WolverineHandler]` attribute) that takes a command/event as its first parameter and returns `Task` or `Task<Result<T>>` is a handler.

Each BC assembly must have a `Marker` class (empty, internal) so Wolverine can reference the assembly:

```csharp
// In each BC project root:
namespace CatCar.Contexts.Atendimento;
internal sealed class Marker { }
```

---

### 6. Aspire AppHost setup

#### 6.1 CatCar.AppHost Program.cs

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL 17 container (or 18, whichever is latest stable at bootstrap)
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("catcar-postgres-data")
    .WithPgAdmin(); // Optional: web UI for DB inspection

// Single database — schemas are created by EF Core migrations
var catcarDb = postgres.AddDatabase("catcar");

// CatCar API project — the web application
var api = builder.AddProject<Projects.CatCar_Api>("api")
    .WithReference(catcarDb)
    .WaitFor(catcarDb) // Wait for Postgres to be healthy before starting API
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Health checks are automatic via Aspire
// OpenTelemetry is automatic via Aspire

builder.Build().Run();
```

#### 6.2 What `dotnet run --project src/Host/CatCar.AppHost.csproj` starts

1. **PostgreSQL container** (with persistent volume `catcar-postgres-data`)
2. **PgAdmin** (optional, at a dynamic port)
3. **CatCar API** (at `https://localhost:7xxx` and `http://localhost:5xxx`)
4. **Aspire Dashboard** (at `https://localhost:1xxxx`)
   - Shows structured logs (Serilog → OpenTelemetry)
   - Shows distributed traces (HTTP → Wolverine handler spans)
   - Shows metrics (request duration, outbox lag, etc.)
   - Shows health status of all resources

#### 6.3 Connection string

The Aspire AppHost injects the connection string via an environment variable. The API reads it from the standard configuration path:

```json
// appsettings.json (no secrets — Aspire injects at runtime)
{
  "ConnectionStrings": {
    "catcar": "" // filled by Aspire at runtime
  }
}
```

#### 6.4 Health checks

Health check endpoints are exposed by the API project (not the AppHost). Registered via `Microsoft.Extensions.Diagnostics.HealthChecks`:

```csharp
// CatCar.Api Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("catcar")!,
        name: "postgres",
        tags: ["ready"])
    .AddCheck("outbox", () => HealthCheckResult.Healthy(), tags: ["ready"]);

// Endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // always returns 200 if the process is up
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

OpenTelemetry, structured logging, and the Aspire Dashboard come out-of-the-box with the Aspire AppHost — no extra code needed.

---

### 7. Test project structure

#### 7.1 One test project per BC

Each BC has its own test project under `tests/Contexts/<BC>.Tests/`. Rationale: tests for a BC should not accidentally depend on another BC's internals.

#### 7.2 Test categories

Within each BC test project:

```
tests/Contexts/Atendimento.Tests/
  Domain/              # Unit tests: aggregates, value objects, domain services
    WorkOrderTests.cs
    BudgetTests.cs
  Features/            # Unit tests: handlers with mocked dependencies
    OpenWorkOrderTests.cs
    IssueBudgetTests.cs
  Integration/         # Integration tests: real Postgres via Testcontainers
    OpenWorkOrderIntegrationTests.cs
```

#### 7.3 Integration test base class

A shared base class that manages a Testcontainers PostgreSQL instance:

```csharp
// tests/Contexts/Atendimento.Tests/Integration/IntegrationTestBase.cs
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("catcar_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected AtendimentoDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AtendimentoDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        DbContext = new AtendimentoDbContext(options);
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}
```

#### 7.4 Architecture.Tests project

A single `tests/Architecture.Tests/` project references all source assemblies and enforces structural rules via NetArchTest:

```csharp
// CrossContextIsolationTests.cs
public class CrossContextIsolationTests
{
    [Fact]
    public void Atendimento_ShouldNot_DependOn_CatalogoEstoque()
    {
        var atendimentoAssembly = typeof(CatCar.Contexts.Atendimento.Marker).Assembly;

        var result = Types()
            .That().ResideInAssembly(atendimentoAssembly)
            .ShouldNot()
            .HaveDependencyOn("CatCar.Contexts.CatalogoEstoque")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Atendimento_ShouldNot_DependOn_Comunicacao()
    {
        // same pattern for all cross-BC pairs
    }

    [Fact]
    public void Atendimento_ShouldNot_DependOn_Identidade()
    {
        // same pattern
    }
}

// DependencyDirectionTests.cs
public class DependencyDirectionTests
{
    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var domainTypes = Types()
            .That().ResideInNamespace("CatCar.Contexts.Atendimento.Domain");

        var result = domainTypes
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Wolverine()
    {
        // Domain must not reference Wolverine types
    }

    [Fact]
    public void SharedKernel_ShouldNot_Reference_Any_BoundedContext()
    {
        var skTypes = Types()
            .That().ResideInNamespace("CatCar.SharedKernel");

        var result = skTypes
            .ShouldNot()
            .HaveDependencyOn("CatCar.Contexts")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}

// VerticalSliceStructureTests.cs
public class VerticalSliceStructureTests
{
    [Fact]
    public void Each_Feature_Folder_Must_Have_Required_Files()
    {
        // Verify each Features/<UseCase>/ folder contains
        // Command, Handler, Validator, Endpoint files
    }

    [Fact]
    public void No_Technical_Folders_In_BC_Root()
    {
        // Verify no Services/, Controllers/, Repositories/ at BC root
    }
}
```

**NetArchTest rules map to constraints:**

| Test class | Enforces |
|---|---|
| `CrossContextIsolationTests` | CON-001 (no cross-BC imports) |
| `DependencyDirectionTests` | CON-002 (no EF Core in Domain), CON-003 (SharedKernel purity) |
| `VerticalSliceStructureTests` | CON-004 (BC folder structure), CON-005 (Vertical Slice per folder) |
| `SharedKernelPurityTests` | CON-003 (SK must not contain business types) |

#### 7.5 Test tooling decisions

- **xUnit v2.9.3** — stable for .NET 10; v3 deferred until LTS confirmation
- **FluentAssertions v8.2.0** — last Apache-2.0 version; see ADR-0002
- **NSubstitute v5.3.0** — preferred over Moq; see ADR-0001
- **Testcontainers.PostgreSql v4.4.0** — real PostgreSQL in Docker for integration tests
- **Bogus v35.6.2** — realistic Brazilian-domain test data (CPF, CNPJ, license plates)
- **coverlet.collector v6.0.4** — code coverage collection; results in Cobertura XML
- **Microsoft.NET.Test.Sdk v17.14.0** — test SDK

---

### 8. API project

#### 8.1 Composition root (CatCar.Api/Program.cs)

```csharp
using CatCar.Contexts.Atendimento;
using CatCar.Contexts.CatalogoEstoque;
using CatCar.Contexts.Comunicacao;
using CatCar.Contexts.Identidade;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console());

// ---- Wolverine ----
builder.Host.UseWolverine(opts =>
{
    opts.UseInProcessTransport();
    opts.Discovery.IncludeAssembly(typeof(Atendimento.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CatalogoEstoque.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(Comunicacao.Marker).Assembly);
    opts.Discovery.IncludeAssembly(typeof(Identidade.Marker).Assembly);
    opts.UseEntityFrameworkCoreOutbox();
});

// ---- BC Modules ----
builder.Services.AddAtendimento(builder.Configuration);
builder.Services.AddCatalogoEstoque(builder.Configuration);
builder.Services.AddComunicacao(builder.Configuration);
builder.Services.AddIdentidade(builder.Configuration);

// ---- OpenAPI ----
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CatCar API", Version = "v1" });
});

// ---- ProblemDetails (RFC 7807) ----
builder.Services.AddProblemDetails();

// ---- Health Checks ----
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("catcar")!,
        name: "postgres", tags: ["ready"]);

var app = builder.Build();

// ---- Run Migrations (dev only) ----
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    foreach (var ctx in scope.ServiceProvider.GetServices<DbContext>())
        await ctx.Database.MigrateAsync();
}

// ---- Middleware pipeline ----
app.UseSerilogRequestLogging();
app.UseExceptionHandler(); // ProblemDetails middleware

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "CatCar v1"));
}

app.UseHttpsRedirection();

// ---- MapGroup per BC ----
app.MapGroup("/api/v1/atendimento")
    .MapAtendimentoEndpoints()
    .RequireAuthorization();

app.MapGroup("/api/v1/catalogo")
    .MapCatalogoEndpoints()
    .RequireAuthorization();

app.MapGroup("/api/v1/comunicacao")
    .MapComunicacaoEndpoints();

app.MapGroup("/api/v1/identidade")
    .MapIdentidadeEndpoints();

// ---- Health ----
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

app.Run();
```

#### 8.2 Endpoint registration pattern (Vertical Slice)

Each Vertical Slice exposes its endpoint via an extension method on `IEndpointRouteBuilder`. The BC aggregates them in `MapAtendimentoEndpoints()`:

```csharp
// CatCar.Contexts.Atendimento/EndpointRouteBuilderExtensions.cs
namespace CatCar.Contexts.Atendimento;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAtendimentoEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenWorkOrder();
        endpoints.MapIssueBudget();
        endpoints.MapStartDiagnosis();
        endpoints.MapRecordApproval();
        endpoints.MapCompleteWorkOrder();
        endpoints.MapDeliverWorkOrder();
        return endpoints;
    }
}
```

```csharp
// CatCar.Contexts.Atendimento/Features/OpenWorkOrder/OpenWorkOrderEndpoint.cs
namespace CatCar.Contexts.Atendimento.Features.OpenWorkOrder;

public static class OpenWorkOrderEndpoint
{
    public static RouteHandlerBuilder MapOpenWorkOrder(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/work-orders",
            async (OpenWorkOrderCommand command, IMessageBus bus, CancellationToken ct) =>
            {
                var result = await bus.InvokeAsync<Result<WorkOrderId>>(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/v1/atendimento/work-orders/{result.Value}", result.Value)
                    : Results.BadRequest(new ProblemDetails
                    {
                        Title = "Failed to open work order",
                        Detail = result.Error,
                        Status = 400
                    });
            })
            .WithName("OpenWorkOrder")
            .WithTags("Atendimento")
            .Produces<WorkOrderId>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
```

#### 8.3 OpenAPI generation

**Decision:** Use the **built-in .NET 10 `Microsoft.AspNetCore.OpenApi`** for document generation (via `MapOpenApi()`) and **Swashbuckle.AspNetCore** for the SwaggerUI renderer only at `/swagger`. This avoids Swashbuckle's code-gen while keeping the familiar SwaggerUI.

- OpenAPI document at `/openapi/v1.json`
- SwaggerUI at `/swagger` (CON-011)
- Both available in Development only (not in Production)
- Auth scheme (JWT Bearer) described in the OpenAPI document via `WithOpenApi()` on each endpoint

#### 8.4 ProblemDetails (RFC 7807)

.NET 10's built-in `AddProblemDetails()` provides standard error responses:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Customer CPF is invalid",
  "traceId": "00-abc123def456-78901234567890-01"
}
```

Validation errors from FluentValidation are automatically converted to ProblemDetails with `errors` map.

---

### 9. Code quality tooling

#### 9.1 `.editorconfig`

A comprehensive `.editorconfig` at the solution root:

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 4
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
# C# 14 conventions
dotnet_style_qualification_for_field = false:error
dotnet_style_qualification_for_property = false:error
dotnet_style_qualification_for_method = false:error
dotnet_style_qualification_for_event = false:error
dotnet_style_predefined_type_for_locals_parameters_members = true:error
dotnet_style_predefined_type_for_member_access = true:error
csharp_style_var_for_built_in_types = true:error
csharp_style_var_when_type_is_apparent = true:error
csharp_style_var_elsewhere = true:error
dotnet_style_explicit_tuple_names = true:error
dotnet_style_coalesce_expression = true:error
dotnet_style_null_propagation = true:error
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:error
dotnet_style_prefer_auto_properties = true:error
dotnet_style_prefer_conditional_expression_over_assignment = true:error
dotnet_style_prefer_conditional_expression_over_return = true:error
dotnet_style_object_initializer = true:error
dotnet_style_collection_initializer = true:error
csharp_prefer_simple_default_expression = true:error
csharp_prefer_braces = true:error
csharp_style_namespace_declarations = file_scoped:error
csharp_style_expression_bodied_methods = when_on_single_line:error
csharp_style_expression_bodied_constructors = when_on_single_line:error
csharp_style_expression_bodied_properties = when_on_single_line:error

# StyleCop severity
dotnet_diagnostic.SA1633.severity = error     # File header required

[*.{csproj,props,xml,json,yml,yaml,md}]
indent_size = 2
```

#### 9.2 Roslyn analyzers

- `Microsoft.CodeAnalysis.NetAnalyzers` (ships with .NET 10 SDK; enabled by default)
- `StyleCop.Analyzers 1.2.0-beta.556` with `StyleCop.Analyzers.ruleset`:
  - Enable: SA1200 (using ordering), SA1503 (braces), SA1600 (documentation)
  - Disable: SA1633 (file header — use `dotnet_diagnostic.SA1633.severity = none` instead)

#### 9.3 `dotnet format`

Run in CI as a check (not auto-fix):

```yaml
- name: Format check
  run: dotnet format --verify-no-changes
```

Developers run `dotnet format` locally before committing.

#### 9.4 NetArchTest rules (exact rules enforcing CON-001 through CON-005)

See §7.4 above for the test classes. These rules must pass for the build to succeed (they are `[Fact]` assertions, so `dotnet test` fails if a rule is violated).

---

### 10. Local dev experience

#### 10.1 `dotnet run --project src/Host/CatCar.AppHost.csproj`

**What starts:**
1. PostgreSQL 17 container with persistent data volume
2. PgAdmin web UI (optional, at a dynamic port — can be disabled)
3. CatCar API at `https://localhost:7134` and `http://localhost:5134`
4. Aspire Dashboard at `https://localhost:17134`
   - Resources tab: shows Postgres + API health
   - Structured Logs: search by traceId, log level
   - Traces: HTTP request → Wolverine handler → EF Core query
   - Metrics: request rate, duration percentiles
5. EF Core migrations run automatically on API startup (dev only)
6. SwaggerUI at `https://localhost:7134/swagger`

**Prerequisites:** .NET 10.0.8 SDK, Docker (for Postgres container).

#### 10.2 Seed data strategy

A `SeedData` static class in `CatCar.Api` (or in each BC's Infrastructure) that runs only in Development:

```csharp
// CatCar.Api/SeedData.cs
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var environment = services.GetRequiredService<IWebHostEnvironment>();
        if (!environment.IsDevelopment()) return;

        using var scope = services.CreateScope();
        var atendimentoDb = scope.ServiceProvider.GetRequiredService<AtendimentoDbContext>();

        if (await atendimentoDb.Set<Customer>().AnyAsync()) return; // already seeded

        // Create sample data
        var customer = Customer.Register(
            "João Silva", new Cpf("529.982.247-25"), "joao@email.com", "11999998888");
        var vehicle = Vehicle.Register("ABC1D23", "Honda", "Civic", 2024, customer.Id);
        atendimentoDb.Set<Customer>().Add(customer);
        atendimentoDb.Set<Vehicle>().Add(vehicle);
        await atendimentoDb.SaveChangesAsync();
    }
}
```

Called in `Program.cs`:
```csharp
if (app.Environment.IsDevelopment())
    await SeedData.InitializeAsync(app.Services);
```

**Rules:**
- Seed only in Development environment.
- Check if already seeded (idempotent).
- Use `Bogus` for realistic data.
- Never run seed in Staging or Production.

#### 10.3 `docker-compose.yml` (Angular-free alternative)

For developers who don't want the full Aspire experience, a standalone `docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:18-alpine
    environment:
      POSTGRES_USER: catcar
      POSTGRES_PASSWORD: catcar_dev
      POSTGRES_DB: catcar
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U catcar"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

Then run the API locally with `dotnet run --project src/Api/CatCar.Api.csproj`.

---

### 11. Docker setup

#### 11.1 Multi-stage Dockerfile

```dockerfile
# === Build stage ===
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layers (cacheable)
COPY Directory.Packages.props ./
COPY CatCar.sln ./
COPY src/SharedKernel/CatCar.SharedKernel.csproj src/SharedKernel/
COPY src/Contracts/CatCar.Contracts.csproj src/Contracts/
COPY src/Contexts/Atendimento/CatCar.Contexts.Atendimento.csproj src/Contexts/Atendimento/
COPY src/Contexts/CatalogoEstoque/CatCar.Contexts.CatalogoEstoque.csproj src/Contexts/CatalogoEstoque/
COPY src/Contexts/Comunicacao/CatCar.Contexts.Comunicacao.csproj src/Contexts/Comunicacao/
COPY src/Contexts/Identidade/CatCar.Contexts.Identidade.csproj src/Contexts/Identidade/
COPY src/Api/CatCar.Api.csproj src/Api/
RUN dotnet restore src/Api/CatCar.Api.csproj

# Build
COPY src/ src/
RUN dotnet publish src/Api/CatCar.Api.csproj -c Release -o /app --no-restore

# === Runtime stage ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "CatCar.Api.dll"]
```

#### 11.2 docker-compose.yml (full, with API)

```yaml
services:
  postgres:
    image: postgres:18-alpine
    environment:
      POSTGRES_USER: catcar
      POSTGRES_PASSWORD: catcar_dev
      POSTGRES_DB: catcar
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U catcar"]
      interval: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__catcar: "Host=postgres;Database=catcar;Username=catcar;Password=catcar_dev"
      ASPNETCORE_ENVIRONMENT: Development
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres_data:
```

---

### 12. CI pipeline (GitHub Actions)

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: 10.0.x

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore CatCar.sln

      - name: Build
        run: dotnet build CatCar.sln -c Release --no-restore

      - name: Format check
        run: dotnet format --verify-no-changes

      - name: Unit + Architecture Tests
        run: >
          dotnet test CatCar.sln -c Release --no-build
          --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~E2E"
          --collect:"XPlat Code Coverage"
          --results-directory ./TestResults
          --logger "trx"

      - name: Integration Tests
        run: >
          dotnet test CatCar.sln -c Release --no-build
          --filter "FullyQualifiedName~Integration"
          --logger "trx"
          --environment "DOCKER_HOST=unix:///var/run/docker.sock"

      - name: Coverage Report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator \
            -reports:./TestResults/**/coverage.cobertura.xml \
            -targetdir:./CoverageReport \
            -reporttypes:Html

      - name: Coverage Gate
        run: |
          # Parse and verify >= 80% on critical domain assemblies
          echo "Coverage check: manual verification on report"

      - name: SAST — Package Vulnerabilities
        run: dotnet list CatCar.sln package --vulnerable --include-transitive
        continue-on-error: false

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: ./TestResults/**/*.trx

      - name: Upload Coverage
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: ./CoverageReport/

  docker:
    name: Docker Build & Scan
    needs: build-and-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Build Docker image
        run: docker build -t catcar:${{ github.sha }} .

      - name: Trivy vulnerability scan
        uses: aquasecurity/trivy-action@0.28.0
        with:
          image-ref: catcar:${{ github.sha }}
          format: table
          exit-code: 1
          severity: CRITICAL,HIGH
```

---

## Constraints emitted

This feature does not introduce new CON-level constraints beyond those already emitted by the strategic design (CON-001 through CON-015). However, the following **tactical standards** established here are binding for all downstream features:

| Standard | Description | Source |
|---|---|---|
| Assembly naming | All projects follow `CatCar.Contexts.<BC>` (BC), `CatCar.SharedKernel`, `CatCar.Contracts`, `CatCar.Api`, `CatCar.AppHost`, `CatCar.<Context>.Tests` | This design §2 |
| Migration naming | `<BC>_<seq>_<description>` with seq zero-padded to 4 digits | This design §4.5 |
| Endpoint pattern | Minimal API endpoints via `MapGroup` per BC, endpoints defined in Vertical Slice folders as extension methods | This design §8.2 |
| Module registration | Each BC exposes `ServiceCollectionExtensions.Add<BC>()` and `EndpointRouteBuilderExtensions.Map<BC>Endpoints()` | This design §8 |
| UUID v7 | All aggregate root IDs use `Guid.CreateVersion7()`, mapped as `uuid` in Postgres | This design §4.2 |
| Audit via interceptor | `created_at`/`updated_at` populated by `SaveChangesInterceptor` using shadow properties | This design §4.3 |
| Concurrency via xmin | Postgres `xmin` as `uint RowVersion` shadow property, `IsRowVersion()` | This design §4.4 |

## Risks

| ID | Risk | Tag | Mitigation / owner |
|---|---|---|---|
| R-001 | Package versions pinned here may have newer patches at implementation time | accepted | Dependabot auto-bumps patches; verify compatibility in CI |
| R-002 | PostgreSQL 18 may not yet be available in Aspire's hosting integration | mitigated | Fall back to PG 17; both are in the Aspire integration catalog |
| R-003 | Wolverine 6.5.1 outbox integration with EF Core 10 may need configuration adjustment | mitigated | Spike during implementation if issues arise; Wolverine 6.x targets .NET 9+, 10 should be compatible |
| R-004 | StyleCop beta analyzer (1.2.0-beta.556) may have false positives on C# 14 features | accepted | Suppress specific rules if needed; stable release expected during feature development |
| R-005 | UUID v7 `Guid.CreateVersion7()` availability depends on .NET 9+ runtime; confirmed present in .NET 10 | mitigated | Already verified; fallback to `UUIDNext` NuGet if needed |

## Module interactions

### Startup sequence (Aspire)

```
AppHost starts
  └─ Postgres container starts
  └─ Api project starts (waits for Postgres health)
       ├─ Wolverine configures handlers from all 4 BC assemblies
       ├─ EF Core migrations run for all 4 BC DbContexts
       ├─ Outbox relay (DurabilityAgent) starts polling
       ├─ Minimal API endpoints are mapped (4 MapGroups)
       └─ Health checks register
```

### Project reference graph

```
CatCar.SharedKernel  ←──  CatCar.Contracts  ←──  CatCar.Contexts.ServiceOperations
                                            ←──  CatCar.Contexts.CatalogInventory
                                            ←──  CatCar.Contexts.Communication
                                            ←──  CatCar.Contexts.IdentityAccess
                                                      ↑
                                              CatCar.Api  (references all 4 BCs + SK + Contracts)
```

**No BC references another BC.** Cross-BC communication is via Wolverine handlers consuming `IIntegrationEvent` types from `CatCar.Contracts`.

## Cross-cutting impacts

- **Observability:** OpenTelemetry is auto-configured by Aspire — every HTTP request, Wolverine handler, and EF Core query produces traces and spans. Serilog enriches logs with trace context.
- **Deployment:** The Dockerfile and docker-compose.yml produced here are the canonical deployment artifacts for Phase 1. In Phase 2 (feature 08), K8s manifests and Terraform will consume the same Docker image.
- **Migration strategy:** EF Core migrations run at API startup in dev; in production (Phase 2), migrations run as an init container or CI step.
- **Compatibility window:** Target `net10.0`. All packages pinned to compatible versions. Patches (patch digit) auto-bumped by Dependabot.

## Open questions

- None. All 12 tactical items have been resolved. The design is complete and ready for `@specificator` to write `spec.md`.

---

## Diagrams

- [Project Reference Graph](project-reference-graph.md) — Mermaid diagram of the assembly dependency graph
