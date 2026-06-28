---
project: catcar
kind: constitution
status: draft
updated: 2026-06-07
applies_to:
  - .kb/features/**
  - src/**
  - tests/**
  - infra/**
  - .github/**
ratified_in: .kb/features/design-estrategico-fases-1-2/design.md
---

# Constitution — CatCar

Diretrizes estáveis e duráveis do projeto CatCar. Tudo aqui é **não-negociável a menos que um ADR novo** diga o contrário. Mudanças nesta constitution exigem ratificação explícita.

> Esta constitution é o **contrato entre estratégia, implementação e revisão**. `@developer` honra, `@reviewer` verifica, `@architector` evolui.

---

## 1. Visão e propósito

O **CatCar** é o back-end (MVP) de um sistema integrado de atendimento e execução de serviços para uma oficina mecânica de médio porte. Foco em gestão de **ordens de serviço (OS)**, **clientes**, **veículos** e **controle de peças/insumos**, com **DDD aplicado** e evolução de plataforma (K8s, IaC, CI/CD) na Fase 2.

- **Não-objetivos:** multi-tenant, marketplace, app mobile nativo, BI/analytics, multi-unidade nesta primeira entrega.

---

## 2. Princípios não-negociáveis

1. **DDD é a gramática do código.** Toda feature nasce de um contexto delimitado, agrega por invariante, e referencia vizinhos por ID.
2. **Monólito modular com fronteiras explícitas.** Um BC é um módulo .NET (namespace + assembly), com schema próprio no Postgres, com ACL no consumidor.
3. **Vertical Slice dentro de cada BC.** Um caso de uso = uma pasta, com `Command`/`Handler`/`Validator`/`Endpoint`. Nada de pastas técnicas (`Services/`, `Controllers/`, `Repositories/`) na raiz de um módulo.
4. **Inverse Conway Law.** A estrutura do código deve tornar **fácil** extrair um BC num microsserviço amanhã. Se uma decisão dificulta isso, é a decisão errada.
5. **Testes vêm antes da produção.** Cobertura mínima de **80% nos domínios críticos** (agregados, value objects, handlers de feature). Testes de arquitetura (NetArchTest) são **obrigatórios** e falham o build.
6. **Eventos cross-BC com outbox.** Toda `IDomainEvent` que vira contrato publicado passa pela outbox na mesma transação do agregado.
7. **Compatibilidade de plataforma antes de fetichismo técnico.** LTS vigente, versões pinning centralizado, zero surpresas em produção.
8. **Segurança desde o primeiro commit.** Validação de entrada, segredos fora do código, autenticação administrada por `Identidade & Acesso` (nunca reimplementada por BC).

---

## 3. Stack tecnológica

### 3.1 Stack pinning (jun/2026)

| Camada | Pacote / Tecnologia | Versão alvo | Licença | URL oficial | Notas |
|---|---|---|---|---|---|
| Runtime | .NET | **10.0.8** (LTS, EOL 2028-11-14) | MIT | https://dotnet.microsoft.com/download/dotnet/10.0 | Confirmado via release-notes JSON do dotnet/core |
| Linguagem | C# | 14 | MIT | https://learn.microsoft.com/dotnet/csharp/ | Acompanha .NET 10 |
| Framework web | ASP.NET Core (Minimal APIs) | 10.0.8 | MIT | https://learn.microsoft.com/aspnet/core/ | Sem MVC controllers (ver §4) |
| Orquestração local | .NET Aspire | **13.1.2** (GA) | MIT | https://github.com/dotnet/aspire/releases/tag/v13.1.2 | AppHost + service discovery + health checks + OpenTelemetry |
| ORM | EF Core | **10** (LTS) | MIT | https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/whatsnew | Migrations, change tracking, JSONB |
| Provider PG | Npgsql.EntityFrameworkCore.PostgreSQL | **10.0** | MIT | https://www.npgsql.org/efcore/release-notes/10.0.html | JSONB, range types, full-text search |
| Banco | PostgreSQL | **17.x (LTS)** | PostgreSQL License | https://www.postgresql.org/ | 18.x aceitável se for a vigente no bootstrap da feature 01 |
| Event bus | **Wolverine** | **6.5.1** (2026-06-06) | **MIT** | https://github.com/JasperFx/wolverine/releases/tag/V6.5.1 | Outbox nativo, in-process + broker unificado, OSS puro |
| Migrations | EF Core Migrations | alinhado a EF Core 10 | MIT | https://learn.microsoft.com/ef/core/managing-schemas/migrations/ | Idempotente; rodam no startup do Aspire AppHost |
| Validação | FluentValidation | a fixar | MIT | https://fluentvalidation.net/ | Versão alinhada a .NET 10 LTS no bootstrap |
| Logging | OpenTelemetry (via Aspire) + Serilog | a fixar | Apache-2.0 | https://opentelemetry.io/ / https://serilog.net/ | Sinks configuráveis |
| Auth admin | JWT Bearer (provider a fixar na feature 02) | a fixar | MIT (provavelmente) | https://learn.microsoft.com/aspnet/core/security/authentication/jwt | BC `Identidade & Acesso` é o dono |
| Testes unit | xUnit | a fixar | Apache-2.0 | https://xunit.net/ | v3 quando LTS para .NET 10 |
| Testes assert | FluentAssertions | a fixar | Apache-2.0 (até v8); Xceed EULA a partir de v9 | https://fluentassertions.com/ | **Atenção:** v8+ mudou licença — preferir última v8 ou alternatives (Shouldly, AwesomeAssertions fork) |
| Testes integration | Testcontainers (.NET) | a fixar | MIT | https://dotnet.testcontainers.org/ | Sobe Postgres real em Docker para testes |
| Testes arquitetura | NetArchTest | a fixar | Apache-2.0 | https://github.com/BenMorris/NetArchTest | Regras estruturais automatizadas |
| Containerização | Docker + docker-compose | latest | Apache-2.0 (engine) | https://www.docker.com/ | `docker-compose.yml` na raiz |
| Orquestração (Fase 2) | Kubernetes | a fixar | Apache-2.0 | https://kubernetes.io/ | Manifestos em `/k8s`, HPA por CPU/mem |
| IaC (Fase 2) | Terraform | a fixar | BSL → MPL-2.0 | https://www.terraform.io/ | Em `/infra` |
| CI/CD | GitHub Actions | n/a | n/a (SaaS) | https://github.com/features/actions | Pipeline obrigatória: build + test + docker build + deploy |

### 3.2 Política de pinning

- Versões exatas ficam em **`versions.props`** (Central Package Management do .NET 10).
- Política: **latest patch** da major alinhada à tabela acima, na **data de início** da feature. Exemplo: ao iniciar a feature 01 em jul/2026, fixar `Wolverine = 6.5.1` (ou `6.5.x` mais recente, se já houver patch).
- Mudança de **major version** exige **ADR novo** justificando a atualização e validando que nenhum BC quebra.
- Atualizações de patch podem ser automáticas via Dependabot/Renovate, mas o PR deve passar no pipeline de teste.

### 3.3 Decisões que precisam de ADR antes de divergir

- Trocar **Wolverine** por outro mediator (improvável — MIT + outbox é exatamente o que precisamos).
- Trocar **PostgreSQL** por outro banco.
- Adicionar **outro** ORM mantendo EF Core (Dapper, Marten, etc.).
- Quebrar a regra "um BC = um schema" (multi-schema por BC só se houver razão registrada).
- Reintroduzir **MVC Controllers** (a casa é Minimal APIs).

---

## 4. Padrão arquitetural

### 4.1 Visão de alto nível
- **Monólito modular** organizado por **Bounded Contexts** (4 BCs nesta entrega, ver `design-estrategico-fases-1-2/design.md#strategic-context-sketch`).
- Cada BC é um **módulo .NET** com:
  - **Domain** (puro, sem deps externas além do `SharedKernel`)
  - **Features** (Vertical Slices por caso de uso)
  - **Infrastructure** (EF Core, integrações)
  - **Integrations/ACLs** (apenas nos BCs que **consomem** outros)
- **Shared Kernel** (`CatCar.SharedKernel`) contém **apenas** classes base táticas: `Entity<TId>`, `ValueObject`, `IAggregateRoot`, `IDomainEvent`, `IIntegrationEvent`, `Result<T>`, exceções de domínio.
- **Contracts** (`CatCar.Contracts`) é a **Published Language**: eventos de domínio serializáveis (records imutáveis) consumidos por outros BCs.

### 4.2 Estrutura de pastas (canônica)
```
CatCar.sln
src/
  Contexts/
    Atendimento/
      Domain/
      Features/<UseCase>/{Command,Handler,Validator,Endpoint}.cs
      Infrastructure/
      Integrations/                 # ACLs
    CatalogoEstoque/
    Comunicacao/
    Identidade/
  SharedKernel/
  Contracts/                        # Published Language (records de eventos)
  Host/                             # Aspire AppHost + Composition Root
  Api/                              # Endpoints (Minimal APIs) — roteamento + DI
tests/
  Contexts/<BC>.Tests/{Domain,Features,Integration}/
  Architecture.Tests/               # NetArchTest
  E2E/                              # Testcontainers + Aspire Host
versions.props                      # Central Package Management
Directory.Packages.props
```

### 4.3 Convenções de Boundary

- **Não** importar de outro BC. Nunca. Sem exceção. (Ver `CON-001` em `design-estrategico-fases-1-2/constraints.md`.)
- **Não** usar EF Core fora da pasta `Infrastructure/` ou `Integrations/` do próprio BC. (Ver `CON-002`.)
- **Não** colocar tipos de domínio no `SharedKernel`. (Ver `CON-003`.)
- **Não** misturar pastas técnicas (`Services/`, `Controllers/`, `Repositories/`) com pastas de features. (Ver `CON-005`.)
- **Não** chamar outro BC diretamente. Toda comunicação é via **handler Wolverine** publicado por outbox. (Ver `CON-007` e `CON-008`.)

### 4.4 API HTTP

- **Minimal APIs** exclusivamente. Sem `Controller` MVC.
- Mapeamento via `MapGroup` por BC: `app.MapGroup("/api/atendimento").MapAbrirOSEndpoint();` etc.
- **OpenAPI** gerado do código (Swashbuckle.AspNetCore ou built-in OpenAPI do .NET 10) e servido em `/swagger`.
- Versionamento: prefixo `/api/v1/` desde o primeiro endpoint.

---

## 5. Convenções de código

### 5.1 Linguagem e estilo
- **C# 14**, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- **Idiomas:** **everything in English** — code, class/method names, comments, logs, domain error messages, resource file keys. No Portuguese in source code. Domain terms in the ubiquitous language (Portuguese for this Brazilian domain) translate to English in code: `OrdemDeServico` → `WorkOrder`, `Orcamento` → `Budget`, etc.
- **Formatador:** `dotnet format` (padrão) com `editorconfig` versionado.
- **Linter:** Roslyn analyzers via `Microsoft.CodeAnalysis.NetAnalyzers` + `StyleCop.Analyzers` (a fixar severidade na feature 01).
- **Imports:** `using` globais via `<ImplicitUsings>` — não declarar `using` redundante.

### 5.2 Naming
| Element | Convention | Example |
|---|---|---|
| Folders | `PascalCase` | `Features/OpenWorkOrder/` |
| Classes | `PascalCase` | `OpenWorkOrderHandler` |
| Interfaces | `IPascalCase` | `IAggregateRoot` |
| Methods | `PascalCase` (verb) | `IssueBudget(...)` |
| Private fields | `_camelCase` | `_customerId` |
| Local variables | `camelCase` | `customerId` |
| Parameters | `camelCase` | `budgetId` |
| Constants | `PascalCase` | `DefaultTokenTtlInDays` |
| Enums | `PascalCase` type, `PascalCase` members | `WorkOrderStatus.AwaitingApproval` |
| Event records | suffix `Event` | `BudgetIssuedEvent` |
| Command records | suffix `Command` | `OpenWorkOrderCommand` |
| Handlers | suffix `Handler` | `OpenWorkOrderHandler` |
| Tests | `MethodName_StateUnderTest_ExpectedBehavior` | `Issue_WhenWorkOrderInDiagnosis_ShouldChangeStatusToAwaitingApproval` |

### 5.3 Comentários
- **Comments must be in English.** Code explains itself; comments explain **why**, not **what**.
- XML doc comments on **public APIs** in `Host/` and `Api/` — in English.
- No `// TODO` without ticket/link. Use `// TODO(phase-2): ...` with an open issue.

### 5.4 Branches
- `main` — sempre deployable.
- `feature/<id>-<slug>` — features em desenvolvimento (ex: `feature/03-catalogo-servicos-pecas-estoque`).
- `fix/<id>-<slug>` — correções.
- `release/<semver>` — opcional para Fase 2.

### 5.5 Commits
- **Conventional Commits** (`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`).
- Mensagem em inglês ou português, mas consistente por repo.

---

## 6. Segurança

### 6.1 Autenticação
- **Administrativa:** JWT Bearer com chave simétrica (HS256) na Fase 1, considerar RS256 com JWKS na Fase 2. Provider específico fica para a feature `02-identidade-acesso-administrativo`.
- **Cliente:** **não** usa login. O `TokenDeAcessoExterno` é o mecanismo. Token de uso único, vinculado a um `OrcamentoId`, com TTL (sugestão 7 dias).
- Senhas de admin: hash com **BCrypt** ou **Argon2id** (preferência por Argon2id via `Konscious.Security.Cryptography.Argon2`).
- Senhas nunca em log, nunca em response, nunca em URL.

### 6.2 Validação de entrada
- Toda entrada externa (HTTP, message handler, evento de broker) passa por **FluentValidation** antes de chegar ao domínio.
- Validação de **CPF** e **CNPJ** via algoritmo de dígitos verificadores (não só regex).
- Validação de **placa** no formato Mercosul (LLLNLNN) e antigo (LLLNNNN).
- Erros de validação retornam `ProblemDetails` (RFC 7807) com `traceId`.

### 6.3 Segredos
- **Nunca** em código ou `appsettings.json` versionado.
- **Desenvolvimento:** `.NET User Secrets` ou `appsettings.Development.json` ignorado pelo `.gitignore`.
- **Produção (Fase 2):** Kubernetes Secrets montado via External Secrets Operator (a confirmar na feature 08) ou variáveis de ambiente injetadas pelo cluster.
- **Templates:** `appsettings.example.json` versionado com placeholders.

### 6.4 Transporte
- TLS 1.2+ obrigatório em produção.
- HSTS habilitado em produção.
- CORS restrito a origens conhecidas (whitelist em config).

### 6.5 OWASP / Análise
- Scan automático no CI: `dotnet list package --vulnerable --include-transitive` em todo PR.
- Ferramenta adicional: `Trivy` para scan de imagem Docker (a partir da feature 01).
- Análise manual de vulnerabilidades no relatório final da Fase 1 (entregável do enunciado).

---

## 7. Testes e qualidade

### 7.1 Pirâmide
- **Unit (dominam):** agregados, value objects, handlers de feature, validators.
- **Integration (apoio):** handlers com EF Core + Postgres real via Testcontainers.
- **Architecture (obrigatório):** NetArchTest garantindo:
  - Nenhum módulo importa outro BC.
  - Domain não depende de EF Core.
  - SharedKernel não tem tipos de negócio.
  - Cada BC tem seu próprio schema.
- **E2E (opcional, depois do MVP):** cenários críticos ponta-a-ponta.

### 7.2 Cobertura
- **Mínima 80%** nos domínios críticos: agregados, value objects, handlers de feature.
- Code coverage publicado em PR (`coverlet.collector` + reportgenerator).
- Build falha se cobertura cair abaixo do limite.

### 7.3 Disciplina test-first
- **Iron law:** nenhum código de produção sem teste falhado que o desenvolvedor **viu falhar**.
- Cada task atômica do `tasks.md` tem um passo explícito de teste (Red → Green → Refactor).
- `@reviewer` verifica o ciclo; ver skill `test-first-discipline`.

### 7.4 Ferramentas
- `xUnit` (test runner) + `FluentAssertions` (asserts) **na versão 8.x** (Apache-2.0); a partir da v9 mudou licença — preferir v8 LTS ou fork community (a decidir no bootstrap da feature 01).
- `NSubstitute` ou `Moq` (a fixar) para mocks de unidade.
- `Testcontainers.PostgreSql` para integration tests com Postgres real.
- `NetArchTest` para regras arquiteturais.
- `Bogus` para dados de teste realistas.

---

## 8. Observabilidade

### 8.1 Logs
- **Structured logging** com Serilog.
- Convenção: `LogInformation("OS {OrdemDeServicoId} mudou de {StatusAnterior} para {StatusAtual}", ...)` — placeholders, não concatenação.
- Correlation ID em todas as mensagens (propagado via `HttpContext.TraceIdentifier` ou `Activity.Current`).
- Níveis: `Information` para domínio, `Warning` para retry/recuperação, `Error` para falhas de invariante, `Critical` para falhas de saúde do sistema.

### 8.2 Tracing
- **OpenTelemetry** (já no Aspire) cobrindo:
  - HTTP (entrada/saída).
  - EF Core (queries).
  - Wolverine (handlers).
- Spans nomeados por handler: `Wolverine.Handler.AbrirOS`.

### 8.3 Métricas
- Métricas básicas de OS: contador por status, histograma de duração por etapa (Recebida→Entregue), contagem de orçamentos pendentes há mais de X dias.
- Expostas via OpenTelemetry → Prometheus (Fase 2) ou Application Insights / Aspire Dashboard (dev).

### 8.4 Health checks
- `/health/live` — liveness (process up).
- `/health/ready` — readiness (DB alcançável, outbox relay rodando, dependências externas respondendo).
- Implementados via `Microsoft.Extensions.Diagnostics.HealthChecks` + Aspire.

---

## 9. Persistência e dados

### 9.1 EF Core
- **DbContext por BC** (nunca compartilhado). Ver `CON-006`.
- **Schema por BC** no Postgres: `atendimento`, `catalogo_estoque`, `comunicacao`, `identidade`.
- **Migrations** com nomes `<BC>_<seq>_<descricao>` (ex: `Atendimento_001_Inicial`).
- **Conventions:**
  - Tabela: `snake_case` plural (`ordens_de_servico`).
  - Coluna: `snake_case` (`cliente_id`).
  - PK: `id` (bigint/UUID a fixar — recomendação: UUID v7 gerado no domínio, não no DB).
  - FK: `<tabela_referenciada>_id`.
  - Audit: `criado_em`, `atualizado_em` populados por interceptor EF Core.

### 9.2 Outbox
- Tabela `outbox_messages` por BC (cada BC com a sua).
- Schema: `id`, `message_type`, `payload` (JSONB), `created_at`, `processed_at`.
- Relay: job em background no Aspire AppHost que publica mensagens não processadas para o transport (in-process na Fase 1; RabbitMQ/Kafka na Fase 2).

### 9.3 Concorrência
- `xmin` do Postgres (row version) como `uint` na entidade para optimistic concurrency.
- Conflito → `DbUpdateConcurrencyException` → handler decide retry ou erro de domínio.

### 9.4 Transações
- **Por agregado:** uma transação por operação de agregado.
- **Cross-agregado no mesmo BC:** permitido (mesma transação).
- **Cross-BC:** **proibido** em transação única. Usar outbox/eventos. (Ver `CON-007`/`CON-008`.)

---

## 10. DevX local

### 10.1 Subir o ambiente
- **Um comando:** `dotnet run --project src/Host/CatCar.AppHost.csproj` (Aspire AppHost sobe Postgres, RabbitMQ opcional, API, e abre o Aspire Dashboard).
- Ou via docker-compose: `docker-compose up` (sem Aspire Dashboard, mais leve).
- README com pré-requisitos (Docker, .NET 10 SDK, dotnet-ef tool).

### 10.2 Seed de dados
- Migration `seed` opcional em dev (cria cliente, veículo, serviços e peças de exemplo).
- NUNCA rodar seed em produção.

### 10.3 Hot reload
- Habilitado em dev (`dotnet watch`).
- EF Core hot-reload de model NÃO habilitado (migrations explícitas).

---

## 11. Versionamento e fluxo de trabalho

### 11.1 Versionamento do produto
- **SemVer** (`MAJOR.MINOR.PATCH`) a partir do primeiro release da Fase 1.
- Tags em `main` viram releases.
- Changelog mantido em `CHANGELOG.md` (gerado a partir de Conventional Commits na Fase 2).

### 11.2 Fluxo de PR
1. Branch a partir de `main`.
2. Commits em Conventional Commits.
3. PR aberto contra `main` dispara pipeline: build → test → coverage → SAST → docker build.
4. Code review obrigatório (pelo menos 1 aprovação, mesmo solo).
5. Merge squash para manter `main` linear.

### 11.3 Pipeline CI (Fase 1)
- Build: `dotnet build -c Release`
- Test: `dotnet test -c Release --collect:"XPlat Code Coverage"`
- Coverage gate: ≥ 80% nos domínios críticos.
- SAST: `dotnet list package --vulnerable --include-transitive`
- Docker build: `docker build -t catcar:${{ github.sha }} .`

### 11.4 Pipeline CI (Fase 2 — `/k8s`, `/infra`, `.github/workflows/`)
- + Deploy em cluster K8s.
- + Terraform plan/apply.
- + Image scan (Trivy).

---

## 12. Critérios de prontidão por entrega

### 12.1 Por feature (`@implementator` → `@developer` → `@reviewer` → `@dredd`)
- [ ] `design.md` aprovado (quando aplicável).
- [ ] `spec.md` (EARS) com ACs testáveis e IDs estáveis.
- [ ] `tasks.md` com tasks atômicas citando AC IDs e arquivos.
- [ ] Testes cobrindo cada AC (test-first).
- [ ] Cobertura dentro do limite.
- [ ] NetArchTest sem regressão.
- [ ] SAST limpo.
- [ ] Docker build local funciona.
- [ ] OpenAPI atualizado.
- [ ] README da feature atualizado (se relevante).

### 12.2 Por release (Fase 1)
- [ ] Todas as features 02–07 fechadas.
- [ ] Coverage global ≥ 80% nos domínios críticos.
- [ ] SAST/SCA limpo.
- [ ] Documento DDD completo (Event Storming + diagrama de contextos).
- [ ] Vídeo demonstrativo.
- [ ] Relatório de vulnerabilidades.
- [ ] Documento de entrega (PDF).
- [ ] Repositório compartilhado com `soat-architecture`.

### 12.3 Por release (Fase 2)
- [ ] Features 01 e 08 fechadas.
- [ ] Manifests K8s aplicados em cluster funcional.
- [ ] Terraform aplicado (cluster + DB).
- [ ] Pipeline CI/CD verde.
- [ ] HPA funcional.
- [ ] Vídeo demonstrando deploy + CI/CD + consumo de APIs + escalabilidade.

---

## 13. Riscos aceitos (com compensação)

| ID | Risco | Compensação |
|---|---|---|
| R-001 | Outbox sem relay ativo pode crescer | Job de relay no Aspire AppHost; alarme em prod (Fase 2) |
| R-002 | `SharedKernel` virar lixão | AD-R obrigatório para qualquer adição; revisão periódica |
| R-003 | `Atendimento/OS` ficar grande | Vertical Slices por fase operacional; gatilho para split |
| R-004 | Acoplamento `Identidade ↔ BCs` via claims | ACL no consumidor + testes específicos na feature 02 |
| R-005 | FluentAssertions mudou de licença na v9 | Travar em v8 LTS ou migrar para fork community; decidir no bootstrap da feature 01 |
| R-006 | Wolverine é projeto single-maintainer (Jeremy Miller) | Risco mitigado por MIT + base de código madura + modelo de handler similar ao MediatR (familiar) |
| R-007 | .NET 10 ainda em patching (10.0.8) | Pinning central; upgrade de patch rápido via Dependabot |
| R-008 | Fase 2 (K8s, Terraform) ainda não detalhada | Feature 08 vai detalhar; manter o monólito portátil |

---

## 14. Política de atualização desta constitution

- Mudanças **exigem** AD-R com justificativa, alternativas, e trade-offs.
- Mudanças de **stack** (linguagem, framework, banco) só com aprovação explícita + plano de migração.
- Mudanças de **padrão arquitetural** (Vertical Slice, Clean Arch) só com evidência (spike, benchmark).
- Mudanças de **regra mecânica** (constraints) viram novas linhas em `constraints.md`, não edição desta constitution.
- Revisão periódica: ao final de cada feature grande (1, 5, 6, 8), abrir issue "Revisar constitution" e atualizar se necessário.
