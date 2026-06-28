---
feature: fundacao-arquitetural-monolito-modular
kind: diagram
updated: 2026-06-26
phase: archived
---

# Project Reference Graph

```mermaid
flowchart TD
    SK["CatCar.SharedKernel<br/>Entity, ValueObject, Result,<br/>IAggregateRoot, IDomainEvent"]
    CTR["CatCar.Contracts<br/>Published Language<br/>(IntegrationEvent records)"]

    ATD["CatCar.Contexts.Atendimento<br/>Core BC<br/>WorkOrder, Budget, Customer, Vehicle"]
    CAT["CatCar.Contexts.CatalogoEstoque<br/>Supporting BC<br/>InventoryItem, CatalogedService"]
    COM["CatCar.Contexts.Comunicacao<br/>Supporting BC<br/>ExternalAccessToken, Notification"]
    IDT["CatCar.Contexts.Identidade<br/>Generic BC<br/>AdministrativeUser, Role"]

    API["CatCar.Api<br/>Composition Root<br/>Minimal APIs, DI, MapGroup"]
    HOST["CatCar.AppHost<br/>Aspire Orchestrator<br/>Postgres, OpenTelemetry"]

    ATD_TEST["Atendimento.Tests"]
    CAT_TEST["CatalogoEstoque.Tests"]
    COM_TEST["Comunicacao.Tests"]
    IDT_TEST["Identidade.Tests"]
    ARCH_TEST["Architecture.Tests<br/>NetArchTest rules"]
    E2E_TEST["E2E.Tests<br/>(skeleton)"]

    SK --> CTR
    SK --> ATD
    SK --> CAT
    SK --> COM
    SK --> IDT
    CTR --> ATD
    CTR --> CAT
    CTR --> COM
    CTR --> IDT

    ATD --> API
    CAT --> API
    COM --> API
    IDT --> API
    SK --> API
    CTR --> API

    API --> HOST

    ATD --> ATD_TEST
    SK --> ATD_TEST
    CAT --> CAT_TEST
    COM --> COM_TEST
    IDT --> IDT_TEST
    ATD --> ARCH_TEST
    CAT --> ARCH_TEST
    COM --> ARCH_TEST
    IDT --> ARCH_TEST
    SK --> ARCH_TEST
    API --> E2E_TEST

    classDef sk fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px,color:#111
    classDef contracts fill:#e3f2fd,stroke:#1565c0,stroke-width:2px,color:#111
    classDef bc fill:#fff3e0,stroke:#e65100,stroke-width:2px,color:#111
    classDef api fill:#fce4ec,stroke:#c62828,stroke-width:2px,color:#111
    classDef host fill:#ede7f6,stroke:#4527a0,stroke-width:2px,color:#111
    classDef test fill:#f1f3f5,stroke:#868e96,stroke-dasharray: 5 5,color:#111

    class SK sk
    class CTR contracts
    class ATD,CAT,COM,IDT bc
    class API api
    class HOST host
    class ATD_TEST,CAT_TEST,COM_TEST,IDT_TEST,ARCH_TEST,E2E_TEST test
```

**Key rules visualized:**

- `CatCar.SharedKernel` depends on **nothing** (pure .NET).
- `CatCar.Contracts` depends **only** on `SharedKernel`.
- Each BC depends on `SharedKernel` + `Contracts` — **never** on another BC.
- `CatCar.Api` (composition root) depends on **all BCs** + `SharedKernel` + `Contracts`.
- `CatCar.AppHost` orchestrates the `Api` project via Aspire project references (no code dependency — coordination is at the process level).
- Test projects depend on their corresponding BC + test infrastructure.
- `Architecture.Tests` depends on **all BCs** + `SharedKernel` (to enforce CON-001 through CON-005).
