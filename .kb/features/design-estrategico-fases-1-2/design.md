---
feature: design-estrategico-fases-1-2
kind: design
status: draft
updated: 2026-06-10
related:
  - decomposicao-features-fases-1-2
phase: archived
---

# Design: Estratégia DDD inicial para fases 1 e 2

- [Event Storming Diagram](event-storming.excalidraw)
- [Event Storming Diagram (Mermaid)](event-storming.md)
- [Context Map](context-map.excalidraw)
- [Context Map (Mermaid)](context-map.md)
- [Module Structure](module-structure.excalidraw)
- [Module Structure (Mermaid)](module-structure.md)

## Context

As fases 1 e 2 serão entregues juntas e depois quebradas em várias features. Este documento registra a linha-base estratégica — bounded contexts, relações, agregados, padrão de monólito modular e stack — para que as próximas features táticas não partam de um modelo anêmico, inconsistente ou tecnicamente divergente.

As decisões deste design foram ratificadas com o usuário em jun/2026. As pendências remanescentes do discovery original (Q1 sobre consistência aprovação↔estoque; Q2 sobre split Recepção/Orçamento vs. Oficina/Execução) estão resolvidas neste documento.

## Decision

- começar com um **monólito modular** (.NET 10 + .NET Aspire + EF Core + PostgreSQL);
- usar **Vertical Slice Architecture + Clean Architecture** dentro de cada módulo, orientados pelo **Inverse Conway Law** (cada BC é um módulo de alto coesão e baixo acoplamento, com fronteiras que forçam o time a tratá-los como blocos que um dia podem ser extraídos);
- quatro contextos estratégicos: `Atendimento/OS` (Core), `Catalogo & Estoque` (Supporting), `Comunicacao com Cliente` (Supporting), `Identidade & Acesso` (Generic);
- um **Shared Kernel** estritamente para **infraestrutura tática de DDD** (`Entity<TId>`, `ValueObject`, `IAggregateRoot`, `IDomainEvent`, etc.) — nunca para modelo de domínio;
- comunicação cross-BC **sempre via contrato publicado** (eventos de domínio ou comandos): nenhum BC importa código de outro BC diretamente;
- eventos de domínio publicados via **Wolverine** com **outbox** na mesma transação do agregado, preparando extração futura sem reescrita;
- `Cliente` e `Veiculo` ficam dentro de `Atendimento/OS` no MVP (pragmático, evita over-engineering);
- preços de serviços e peças são **congelados no orçamento/OS** (snapshot) para auditoria e histórico;
- interação do cliente se dá por **link/token externo seguro** (não há "login do cliente");
- infraestrutura da fase 2 (Docker, K8s, Terraform, CI/CD) é **plataforma**, não bounded context de negócio.

## Strategic Context Sketch

### `Atendimento/OS` — Core
- Concentra abertura da OS, dados de cliente/veículo, diagnóstico, orçamento, decisão do cliente e acompanhamento do ciclo completo.
- É o único BC que conhece o conceito de "OS como artefato rastreável".
- Sua amplitude no MVP é proposital; a modularização interna (Vertical Slices) já é uma disciplina de split futuro.

### `Catalogo & Estoque` — Supporting
- Mantém serviços catalogados, peças/insumos, disponibilidade, preço-base, reserva e movimentação de estoque.
- Est isolado mesmo dentro do monólito porque suas invariantes (não-negativo, reserva→consumo rastreável) diferem do fluxo da OS.
- No MVP, serviços e peças dividem o mesmo BC; a taxa de mudança é o gatilho para reavaliar uma divisão futura.

### `Comunicacao com Cliente` — Supporting
- Dono de canais externos (e-mail, link tokenizado) e da tradução entre o domínio da OS e o mundo externo.
- O cliente **não tem login administrativo**: sua "autenticação" é um **token externo de uso único, vinculado a um orçamento**. Esse token é propriedade deste BC.
- Não decide o negócio da aprovação — apenas media o canal e entrega comandos autenticados.

### `Identidade & Acesso` — Generic
- Autenticação/autorização **administrativa** (JWT, papéis, segredos).
- Não vaza modelo de identidade para os BCs de negócio (a fronteira é uma ACL no consumidor).

## DDD Context Map  *(ratified: true)*

### Contexts

| Context | Tipo | Owned terms (summary) | .NET Module proposed |
|---|---|---|---|---|
| `Atendimento/OS` | Core | WorkOrder, Customer, Vehicle, Budget | `CatCar.Contexts.Atendimento` |
| `Catalogo & Estoque` | Supporting | CatalogedService, InventoryItem | `CatCar.Contexts.CatalogoEstoque` |
| `Comunicacao com Cliente` | Supporting | Notification, ExternalAccessToken | `CatCar.Contexts.Comunicacao` |
| `Identidade & Acesso` | Generic | AdministrativeUser, Role, Session | `CatCar.Contexts.Identidade` |
| **Shared Kernel** (tático) | infra | `Entity<TId>`, `ValueObject`, `IAggregateRoot`, `IDomainEvent`, `IIntegrationEvent`, `Result<T>` | `CatCar.SharedKernel` |

### Integrações cross-BC

| Upstream | Downstream | Relationship | Translation strategy | What crosses the boundary |
|---|---|---|---|---|---|
| `Catalogo & Estoque` | `Atendimento/OS` | Customer-Supplier | **OHS + Published Language**, with **ACL** inside `Atendimento/OS` | `InventoryItemId`, `CatalogedServiceId`, base price, availability — only catalog DTOs, never entities |
| `Atendimento/OS` | `Comunicacao com Cliente` | Published Language | Event contract published in `CatCar.Contracts/Atendimento` | `BudgetIssued`, future `BudgetApproved`, `WorkOrderStatusChanged` |
| `Comunicacao com Cliente` | `Atendimento/OS` | Customer-Supplier | **OHS** with **ACL** inside `Atendimento/OS` (customer already authenticated via external token) | `RecordBudgetApproval`, future rejection |
| `Identidade & Acesso` | `Atendimento/OS` (and others) | Open Host Service | **ACL** on the consumer — JWT validated as a service | `UserId`, `Role` arrive as claims, never as entities |
| All | all (infra) | **Shared Kernel** (tactical) | Shared base classes in `CatCar.SharedKernel` | Only `Entity<TId>`, `ValueObject`, `IAggregateRoot`, `IDomainEvent`, `IIntegrationEvent`, `Result<T>` — no model types |

### Anti-corruption layers (ACLs)
- `Atendimento/Integrations/CatalogAcl` — translates catalog DTOs into WorkOrder domain models. Lives inside `CatCar.Contexts.Atendimento`, not in the upstream BC.
- `Atendimento/Integrations/IdentityAcl` — receives JWT claims, exposes only the current `UserId` to the domain.
- `Atendimento/Integrations/CommunicationAcl` — receives commands from the Communication BC (already authenticated), delivers to the domain.

### Shared Kernel (escopo e limites)
- **Pode:** conter tipos genéricos de DDD tático (`Entity<TId>`, `ValueObject`, `IAggregateRoot`, `IDomainEvent`, `IIntegrationEvent`, `Result<T>`, exceções de domínio como `BusinessRuleViolatedException`).
- **Não pode:** conter tipos com semântica de negócio de um BC específico, enums de domínio, validações de CPF/placa, mapeamentos de tabela. Tudo isso vive dentro do BC correspondente.
- Qualquer adição ao SK exige AD-R e revisão; o SK cresce devagar por design.

### Por que **não** Conformist nem Big Ball of Mud
- **Conformist** foi considerado para `Atendimento/OS` consumindo `Identidade & Acesso` (importar o `Usuario` direto). Rejeitado: vaza modelo de identidade no domínio da OS.
- **Big Ball of Mud** não se aplica — o monólito é modular, com fronteiras explícitas em código (módulos .NET), em eventos (Wolverine outbox) e em schema (schema por BC no Postgres).

## DDD Glossary  *(ratified: true)*

### WorkOrder *(in `Atendimento/OS`)*
- **Definition:** unit of tracking for work performed on a specific vehicle, with status history, active budget, and operational result.
- **Aliases (forbidden):** "chamado", "ticket".

### Budget *(in `Atendimento/OS`)*
- **Definition:** active commercial proposal of services and parts linked to a WorkOrder, frozen for audit from the moment of issuance.
- **Aliases (forbidden):** "cotação dinâmica".

### ApprovalLink / ExternalAccessToken *(in `Comunicacao com Cliente`)*
- **Definition:** secure external mechanism by which the customer approves or rejects the budget without needing an administrative login. Single-use token, linked to a specific budget, with expiration.
- **Aliases (forbidden):** "login do cliente", "sessão do cliente".

### InventoryItem *(in `Catalogo & Estoque`)*
- **Definition:** part or supply controlled with availability, reservation, and consumption traceable by WorkOrder.
- **Aliases (forbidden):** "produto" when the intent is operational inventory.

### CatalogedService *(in `Catalogo & Estoque`)*
- **Definition:** service offered by the shop, with description, base price, and category. Does not represent execution — execution is the WorkOrder's responsibility.
- **Aliases (forbidden):** "item de serviço", "serviço executado".

### AdministrativeUser *(in `Identidade & Acesso`)*
- **Definition:** administrative panel access credential, with an associated role.
- **Aliases (forbidden):** "cliente" (customers don't have logins), "funcionário" (role model, not identity).

### Customer *(in `Atendimento/OS`)*
- **Definition:** individual or legal entity that contracts services for a vehicle, identified by CPF/CNPJ.
- **Aliases (forbidden):** "usuário" (confuses with administrative), "consumidor" (retail language).

### Vehicle *(in `Atendimento/OS`)*
- **Definition:** a customer's vehicle, identified by license plate, with make/model/year.
- **Aliases (forbidden):** "carro" (when context is technical), "automóvel" (same reason).

## DDD Aggregate — WorkOrder  *(ratified: true)*

- **Context:** `Atendimento/OS`
- **Root:** `WorkOrder` (id `WorkOrderId`). Lifecycle: `Received → InDiagnosis → AwaitingApproval → InExecution → Completed → Delivered`.

### Invariants
- A OS pertence a **um único** `Cliente` e **um único** `Veiculo` durante todo o ciclo de vida.
- O orçamento vigente é um **snapshot imutável** dos preços e descrições de serviços e peças no momento da emissão; alterações posteriores no catálogo não afetam a OS em andamento.
- A OS não transita para `EmExecucao` sem uma decisão válida (`Aprovada` ou `Rejeitada`) sobre o orçamento vigente.
- Toda transição de status emite um `IDomainEvent` e é registrada em um log de auditoria (anexado à OS, não externalizado).
- Cliente e veículo referenciados apenas por `CustomerId` / `VehicleId` (referência por ID; o agregado não carrega o agregado vizinho).

### Operations
| Operation | Pre-conditions | Post-conditions / events |
|---|---|---|
| `Receive(customerId, vehicleId, initialDescription)` | customer and vehicle exist and are active | status = `Received`; emits `WorkOrderReceived` |
| `StartDiagnosis()` | status = `Received` | status = `InDiagnosis`; emits `DiagnosisStarted` |
| `IssueBudget(lines, priceSnapshot)` | status = `InDiagnosis`; no active budget | status = `AwaitingApproval`; emits `BudgetIssued` (integration event) |
| `RecordApproval(decision, validatedToken)` | status = `AwaitingApproval`; token is valid and matches the active budget | status = `InExecution` if approved; status = `Rejected` (terminal for this budget) if rejected; emits `BudgetApproved` or `BudgetRejected` |
| `StartExecution()` | status = `AwaitingApproval` with approval recorded | status = `InExecution`; emits `ExecutionStarted` |
| `Complete()` | status = `InExecution` | status = `Completed`; emits `WorkOrderCompleted` |
| `Deliver()` | status = `Completed` | status = `Delivered`; emits `WorkOrderDelivered` |

### Boundary rules
- Dentro do agregado: status, orçamento, linhas do orçamento, log de auditoria — tudo fortemente consistente.
- Cruzando fronteira: referência por `CustomerId`, `VehicleId`, `InventoryItemId`, `CatalogedServiceId` — apenas IDs.
- Estoque (reserva/consumo) **não** é parte deste agregado; o acoplamento é resolvido via evento `BudgetApproved` consumido por `Catalogo & Estoque`.

## DDD Aggregate — InventoryItem  *(ratified: true)*

- **Context:** `Catalogo & Estoque`
- **Root:** `InventoryItem` (id `InventoryItemId`). Lifecycle: `Available → Reserved → Consumed | Restocked`.

### Invariants
- A quantidade disponível **nunca** é negativa (constraint absoluta do agregado).
- Toda reserva precisa ser **rastreável por `WorkOrderId`**.
- Uma reserva não pode consumir mais do que a quantidade disponível no momento da reserva.
- Liberação de reserva (e.g. orçamento rejeitado) é operação de domínio explícita, não "limpeza" implícita.

### Operations
| Operation | Pre-conditions | Post-conditions / events |
|---|---|---|
| `Reserve(workOrderId, quantity)` | quantity ≤ available; valid WorkOrder | status = `Reserved` (partial or total); emits `InventoryReserved` |
| `ReleaseReservation(workOrderId)` | active reservation exists for this WorkOrder | status returns to `Available` for the released quantity; emits `InventoryReservationReleased` |
| `Consume(workOrderId, quantity)` | active reservation for the WorkOrder; quantity ≤ reserved | decrements; emits `InventoryConsumed` |
| `Restock(quantity, reason)` | always | increments; emits `InventoryRestocked` |

### Boundary rules
- O agregado não conhece a OS além do `WorkOrderId` — não importa o agregado da OS.
- O acoplamento com a OS é feito pelo consumidor do evento `BudgetApproved` (handler que chama `Reservar`).

## DDD Aggregate — Budget  *(ratified: true)*

- **Context:** `Atendimento/OS`
- **Root:** `Budget` (id `BudgetId`). Vinculado 1:1 a uma OS em um dado momento; novos orçamentos substituem o anterior (não coexistem).

### Invariants
- Snapshot de preço, descrição e quantidade de cada linha no momento da emissão — **imutável** após emissão.
- Linhas de serviço e peça referenciadas por `CatalogedServiceId` / `InventoryItemId` apenas.
- O orçamento tem um **ciclo de vida próprio** dentro da OS: `Active → Approved | Rejected → Replaced`.

### Operations
| Operation | Pre-conditions | Post-conditions / events |
|---|---|---|
| `Issue(workOrderId, lines, snapshot)` | WorkOrder in `InDiagnosis` or `AwaitingApproval` (replaces the active one) | status = `Active`; emits `BudgetIssued` |
| `Approve(token)` | status = `Active`; token matches | status = `Approved`; emits `BudgetApproved` (integration event) |
| `Reject(token, reason)` | status = `Active`; token matches | status = `Rejected`; emits `BudgetRejected` |
| `Replace(newBudget)` | status = `Active` or `Rejected` | status = `Replaced`; new budget becomes active |

## DDD Aggregate — ExternalAccessToken  *(ratified: true)*

- **Context:** `Comunicacao com Cliente`
- **Root:** `ExternalAccessToken` (id `ExternalAccessTokenId`). Vinculado 1:1 a um `BudgetId`.

### Invariants
- Token é de **uso único** — consumido quando o cliente aprova/rejeita.
- Tem **expiração** (sugestão: 7 dias para o MVP, ajustável).
- Apenas o `BudgetId` vinculado pode ser aprovado/rejeitado por esse token.
- O token **não carrega** informação do cliente além do que está no orçamento — não há perfil de "cliente logado".

### Operations
| Operation | Pre-conditions | Post-conditions / events |
|---|---|---|
| `Issue(budgetId, ttl)` | active budget exists | status = `Active`; emits `ApprovalLinkGenerated` |
| `Validate(rawToken)` | status = `Active` and not expired | returns `BudgetId` and the active budget; does **not** consume the token |
| `Consume(decision)` | status = `Active`; after successful `Validate` | status = `Consumed`; emits `BudgetApprovalRecorded` or `BudgetRejectionRecorded` |
| `Expire()` | TTL elapsed | status = `Expired` (background job or lazy on `Validate`) |

## Resolved Open Questions (do `discovery.md`)

### Q1 — Consistência entre aprovação do orçamento e comprometimento de estoque
- **Decisão:** **consistência forte no MVP** via chamada síncrona dentro do monólito (`ReserveInventory` é invocado no mesmo handler que registra a aprovação), combinada com a **publicação do evento `BudgetApproved` via outbox** para qualquer observer secundário.
- **Por que forte, não eventual:** invariante crítica para auditoria (a reserva precisa ser rastreável, e o cliente precisa de feedback imediato de que a aprovação foi processada).
- **Por que com outbox mesmo assim:** a outbox garante que, se um BC for extraído no futuro, basta plugar um transport real (Rabbit/Kafka) consumindo da outbox — a invariante local não é reescrita.
- **Trade-off aceito:** leve overhead de outbox no MVP; pago em evolução futura.

### Q2 — Fronteira operacional dentro de `Atendimento/OS`
- **Decisão:** **contexto único por enquanto**, com **modularização interna já disciplinada por Vertical Slices nomeadas por fase operacional** (`Features/OpenWorkOrder/`, `Features/IssueBudget/`, `Features/RecordApproval/`, `Features/ManageExecution/`).
- **Por que não separar agora:** o enunciado da Fase 1 não exige, o MVP ficaria mais complexo sem ganho, e a estrutura por slices já é uma "porta aberta" para o split Recepção/Orçamento ↔ Oficina/Execução.
- **Gatilho para revisar:** se as taxas de mudança de `Features/OpenWorkOrder` + `IssueBudget` começarem a divergir das de `ManageExecution`, ou se times diferentes assumirem essas fases.

## Architectural Pattern (Monólito Modular)

### Estrutura proposta
```
src/
  Contexts/
    Atendimento/                      # BC Atendimento/OS
      Domain/                         # aggregate, events, value objects (pure, no external deps)
      Features/                       # Vertical Slices
        OpenWorkOrder/{Command, Handler, Validator, Endpoint}.cs
        IssueBudget/...
        RecordApproval/...
        ManageExecution/...
      Infrastructure/                 # EF Core DbContext for the BC, repositories, ACLs
      Integrations/                   # ACLs: CatalogAcl, IdentityAcl, CommunicationAcl
    CatalogoEstoque/                  # BC Catalogo & Estoque
      Domain/
      Features/...                    # RegisterService, RegisterPart, ReserveInventory, ...
      Infrastructure/                 # DbContext, repositories
    Comunicacao/                      # BC Comunicacao com Cliente
      Domain/
      Features/...                    # IssueApprovalLink, SendEmail, RecordCustomerDecision
      Infrastructure/                 # SMTP/EmailProvider, template engine
    Identidade/                       # BC Identidade & Acesso
      Domain/
      Features/...                    # Login, IssueToken, ValidateToken
      Infrastructure/                 # ASP.NET Identity or equivalent
  SharedKernel/                       # SK tático: Entity<TId>, ValueObject, IAggregateRoot, ...
  Contracts/                          # Published Language: serializable domain events
  Host/                               # AppHost do Aspire, bootstrap, Composition Root
  Api/                                # Endpoints REST (Minimal APIs) — apenas roteamento + DI
tests/
  Contexts/<BC>.Tests/                 # unit + integration tests per BC
  Architecture.Tests/                 # NetArchTest, regras estruturais
```

### Princípios
- **Vertical Slice dentro de cada BC:** uma feature = um diretório com `Command`/`Handler`/`Validator`/`Endpoint`. Não existem pastas `Services/`, `Controllers/`, `Repositories/` na raiz do módulo.
- **Clean Architecture interna:** `Domain` (puro, sem dependências de .NET fora de tipos básicos) → `Features` (orchestration, depende de `Domain` e `SharedKernel`) → `Infrastructure` (EF Core, SMTP, integrações externas) → `Integrations/ACLs` (fronteira com outros BCs).
- **Inverse Conway Law:** os módulos são organizados para que **o "time" que cuida de um BC consiga existir como time separado amanhã**. A fronteira em código (.NET modules + namespaces + ACLs + outbox + schema por BC) é o que torna essa evolução possível.
- **Sem `MediatR` direto:** a comunicação cross-feature e cross-BC é feita via **Wolverine** (commands/queries in-process + events com outbox + transporte unificado). Isso remove a dependência frágil de terceiros e prepara extração de BCs.

## Stack (referência — detalhes em `.kb/constitution.md`)

Versões verificadas em jun/2026 (URLs em `.kb/constitution.md#stack-pinning`):

| Camada | Decisão | Versão jun/2026 | Por quê |
|---|---|---|---|
| Linguagem | .NET 10 (C# 14) | **10.0.8** (LTS, EOL 2028-11-14) | LTS, ecossistema maduro, DDD tático bem suportado |
| Framework web | ASP.NET Core 10 (Minimal APIs) | 10.0.8 | Menos cerimônia, encaixa em Vertical Slice |
| Orquestração local | .NET Aspire | **13.1.2** (GA) | AppHost oficial Microsoft; health checks, OpenTelemetry, service discovery, integrações para Postgres/RabbitMQ |
| ORM | EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL | EF Core 10 LTS / Npgsql 10.x | Mapeamento rico, migrations, JSONB/range/fts |
| Banco | PostgreSQL 17+ (a fixar 17.x LTS ou 18.x) | 17.x (LTS) ou 18.x (vigente) | Open source, JSONB, transações robustas, full-text |
| Event bus | **Wolverine** (in-process + outbox + broker-ready) | **6.5.1** (MIT) | Outbox nativo, modelo unificado de handlers, OSS puro |
| Migrations | EF Core Migrations | alinhado a EF Core 10 | Padrão com EF Core |
| Testes | xUnit + FluentAssertions + Testcontainers + NetArchTest | a fixar na feature 01 | Unit, integration reais, regras estruturais |
| Validação | FluentValidation | a fixar na feature 01 | Padrão .NET, separado do domínio |
| Logs/Tracing | OpenTelemetry (via Aspire) + Serilog | a fixar na feature 01 | Standard do Aspire |
| Auth admin | JWT Bearer (a fixar provider na feature 02) | — | Padrão .NET |
| Auth cliente | Token externo de uso único (sem JWT) | — | Já descrito no agregado `ExternalAccessToken` |
| Linter/Formatador | `dotnet format` + Roslyn analyzers | — | Padrão Microsoft |
| Container | Docker + docker-compose (dev) + K8s (Fase 2) | — | Exigência do enunciado |

### Por que **Wolverine e não MediatR** (decisão explícita)
- MediatR v13+ migrou para **licença comercial** (LuckyPennySoftware) com **license key obrigatória** ([anúncio do Jimmy Bogard](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/), [release v13.0.0](https://github.com/LuckyPennySoftware/MediatR/releases/tag/v13.0.0)).
- Wolverine 6.5.1 é **MIT puro** ([LICENSE](https://raw.githubusercontent.com/JasperFx/wolverine/main/LICENSE)), com **outbox nativo** e modelo unificado in-process + broker (RabbitMQ/Kafka).
- Wolverine cobre **tudo** o que MediatR cobre (request/handler, notification/multi-handler) e adiciona outbox/durabilidade/transportes, que são exatamente o que o CatCar precisa para a decisão Q1 (consistência forte + outbox) e para a extração futura de BCs.

> **Política de versão (pinning):** a versão exata de cada pacote é fixada em `versions.props` no momento da feature `01-fundacao-arquitetural-monolito-modular`, usando `latest patch` da major alinhada à tabela acima na data de início. Mudanças de major exigem novo AD-R.

## Module Interactions (summary)

Canonical sequence for the critical flow `BudgetApproved → ReserveInventory`:

1. Customer accesses the link → `Comunicacao` validates token (consuming the `ExternalAccessToken`).
2. `Comunicacao` calls the **authenticated endpoint** `RecordBudgetApproval` in `Atendimento` (with the token already validated — ACL on the consumer).
3. `Atendimento.Features.RecordApproval.Handler` loads the WorkOrder, applies the command (status transition + new budget state) and **publishes the `BudgetApproved` event to the outbox in the same aggregate transaction**.
4. Wolverine consumes the event in-process → calls the handler `CatalogoEstoque.Features.ReserveInventory.Handler` (synchronous in-process call) → inventory reserved, event `InventoryReserved` published.
5. The customer receives a confirmation email (`BudgetApproved` event also feeds the `Comunicacao.Features.SendConfirmation.Handler`).

> This flow is **strongly consistent in steps 1–4** (same database transaction with outbox) and **eventually consistent only for confirmations/emails** (step 5, asynchronous by design).

## Constraints emitted
- Ver `constraints.md` para o contrato mecânico que `@developer` e `@reviewer` honrarão:
  - `CON-001`..`CON-005`: fronteiras e dependências entre BCs
  - `CON-006`..`CON-009`: padrões de Vertical Slice e teste
  - `CON-010`..`CON-013`: stack e pinning de versão

## Risks
| ID | Risk | Tag | Mitigation / owner |
|---|---|---|---|
| R-001 | Outbox sem relay ativo pode crescer indefinidamente em produção | mitigated | Adicionar job de relay no AppHost Aspire; monitorar lag; na Fase 2, plugar transport real |
| R-002 | Shared Kernel virar "lixão" com o tempo | accepted | AD-R obrigatório para qualquer adição; revisão periódica na feature `01` |
| R-003 | `Atendimento/OS` ficar grande demais | accepted | Vertical Slices por fase operacional; gatilho definido para split |
| R-004 | Acoplamento entre `Identidade & Acesso` e BCs via claims mal filtradas | mitigated | ACL no consumidor + testes específicos na feature `02` |
| R-005 | Versões de pacotes .NET (Aspire, EF Core, Wolverine) ainda em movimento no momento do design | mitigated | Pinning centralizado em `versions.props` no início da feature `01` |

## Open questions
- Nenhuma questão arquitetural em aberto neste momento. As duas pendências do discovery foram resolvidas em "Resolved Open Questions" acima.
- Próximas decisões táticas (banco de migrations, estrutura de pastas final, naming de pacotes NuGet por BC) ficam para a feature `01-fundacao-arquitetural-monolito-modular`.
