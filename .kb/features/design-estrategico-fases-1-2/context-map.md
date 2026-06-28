---
feature: design-estrategico-fases-1-2
kind: diagram
updated: 2026-06-10
phase: archived
---

# Context Map

```mermaid
flowchart TB
  subgraph BCs["Bounded Contexts"]
    direction LR

    subgraph AT["Atendimento/OS [Core]"]
      atendimento["Atendimento/OS [Core]<br/>Aggregates:<br/>WorkOrder<br/>Budget<br/>Customer<br/>Vehicle"]
    end

    subgraph CE["Catalogo & Estoque [Supporting]"]
      catalogo["Catalogo & Estoque [Supporting]<br/>Aggregates:<br/>InventoryItem<br/>CatalogedService"]
    end

    subgraph CC["Comunicacao com Cliente [Supporting]"]
      comunicacao["Comunicacao com Cliente [Supporting]<br/>Aggregates:<br/>ExternalAccessToken<br/>Notification"]
    end

    subgraph IA["Identidade & Acesso [Generic]"]
      identidade["Identidade & Acesso [Generic]<br/>Aggregates:<br/>AdministrativeUser<br/>Role"]
    end
  end

  subgraph SK["Shared Kernel (Tactical)"]
    direction TB
    shared_kernel["Shared Kernel (Tactical)<br/>Entity&lt;TId&gt;<br/>ValueObject<br/>IAggregateRoot<br/>IDomainEvent<br/>IIntegrationEvent<br/>Result&lt;T&gt;"]
  end

  catalogo -->|Customer/Supplier (OHS+ACL)<br/>Catalog DTOs| atendimento
  atendimento -->|Published Language<br/>BudgetIssued, BudgetApproved| comunicacao
  comunicacao -->|Customer/Supplier (OHS+ACL)<br/>RecordBudgetApproval| atendimento

  identidade -->|OHS+ACL<br/>JWT Claims (UserId, Role)| atendimento
  identidade -->|OHS+ACL<br/>JWT Claims (UserId, Role)| catalogo
  identidade -->|OHS+ACL<br/>JWT Claims (UserId, Role)| comunicacao

  atendimento -->|Shared Kernel<br/>(tactical base classes only)| shared_kernel
  shared_kernel -->|Shared Kernel<br/>(tactical base classes only)| atendimento

  catalogo -->|Shared Kernel<br/>(tactical base classes only)| shared_kernel
  shared_kernel -->|Shared Kernel<br/>(tactical base classes only)| catalogo

  comunicacao -->|Shared Kernel<br/>(tactical base classes only)| shared_kernel
  shared_kernel -->|Shared Kernel<br/>(tactical base classes only)| comunicacao

  identidade -->|Shared Kernel<br/>(tactical base classes only)| shared_kernel
  shared_kernel -->|Shared Kernel<br/>(tactical base classes only)| identidade

  classDef core fill:#E3F2FD,stroke:#1565C0,stroke-width:2px,color:#0D47A1;
  classDef supporting fill:#E8F5E9,stroke:#2E7D32,stroke-width:2px,color:#1B5E20;
  classDef generic fill:#FFF8E1,stroke:#F9A825,stroke-width:2px,color:#8D6E63;
  classDef shared fill:#F3E5F5,stroke:#6A1B9A,stroke-width:2px,color:#4A148C,stroke-dasharray: 5 3;

  class atendimento core;
  class catalogo supporting;
  class comunicacao supporting;
  class identidade generic;
  class shared_kernel shared;
```
