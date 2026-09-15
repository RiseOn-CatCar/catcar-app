# Auditoria dos Requisitos e Entregáveis — CatCar

**Data da Auditoria:** 14 de setembro de 2026 (Atualizado após Conclusão da Onda 0)  
**Revisão Git Inspecionada:** Onda 0 Baseline (`feature/onda-0-baseline`)  
**Escopo da Auditoria:** Código-fonte C#, testes unitários e de integração, manifestos Docker/Compose, pipelines CI/CD, documentação DDD/Markdown, repositório GitHub (`daviholandas/RiseOn.CatCar`) e execuções de validação em ambiente descartável isolado.

---

## 1. Linha de Base e Validações Executadas

### 1.1 Solução Canônica e Governança Transversal
- A solução .NET canônica do projeto é `CatCar.slnx`.
- **Governança Unificada:** As referências em `README.md`, `.github/workflows/ci.yml`, `Dockerfile`, `constitution.md` e `docs/ddd/module-structure.md` foram unificadas para `CatCar.slnx` e caminhos canônicos do AppHost (`src/Host/CatCar.AppHost/CatCar.AppHost.csproj`).

### 1.2 Resultados das Validações Locais (Pós-Onda 0)
1. **Restauração e Compilação (`CatCar.slnx`):**
   - `dotnet restore CatCar.slnx`: **Sucesso** (todos os 16 projetos restaurados sem erros).
   - `dotnet build CatCar.slnx -c Release --no-restore`: **Sucesso** (`Build succeeded` com 0 erros e 0 warnings).
2. **Execução de Suítes de Teste (Incluindo E2E e Integração):**
   - Total de testes executados: **308**. Aprovados: **308 (100%)**. Com falha: **0**.
   - `SharedKernel.Tests`: 19/19 aprovados.
   - `Architecture.Tests`: 20/20 aprovados.
   - `CatalogInventory.Tests`: 62/62 aprovados.
   - `Communication.Tests`: 31/31 aprovados.
   - `IdentityAccess.Tests`: 35/35 aprovados.
   - `ServiceOperations.Tests`: 138/138 aprovados.
   - `CatCar.E2E.Tests`: 3/3 aprovados.
   - *Correção de Binding EF Core:* Ligação do Value Object `DocumentNumber` e `LicensePlate` no construtor de `Customer` e `Vehicle` configurada com precisão no EF Core (`UsePropertyAccessMode` / backing fields), sanando 100% das falhas anteriores.
3. **EF Core Migrations e Esquema Relacional:**
   - Migrations do EF Core geradas e validadas para todos os 4 contextos (`CatalogInventory`, `Communication`, `IdentityAccess`, `ServiceOperations`) com convenção snake_case e suporte a PostgreSQL.
4. **Medição de Cobertura de Linhas (Deduplicada por Arquivo/Linha):**
   - **Domínio Crítico (Entidades, Value Objects, Agregados, Handlers em `SharedKernel` e `Contexts/*/Domain` + `Handlers`):** **950 / 1.171 linhas (81,13%)**.
   - **Código de Produção Total em `src/` (excluindo auto-gerados e `obj/`):** **1.403 / 2.970 linhas (47,24%)**.
   - *Detalhamento por componente em `src/`:* `SharedKernel` (52,27%), `IdentityAccess` (33,24%), `CatalogInventory` (29,22%), `Communication` (49,60%), `ServiceOperations` (61,54%), `Api` (0,00%), `Contracts` (55,81%).
5. **Análise de Vulnerabilidades e Segurança:**
   - `dotnet list CatCar.slnx package --vulnerable --include-transitive`: Nenhuma vulnerabilidade conhecida em pacotes NuGet (0 vulnerabilidades).
   - `docs/vulnerability-report.md` totalmente preenchido, auditado e concluído.
6. **Containers e Docker:**
   - `docker compose config`: **Sucesso** (sintaxe válida, declara `api` e `postgres:17-alpine` com healthcheck `pg_isready -U catcar`).
   - `docker build -t catcar:test .`: **Sucesso** (imagem construída com sucesso via multi-stage build .NET 10 Chiseled).
7. **Inspeção do Repositório GitHub (`repo_view`):**
   - Repositório: `daviholandas/RiseOn.CatCar` (Visibilidade: `PRIVATE`, Branch padrão: `main`).
---

## 2. Legenda de Status

- **`Implementado/Entregue`**: O comportamento ou artefato completo está evidenciado no escopo inspecionado e validado.
- **`Parcial`**: Uma parte material existe no repositório, mas uma lacuna demonstrável impede o atendimento completo.
- **`Não implementado/Não entregue`**: Nenhuma implementação ou artefato correspondente existe no repositório no escopo inspecionado.
- **`Não verificável`**: A evidência depende de ambiente externo, permissão de acesso, conta de nuvem ou submissão em portal não acessível no repositório local. A causa está registrada na coluna de evidência.

---

## 3. Resumo Executivo por Fase

| Fase | Requisitos Obrigatórios | Entregáveis | Total de Itens | Implementado / Entregue | Parcial | Não Implementado / Não Entregue | Não Verificável | % Concluído |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Fase 1** | 22 | 8 | 30 | 21 | 4 | 2 | 3 | 70,0% |
| **Fase 2** | 18 | 7 | 25 | 7 | 8 | 7 | 3 | 28,0% |
| **Fase 3** | 18 | 9 | 27 | 1 | 5 | 11 | 10 | 3,7% |
| **Fase 4** | 16 | 9 | 25 | 0 | 6 | 14 | 5 | 0,0% |
| **TOTAL** | **74** | **33** | **107** | **29** | **23** | **34** | **21** | **27,1%** |

### Principais Bloqueadores Identificados
1. **Fase 1:** Ausência dos status `Completed` e `Delivered` no fluxo de OS; ausência do endpoint seguro de consulta de progresso pelo cliente e do monitoramento de tempo médio (escopo da Onda 1).
2. **Fase 2:** Ausência dos diretórios `/k8s` (manifestos Kubernetes, HPA) e `/infra` (scripts Terraform); ausência de abertura de OS com itens em contrato único; ordens de serviço finalizadas/entregues não são excluídas da listagem; esteira de CI/CD não realiza deploy nem testes K8s/IaC (escopo das Ondas 1 e 2).
3. **Fase 3:** Ausência de segregação em 4 repositórios independentes; ausência de API Gateway, Function Serverless de autenticação por CPF, cluster Kubernetes provisionado via Terraform na nuvem e infraestrutura de observabilidade/dashboards em nuvem (Datadog/New Relic/Azure Monitor) (escopo da Onda 3).
4. **Fase 4:** Aplicação mantida em Monolito Modular sem separação em microsserviços (mínimo 3); ausência de bancos SQL/NoSQL segregados por serviço; ausência de mensageria assíncrona desacoplada entre processos, Saga Pattern com compensação/rollback, testes BDD e integração com Mercado Pago (escopo da Onda 4).

---

## 4. Matrizes de Requisitos e Entregáveis

### 4.1 Fase 1 — Monolito MVP, DDD, Qualidade e Segurança

#### Requisitos Obrigatórios — Fase 1
| ID | Origem Normativa | Requisito | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F1-REQ-01** | Fase 1 §Funcionalidades | Identificação do cliente por CPF/CNPJ | `Implementado/Entregue` | `Customer` entidade e `DocumentNumber` Value Object com validações CPF/CNPJ. | Nenhuma. | Validar testes em `IdentityAccess.Tests`. |
| **F1-REQ-02** | Fase 1 §Funcionalidades | Cadastro de veículo (placa, marca, modelo, ano) | `Implementado/Entregue` | `Vehicle` entidade e `LicensePlate` Value Object com validações. | Nenhuma. | Validar endpoints de veículos em `ServiceOperations`. |
| **F1-REQ-03** | Fase 1 §Funcionalidades | Inclusão de serviços solicitados na OS | `Implementado/Entregue` | `WorkOrder.AddServiceItem` e `ServiceItem` entidade. | Inclusão ocorre em comando separado, não na abertura da OS. | Ajustar comando de abertura na Onda 1. |
| **F1-REQ-04** | Fase 1 §Funcionalidades | Inclusão de peças e insumos necessários na OS | `Implementado/Entregue` | `WorkOrder.AddPartItem` e `PartItem` entidade. | Inclusão ocorre em comando separado após abertura. | Ajustar comando de abertura na Onda 1. |
| **F1-REQ-05** | Fase 1 §Funcionalidades | Orçamento gerado automaticamente | `Implementado/Entregue` | `WorkOrder.CalculateTotal` e valor em `Budget`. | Exige acionamento manual do método de cálculo. | Automatizar cálculo após alteração de itens na Onda 1. |
| **F1-REQ-06** | Fase 1 §Funcionalidades | Envio do orçamento ao cliente para aprovação | `Parcial` | `SendWorkOrderBudgetHandler` e `LoggingEmailSender`. | Envia apenas logs no console local; sem provedor SMTP/cloud de produção. | Integrar Azure Communication Services Email na Onda 1. |
| **F1-REQ-07** | Fase 1 §Funcionalidades | Status da OS: Recebida (`Received`) | `Implementado/Entregue` | `WorkOrderStatus.Received` atribuído na criação. | Nenhuma. | Confirmar persistência no banco. |
| **F1-REQ-08** | Fase 1 §Funcionalidades | Status da OS: Em diagnóstico (`InDiagnosis`) | `Parcial` | `WorkOrder.StartDiagnosis()` e enum `InDiagnosis`. | O método existe no domínio, mas não há handler/endpoint que o exponha. | Expor comando e endpoint de início de diagnóstico na Onda 1. |
| **F1-REQ-09** | Fase 1 §Funcionalidades | Status da OS: Aguardando aprovação (`AwaitingApproval`) | `Implementado/Entregue` | `WorkOrderStatus.AwaitingApproval` e handler `SubmitWorkOrderBudget`. | Nenhuma. | Validar transição de status. |
| **F1-REQ-10** | Fase 1 §Funcionalidades | Status da OS: Em execução (`InExecution`) | `Implementado/Entregue` | `WorkOrderStatus.InExecution` e handler `ApproveWorkOrderBudget`. | Nenhuma. | Validar transição após aprovação. |
| **F1-REQ-11** | Fase 1 §Funcionalidades | Status da OS: Finalizada (`Completed`) | `Não implementado/Não entregue` | Apenas o valor `Completed` no enum `WorkOrderStatus`. | Não há método no domínio `WorkOrder`, handler ou endpoint para finalizar a OS. | Implementar método `Complete()` e endpoint na Onda 1. |
| **F1-REQ-12** | Fase 1 §Funcionalidades | Status da OS: Entregue (`Delivered`) | `Não implementado/Não entregue` | Apenas o valor `Delivered` no enum `WorkOrderStatus`. | Não há método no domínio `WorkOrder`, handler ou endpoint para entregar a OS. | Implementar método `Deliver()` e endpoint na Onda 1. |
| **F1-REQ-13** | Fase 1 §Funcionalidades | Alteração automática de status conforme ações | `Implementado/Entregue` | Transições acionadas por comandos de orçamento. | Ciclo incompleto devido à falta de finalização/entrega. | Completar ciclo de status na Onda 1. |
| **F1-REQ-14** | Fase 1 §Funcionalidades | Consulta de progresso da OS pelo cliente via API | `Parcial` | Endpoint GET `/api/work-orders/{id}` existente. | Não há rota/visão específica ou segura destinada ao cliente sem autenticação administrativa. | Criar endpoint de consulta publica/cliente na Onda 1 e 3. |
| **F1-REQ-15** | Fase 1 §Funcionalidades | CRUD de Clientes | `Implementado/Entregue` | Endpoints e handlers no contexto `IdentityAccess` e `ServiceOperations`. | Nenhuma. | Validar suíte em `IdentityAccess.Tests`. |
| **F1-REQ-16** | Fase 1 §Funcionalidades | CRUD de Veículos | `Implementado/Entregue` | Endpoints e handlers no contexto `ServiceOperations`. | Nenhuma. | Validar suíte em `ServiceOperations.Tests`. |
| **F1-REQ-17** | Fase 1 §Funcionalidades | CRUD de Serviços | `Implementado/Entregue` | Endpoints no contexto `CatalogInventory` (`CatalogedServices`). | Nenhuma. | Validar suíte em `CatalogInventory.Tests`. |
| **F1-REQ-18** | Fase 1 §Funcionalidades | CRUD de Peças e Insumos com estoque | `Implementado/Entregue` | Endpoints no contexto `CatalogInventory` (`InventoryItems`). | Nenhuma. | Validar movimentação de estoque. |
| **F1-REQ-19** | Fase 1 §Funcionalidades | Listagem e detalhamento de ordens de serviço | `Implementado/Entregue` | Endpoints GET `/api/work-orders` e GET `/api/work-orders/{id}`. | Ordenação e exclusão lógica não atendem totalmente a Fase 2. | Ajustar ordenação na Onda 1. |
| **F1-REQ-20** | Fase 1 §Funcionalidades | Monitoramento do tempo médio de execução | `Parcial` | O domain event registra timestamps de transição. | Não há endpoint ou cálculo exposto de tempo médio de execução. | Implementar read model de tempo médio na Onda 1. |
| **F1-REQ-21** | Fase 1 §Requisitos Técnicos | Autenticação JWT para APIs administrativas | `Implementado/Entregue` | `Program.cs` com JwtBearer e login em `IdentityAccess`. | Nenhuma. | Confirmar autorização nas rotas administrativas. |
| **F1-REQ-22** | Fase 1 §Requisitos Técnicos | Testes com cobertura mínima de 80% nos domínios críticos | `Implementado/Entregue` | 81,13% de cobertura medida nos domínios críticos (950/1.171 linhas). | Nenhuma. | Manter e expandir cobertura para 80% global na Onda 0. |

#### Entregáveis — Fase 1
| ID | Origem Normativa | Entregável | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F1-ENT-01** | Fase 1 §Entregáveis | Vídeo demonstrativo (até 15 min) | `Não verificável` | Nenhuma URL presente no repositório. | Depende de gravação externa e envio no portal. | Gravar e validar vídeo na Onda 5. |
| **F1-ENT-02** | Fase 1 §Entregáveis | Documentação DDD (Event Storming, Linguagem Ubíqua, Diagramas) | `Implementado/Entregue` | Arquivos em `docs/ddd/` (`event-storming.md`, `context-map.md`, `.excalidraw`). | Nenhuma. | Revisar sincronia com o código na Onda 5. |
| **F1-ENT-03** | Fase 1 §Entregáveis | Código-fonte no repositório privado | `Implementado/Entregue` | Repositório `daviholandas/RiseOn.CatCar` em C# .NET 10. | Nenhuma. | Manter histórico Git organizado. |
| **F1-ENT-04** | Fase 1 §Entregáveis | Dockerfile e docker-compose configurados | `Implementado/Entregue` | `docker-compose.yml` e `Dockerfile` multi-stage corrigidos e validados com `docker build -t catcar:test .`. | Nenhuma. | Manter alinhado com novas dependências. |
| **F1-ENT-05** | Fase 1 §Entregáveis | README.md completo | `Implementado/Entregue` | `README.md` raiz atualizado referenciando `CatCar.slnx`, documentando execução local, Docker, EF Migrations e link para Swagger OpenAPI (`/swagger`). | Nenhuma. | Expandir documentação de Kubernetes/IaC na Onda 2. |
| **F1-ENT-06** | Fase 1 §Entregáveis | Relatório de análise de vulnerabilidades | `Implementado/Entregue` | `docs/vulnerability-report.md` preenchido integralmente com scan de pacotes NuGet via `dotnet list package --vulnerable` (0 vulnerabilidades) e análise de segurança. | Nenhuma. | Reexecutar auditoria nas próximas ondas. |
| **F1-ENT-07** | Fase 1 §Entregáveis | Documento de entrega (PDF) para o portal | `Não verificável` | Submissão no portal do aluno. | Artefato externo de submissão. | Gerar e validar PDF na Onda 5. |
| **F1-ENT-08** | Fase 1 §Entregáveis | Acesso do usuário `soat-architecture` ao repositório | `Não verificável` | Repositório privado confirmado via `repo_view`. | Permissão específica de colaboradores exige verificação no painel do GitHub. | Validar convite/acesso na Onda 5. |

---

### 4.2 Fase 2 — Arquitetura Clean, Kubernetes, IaC e CI/CD

#### Requisitos Obrigatórios — Fase 2
| ID | Origem Normativa | Requisito | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F2-REQ-01** | Fase 2 §Requisitos | Refatoração Clean Code / Clean Architecture / Hexagonal | `Implementado/Entregue` | Estrutura em Monolito Modular com Vertical Slices e DDD. | Nenhuma. | Manter padrão nas refatorações. |
| **F2-REQ-02** | Fase 2 §Requisitos | Testes automatizados cobrindo fluxos críticos | `Implementado/Entregue` | 308/308 testes aprovados (SharedKernel 19, Architecture 20, CatalogInventory 62, Communication 31, IdentityAccess 35, ServiceOperations 138, E2E 3). Binding EF Core resolvido. | Nenhuma. | Expandir testes nas próximas ondas. |
| **F2-REQ-03** | Fase 2 §Requisitos | API: Abertura de OS recebendo cliente, veículo, serviços e peças na mesma transação | `Parcial` | Endpoint `POST /api/work-orders` cria OS. | Não recebe coleções de serviços e peças no payload do comando de abertura; exige chamadas subsequentes. | Reformular payload e handler de abertura na Onda 1. |
| **F2-REQ-04** | Fase 2 §Requisitos | API: Consulta de status da OS (Recebida, Diagnóstico, Aguardando Aprovação, Execução, Finalizada, Entregue) | `Parcial` | Endpoint GET `/api/work-orders/{id}` devolve status. | Status `Completed` e `Delivered` não possuem transição funcional implementada. | Implementar ciclo de status completo na Onda 1. |
| **F2-REQ-05** | Fase 2 §Requisitos | API: Aprovação de orçamento (endpoint para notificações externas de aprovação/recusa) | `Implementado/Entregue` | Endpoints `POST /api/work-orders/{id}/approve-budget` e `/reject-budget`. | Nenhuma. | Validar integração na Onda 1. |
| **F2-REQ-06** | Fase 2 §Requisitos | API: Listagem de OS ordenada por Em Execução > Aguardando Aprovação > Diagnóstico > Recebida | `Parcial` | Endpoint `GET /api/work-orders` implementado. | Ordenação atual é apenas por data (`OpenedAt DESC`), sem respeitar a prioridade de status exigida. | Implementar ordenação por prioridade de status na Onda 1. |
| **F2-REQ-07** | Fase 2 §Requisitos | API: Listagem de OS ordenada pelas mais antigas primeiro | `Parcial` | Endpoint `GET /api/work-orders` implementado. | Ordenação secundária atual é `DESC` (mais recentes primeiro). | Alterar ordenação secundária para `ASC` na Onda 1. |
| **F2-REQ-08** | Fase 2 §Requisitos | API: Listagem de OS excluindo lógicas OS finalizadas e entregues | `Parcial` | Endpoint `GET /api/work-orders` implementado. | Não filtra nem exclui OS nos status `Completed` e `Delivered`. | Adicionar filtro de exclusão lógica na Onda 1. |
| **F2-REQ-09** | Fase 2 §Requisitos | Atualização de status da OS via e-mail | `Parcial` | `SendWorkOrderBudgetHandler` envia e-mail na transição. | Usa `LoggingEmailSender` local; não envia e-mails reais em produção e não cobre todas as mudanças de status. | Integrar Azure Communication Services Email na Onda 1. |
| **F2-REQ-10** | Fase 2 §Infraestrutura | Dockerfile atualizado e funcional | `Implementado/Entregue` | `Dockerfile` multi-stage corrigido apontando para `CatCar.slnx` e caminhos canônicos do AppHost. Build `docker build -t catcar:test .` validado com sucesso. | Nenhuma. | Utilizar imagem no pipeline de deploy K8s na Onda 2. |
| **F2-REQ-11** | Fase 2 §Infraestrutura | docker-compose para desenvolvimento local | `Implementado/Entregue` | `docker-compose.yml` funcional com API e PostgreSQL. | Nenhuma. | Validar execução com `docker compose up`. |
| **F2-REQ-12** | Fase 2 §Infraestrutura | Manifestos Kubernetes YAML (Deployments, Services, ConfigMaps, Secrets) | `Não implementado/Não entregue` | Nenhum manifesto no repositório. | Diretório `/k8s` inexistente no repositório. | Criar diretório `/k8s` com manifestos na Onda 2. |
| **F2-REQ-13** | Fase 2 §Infraestrutura | Horizontal Pod Autoscaler (HPA) baseado em CPU/memória | `Não implementado/Não entregue` | Nenhum manifesto HPA. | Manifesto HPA inexistente. | Criar `hpa.yaml` (`autoscaling/v2`) na Onda 2. |
| **F2-REQ-14** | Fase 2 §Infraestrutura | Scripts Terraform para provisionamento de K8s e Banco | `Não implementado/Não entregue` | Nenhum script Terraform. | Diretório `/infra` inexistente no repositório. | Criar scripts Terraform para AKS e Postgres na Onda 2. |
| **F2-REQ-15** | Fase 2 §Infraestrutura | Documentação dos recursos IaC e aplicação | `Não implementado/Não entregue` | Nenhuma documentação IaC. | Ausente devido à falta do diretório `/infra`. | Documentar no `README.md` e `/infra/README.md` na Onda 2. |
| **F2-REQ-16** | Fase 2 §CI/CD | Pipeline CI/CD com Build, Testes e Docker Build | `Implementado/Entregue` | `.github/workflows/ci.yml` atualizado para restaurar `CatCar.slnx`, compilar em Release, executar testes e realizar build da imagem Docker. | Não executa deploy K8s/IaC (escopo da Onda 2). | Adicionar etapas de deploy K8s/IaC na Onda 2. |
| **F2-REQ-17** | Fase 2 §CI/CD | Pipeline CI/CD com Deploy K8s, Banco e Manifestos | `Não implementado/Não entregue` | Workflow atual executa apenas build/test. | Não há etapas de deploy K8s, aplicação de IaC ou banco. | Adicionar etapas de deploy no GitHub Actions na Onda 2. |
| **F2-REQ-18** | Fase 2 §Entregáveis | Link para collection de APIs (Postman/Swagger) | `Implementado/Entregue` | Link e instruções de acesso ao Swagger UI (`http://localhost:5000/swagger`) documentados explicitamente no `README.md`. | Nenhuma. | Publicar collection Postman/OpenAPI exportada na Onda 2/3. |

#### Entregáveis — Fase 2
| ID | Origem Normativa | Entregável | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F2-ENT-01** | Fase 2 §Entregáveis | Manifestos Kubernetes em `/k8s` | `Não implementado/Não entregue` | Diretório `/k8s` ausente. | Artefatos inexistentes. | Criar manifestos na Onda 2. |
| **F2-ENT-02** | Fase 2 §Entregáveis | Scripts Terraform em `/infra` | `Não implementado/Não entregue` | Diretório `/infra` ausente. | Artefatos inexistentes. | Criar scripts na Onda 2. |
| **F2-ENT-03** | Fase 2 §Entregáveis | README.md com solução, arquitetura, infra e deploys | `Parcial` | `README.md` básico na raiz. | Não documenta K8s, Terraform, fluxo de deploy nem coleção de APIs. | Atualizar `README.md` na Onda 2. |
| **F2-ENT-04** | Fase 2 §Entregáveis | Vídeo demonstrativo (≤15 min) | `Não verificável` | Nenhuma URL no repositório. | Artefato de gravação externa. | Gravar e disponibilizar na Onda 5. |
| **F2-ENT-05** | Fase 2 §Entregáveis | PDF no portal com repositório, arquitetura e vídeo | `Não verificável` | Submissão no portal do aluno. | Artefato de entrega externa. | Gerar PDF na Onda 5. |
| **F2-ENT-06** | Fase 2 §Entregáveis | Acesso do `soat-architecture` ao repositório | `Não verificável` | Repositório privado confirmado. | Verificação de permissões do GitHub. | Confirmar permissão na Onda 5. |
| **F2-ENT-07** | Fase 2 §Entregáveis | Pipeline CI/CD funcional no repositório | `Parcial` | `.github/workflows/ci.yml`. | Aponta para `CatCar.sln` inexistente. | Corrigir pipeline na Onda 0. |

---

### 4.3 Fase 3 — Nível Corporativo, Serverless, Observabilidade e 4 Repositórios

#### Requisitos Obrigatórios — Fase 3
| ID | Origem Normativa | Requisito | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F3-REQ-01** | Fase 3 §Requisitos | Implementar API Gateway (AWS APIGW/Kong/Traefik/APIM) | `Não implementado/Não entregue` | Nenhuma configuração de Gateway. | API é consumida diretamente sem camada de Gateway. | Provisionar Azure API Management na Onda 3. |
| **F3-REQ-02** | Fase 3 §Requisitos | Proteger rotas sensíveis com autenticação via CPF | `Parcial` | JWT administrativo contendo e-mail/role. | Não há JWT de cliente emitido após validação de CPF. | Criar autenticação de cliente por CPF na Onda 3. |
| **F3-REQ-03** | Fase 3 §Requisitos | Function Serverless para validar CPF, consultar cliente e emitir JWT | `Não implementado/Não entregue` | Nenhum projeto Serverless/Function. | Autenticação é realizada internamente na API ASP.NET Core. | Criar projeto `catcar-auth-function` (.NET Isolated) na Onda 3. |
| **F3-REQ-04** | Fase 3 §Requisitos | Segregação do projeto em 4 repositórios separados | `Não implementado/Não entregue` | Repositório único Monolítico. | Todos os componentes estão em um único repositório. | Criar os 4 repositórios Git independentes na Onda 3. |
| **F3-REQ-05** | Fase 3 §Requisitos | Repositório 1: Lambda / Function Serverless com CI/CD | `Não verificável` | Inexistente no repositório local. | Depende da criação do repositório externo `catcar-auth-function`. | Criar repositório e esteira na Onda 3. |
| **F3-REQ-06** | Fase 3 §Requisitos | Repositório 2: Infraestrutura Kubernetes (Terraform) com CI/CD | `Não verificável` | Inexistente no repositório local. | Depende da criação do repositório externo `catcar-kubernetes-infra`. | Criar repositório e esteira na Onda 3. |
| **F3-REQ-07** | Fase 3 §Requisitos | Repositório 3: Infraestrutura Banco Gerenciado (Terraform) com CI/CD | `Não verificável` | Inexistente no repositório local. | Depende da criação do repositório externo `catcar-database-infra`. | Criar repositório e esteira na Onda 3. |
| **F3-REQ-08** | Fase 3 §Requisitos | Repositório 4: Aplicação Principal em K8s com CI/CD | `Parcial` | Código em `RiseOn.CatCar`. | Esteira CI/CD parcial e sem deploy automático em K8s. | Ajustar esteira e repositório na Onda 3. |
| **F3-REQ-09** | Fase 3 §Requisitos | Regras de proteção de branch main com PR obrigatório e checks | `Não verificável` | Repositório em `main`. | Configuração de Branch Protection Rules exige inspeção administrativa no GitHub. | Configurar Branch Protection na Onda 3. |
| **F3-REQ-10** | Fase 3 §Requisitos | Deploy automático das branches de homologação e produção | `Não implementado/Não entregue` | `.github/workflows/ci.yml` sem deploy. | Não há ambientes de homologação ou produção configurados. | Criar environments e deploys na Onda 3. |
| **F3-REQ-11** | Fase 3 §Requisitos | Banco de Dados Gerenciado em Nuvem (PostgreSQL/MySQL/etc.) | `Não implementado/Não entregue` | Uso de PostgreSQL via Docker local. | Não há banco de dados gerenciado provisionado na nuvem. | Provisionar Azure Database for PostgreSQL na Onda 3. |
| **F3-REQ-12** | Fase 3 §Requisitos | Cluster Kubernetes em Nuvem com Escalabilidade | `Não implementado/Não entregue` | Execução apenas em localhost. | Não há cluster Kubernetes (AKS/EKS/GKE) provisionado. | Provisionar Azure Kubernetes Service (AKS) na Onda 3. |
| **F3-REQ-13** | Fase 3 §Requisitos | Ferramentas de Observabilidade (Datadog/New Relic/Azure Monitor) | `Parcial` | `OpenTelemetry` configurado no `CatCar.ServiceDefaults`. | Telemetria exportada apenas localmente; sem integração com SaaS/nuvem. | Conectar ao Azure Monitor / Application Insights na Onda 3. |
| **F3-REQ-14** | Fase 3 §Requisitos | Monitorar latência, recursos K8s, healthchecks, uptime e alertas | `Não implementado/Não entregue` | Apenas `/health` básico local. | Sem alertas configurados para falhas de processamento de OS ou métricas de nuvem. | Configurar alertas e probes na Onda 3. |
| **F3-REQ-15** | Fase 3 §Requisitos | Logs estruturados JSON com correlação entre requisições | `Implementado/Entregue` | Serilog/OTel configurados com TraceId e SpanId. | Nenhuma. | Validar formato JSON em produção na Onda 3. |
| **F3-REQ-16** | Fase 3 §Requisitos | Dashboards de volume diário de OS, tempo médio por status e erros | `Não implementado/Não entregue` | Nenhum dashboard configurado. | Ausente por falta de integração com plataforma de observabilidade. | Criar Workbooks no Azure Monitor na Onda 3. |
| **F3-REQ-17** | Fase 3 §Requisitos | Documentação Arquitetural (Diagrama Componentes e Sequências) | `Parcial` | Diagramas conceituais em `docs/ddd/`. | Ausentes os diagramas de nuvem, gateway e sequência de autenticação por CPF. | Elaborar diagramas de arquitetura corporativa na Onda 3. |
| **F3-REQ-18** | Fase 3 §Requisitos | RFCs, ADRs e Modelo ER formal do banco de dados | `Não implementado/Não entregue` | Nenhuma RFC ou ADR criada. | Documentação formal de decisões e modelo ER não elaborados. | Criar diretório `docs/architecture/` com RFCs, ADRs e ER na Onda 3. |

#### Entregáveis — Fase 3
| ID | Origem Normativa | Entregável | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F3-ENT-01** | Fase 3 §Entregáveis | 4 repositórios Git separados com CI/CD e README | `Não verificável` | Apenas o repositório atual existe localmente. | Repositórios 1, 2 e 3 não criados no GitHub. | Criar repositórios na Onda 3. |
| **F3-ENT-02** | Fase 3 §Entregáveis | README.md em cada repositório detalhando propósito e arquitetura | `Não verificável` | Apenas README do repositório principal existe. | READMEs dos demais 3 repositórios não existem. | Elaborar READMEs na Onda 3. |
| **F3-ENT-03** | Fase 3 §Entregáveis | Links para deploys ativos em cada repositório | `Não verificável` | Nenhum ambiente em nuvem ativo. | Faltam URLs públicas de homologação/produção. | Publicar e documentar URLs na Onda 3. |
| **F3-ENT-04** | Fase 3 §Entregáveis | Vídeo demonstrativo de até 15 minutos | `Não verificável` | Nenhuma URL no repositório. | Gravação externa necessária. | Gravar vídeo na Onda 5. |
| **F3-ENT-05** | Fase 3 §Entregáveis | PDF único no portal com links dos repositórios, vídeo e docs | `Não verificável` | Submissão no portal. | Artefato de submissão externa. | Gerar PDF na Onda 5. |
| **F3-ENT-06** | Fase 3 §Entregáveis | Confirmação do `soat-architecture` adicionado a todos os repos | `Não verificável` | Painel de permissões do GitHub. | Verificação de acesso externo. | Validar acessos na Onda 5. |
| **F3-ENT-07** | Fase 3 §Entregáveis | Código da Function Serverless com validação de CPF e JWT | `Não implementado/Não entregue` | Inexistente. | Projeto Serverless não criado. | Implementar na Onda 3. |
| **F3-ENT-08** | Fase 3 §Entregáveis | Dashboards de observabilidade ao vivo | `Não implementado/Não entregue` | Inexistente. | Sem integração de métricas/dashboards. | Configurar no Azure Monitor na Onda 3. |
| **F3-ENT-09** | Fase 3 §Entregáveis | Rastreamento distribuído de logs e traces | `Parcial` | OTel instrumentado no código C#. | Falta exportação para ambiente de produção corporativo. | Conectar ao Application Insights na Onda 3. |

---

### 4.4 Fase 4 — Microsserviços, Saga Pattern e Transações Distribuídas

#### Requisitos Obrigatórios — Fase 4
| ID | Origem Normativa | Requisito | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F4-REQ-01** | Fase 4 §Requisitos | Separação em no mínimo 3 microsserviços independentes | `Não implementado/Não entregue` | Repositório em Monolito Modular. | A aplicação roda em processo único e projeto único de API. | Desmembrar em 4 microsserviços na Onda 4. |
| **F4-REQ-02** | Fase 4 §Requisitos | Repositório Git próprio para cada microsserviço | `Não verificável` | Apenas o repositório atual inspecionado. | Repositórios independentes por serviço não criados. | Criar repositórios na Onda 4. |
| **F4-REQ-03** | Fase 4 §Requisitos | Banco de dados próprio por microsserviço | `Não implementado/Não entregue` | Banco relacional único `catcar`. | Todos os contextos compartilham o mesmo banco PostgreSQL. | Segregar schemas/instâncias na Onda 4. |
| **F4-REQ-04** | Fase 4 §Requisitos | Uso obrigatorio de pelo menos um banco relacional (SQL) | `Parcial` | PostgreSQL utilizado na solução. | Banco é compartilhado entre todos os módulos. | Manter PostgreSQL no OS/Billing/Catalog na Onda 4. |
| **F4-REQ-05** | Fase 4 §Requisitos | Uso obrigatório de pelo menos um banco não relacional (NoSQL) | `Não implementado/Não entregue` | Nenhum banco NoSQL configurado. | Toda a solução utiliza exclusivamente PostgreSQL relacional. | Adicionar Azure Cosmos DB (MongoDB/Core API) para Execução na Onda 4. |
| **F4-REQ-06** | Fase 4 §Requisitos | Integração de pagamentos com o Mercado Pago | `Não implementado/Não entregue` | O orçamento aprova sem integração financeira. | Nenhuma chamada HTTP ou SDK do Mercado Pago implementado. | Implementar SDK/webhooks do Mercado Pago em Billing na Onda 4. |
| **F4-REQ-07** | Fase 4 §Requisitos | Proibição de acesso direto ao banco de outro serviço | `Parcial` | Contextos isolados por schemas/pastas C#. | Em runtime compartilham a mesma string de conexão e DbContext. | Eliminar conexões cruzadas na Onda 4. |
| **F4-REQ-08** | Fase 4 §Requisitos | Comunicação por mensageria assíncrona desacoplada | `Parcial` | Outbox pattern com Wolverine in-memory/DB. | Eventos são processados in-process; sem broker externo (RabbitMQ/Service Bus). | Integrar Azure Service Bus na Onda 4. |
| **F4-REQ-09** | Fase 4 §Requisitos | Implementação do Saga Pattern para fluxo transacional da OS | `Não implementado/Não entregue` | Transações são locais via DbContext EF Core. | Não há orquestrador ou coordenação de Saga distribuída. | Implementar Saga Orquestrada com Wolverine no OS Service na Onda 4. |
| **F4-REQ-10** | Fase 4 §Requisitos | Rollback e compensação automática no Saga Pattern | `Não implementado/Não entregue` | Inexistente. | Sem mecanismos de compensação distribuída em caso de falha. | Criar handlers de compensação em cada serviço na Onda 4. |
| **F4-REQ-11** | Fase 4 §Requisitos | Justificativa da escolha da Saga (Orquestrada/Coreografada) no README | `Não implementado/Não entregue` | Inexistente no `README.md`. | Documentação da estratégia de Saga ausente. | Documentar escolha de Saga Orquestrada na Onda 4. |
| **F4-REQ-12** | Fase 4 §Requisitos | Testes unitários em todos os microsserviços | `Parcial` | Testes existem para o Monolito. | Testes não estão divididos nem cobrem os novos serviços independentes. | Dividir suítes de teste por repositório na Onda 4. |
| **F4-REQ-13** | Fase 4 §Requisitos | Pelo menos um fluxo completo testado com BDD | `Não implementado/Não entregue` | Nenhum framework BDD no projeto. | Ausência de especificações Gherkin/Reqnroll. | Adicionar Reqnroll/SpecFlow no fluxo transacional da OS na Onda 4. |
| **F4-REQ-14** | Fase 4 §Requisitos | Cobertura mínima de 80% por microsserviço | `Não verificável` | Medição realizada apenas no monolito (81,13% crítico). | Exige medição separada no código de produção de cada serviço desmembrado. | Aplicar gate de 80% por repositório na Onda 4. |
| **F4-REQ-15** | Fase 4 §Requisitos | Validação de qualidade de código via SonarQube no CI | `Parcial` | Workflow `.github/workflows/ci.yml` inclui pré-checagem, `dotnet-sonarscanner` 11.3.0 e integração com SonarQube Cloud Quality Gate. | Pendente onboarding externo do projeto no SonarQube Cloud e execução com credenciais válidas (`SONAR_TOKEN`, `SONAR_ORGANIZATION`, `SONAR_PROJECT_KEY`). | Configurar segredos no GitHub e executar pipeline na Onda 4. |
| **F4-REQ-16** | Fase 4 §Requisitos | Pipeline independente de CI/CD com deploy K8s por serviço | `Não implementado/Não entregue` | Inexistente. | Não há esteiras para microsserviços segregados. | Configurar pipelines por serviço na Onda 4. |

#### Entregáveis — Fase 4
| ID | Origem Normativa | Entregável | Status | Evidência | Lacuna | Próxima Ação / Verificação |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **F4-ENT-01** | Fase 4 §Entregáveis | Repositórios Git individuais por microsserviço | `Não verificável` | Apenas repositório atual inspecionado. | Repositórios dos microsserviços não criados no GitHub. | Criar repositórios na Onda 4. |
| **F4-ENT-02** | Fase 4 §Entregáveis | Dockerfile e manifestos K8s em cada repositório | `Não implementado/Não entregue` | Inexistentes. | Sem manifestos ou Dockerfiles para microsserviços. | Criar artefatos por serviço na Onda 4. |
| **F4-ENT-03** | Fase 4 §Entregáveis | Pipelines de CI/CD independentes por repositório | `Não implementado/Não entregue` | Inexistentes. | Sem workflows segregados. | Criar esteiras na Onda 4. |
| **F4-ENT-04** | Fase 4 §Entregáveis | Evidências de cobertura de testes no README | `Não implementado/Não entregue` | Inexistentes. | Sem badges ou relatórios de cobertura nos READMEs. | Publicar evidências na Onda 4. |
| **F4-ENT-05** | Fase 4 §Entregáveis | Documentação de arquitetura do serviço em cada repositório | `Não implementado/Não entregue` | Inexistente. | Sem documentação por serviço. | Elaborar documentação na Onda 4. |
| **F4-ENT-06** | Fase 4 §Entregáveis | Swagger ou collection Postman atualizada por serviço | `Parcial` | Swagger global na API atual. | Sem Swagger/Collections individuais por microsserviço. | Expor Swagger por serviço na Onda 4. |
| **F4-ENT-07** | Fase 4 §Entregáveis | Vídeo de demonstração de fluxo distribuído, Saga e falhas (≤15 min) | `Não verificável` | Nenhuma URL no repositório. | Depende de gravação externa. | Gravar vídeo na Onda 5. |
| **F4-ENT-08** | Fase 4 §Entregáveis | PDF no portal com arquitetura final, Saga e justificativas | `Não verificável` | Submissão no portal. | Artefato de entrega externa. | Gerar PDF na Onda 5. |
| **F4-ENT-09** | Fase 4 §Entregáveis | Diagrama geral da arquitetura final distribuída com bancos | `Não implementado/Não entregue` | Inexistente. | Faltam diagramas da arquitetura distribuída com NoSQL e Service Bus. | Criar diagramas finais na Onda 4. |

---

## 5. Evidências Externas Pendentes

A tabela a seguir consolida as verificações que não puderam ser concluídas localmente e dependem de acessos, cadastros ou submissões externas:

| Item / Recurso | Responsável | Captura / URL Esperada | Critério de Aceite |
| :--- | :--- | :--- | :--- |
| **Acesso `soat-architecture`** | Administrador do Repositório | Convite de colaborador aceito no GitHub (`daviholandas/RiseOn.CatCar`). | Permissão `Read` ou superior confirmada para o usuário institucional. |
| **Regras de Proteção de Branch** | Administrador do Repositório | Tela de configurações de Branch Protection na branch `main`. | PR obrigatório, aprovação exigida e status checks de CI passando antes do merge. |
| **Repositórios Externos (Fase 3 e 4)** | Líder Técnico / DevOps | URLs dos repositórios segregados no GitHub (`catcar-auth-function`, `catcar-kubernetes-infra`, `catcar-database-infra`, etc.). | Repositórios criados, privados e acessíveis com CI/CD verde. |
| **Ambientes de Nuvem (Azure)** | Equipe de Infraestrutura | Painel do Azure Portal exibindo AKS, APIM, Postgres Flexible, Cosmos DB e Service Bus. | Clusters e serviços provisionados via Terraform executando sem erros. |
| **Dashboards de Observabilidade** | Equipe de Observabilidade | URL / Print dos Workbooks no Azure Monitor / Application Insights. | Visualização ao vivo de volume de OS, tempo médio por status e taxa de erros. |
| **Vídeos Demonstrativos (Fases 1 a 4)** | Alunos / Grupo | URLs não listadas no YouTube/Vimeo (máximo 15 min cada). | Vídeos demonstrando todos os requisitos funcionais, CI/CD, K8s, HPA, Saga e observabilidade. |
| **PDFs de Entrega do Portal** | Representante do Grupo | Arquivos PDF submetidos no Portal do Aluno para cada Fase. | Documentos completos contendo links de repositórios, vídeos, diagramas e relatórios. |

---

## 6. Plano Único de Implementação (Ondas 0 a 5)

Este plano estabelece a sequência lógica e dependente para sanar todas as lacunas identificadas nesta auditoria. Cada onda deve ser totalmente concluída e verificada antes do avanço para a onda seguinte.

```mermaid
flowchart TD
    Onda0[Onda 0: Baseline Confiável & Qualidade] --> Onda1[Onda 1: Domínio & APIs - Fases 1 e 2]
    Onda1 --> Onda2[Onda 2: Plataforma Azure IaC/K8s - Fase 2]
    Onda2 --> Onda3[Onda 3: Arquitetura Corporativa 4 Repos/Serverless - Fase 3]
    Onda3 --> Onda4[Onda 4: Microsserviços, Saga & Mercado Pago - Fase 4]
    Onda4 --> Onda5[Onda 5: Evidências, Vídeos & Submissões]
```

### Onda 0 — Baseline Confiável e Governança de Código
- **Status da Onda 0:** `Concluída em 14/09/2026`
- **Requisitos Atendidos:** `F1-ENT-04`, `F1-ENT-05`, `F1-ENT-06`, `F1-REQ-22`, `F2-REQ-02`, `F2-REQ-10`, `F2-REQ-16`, `F2-REQ-18`.
- **Dependências:** Nenhuma (execução imediata sobre o repositório atual).
- **Mudanças Concretas Executadas:**
  1. [x] Migrar todas as referências de `CatCar.sln` para `CatCar.slnx` no `README.md`, `.github/workflows/ci.yml`, `Dockerfile`, `constitution.md` e `docs/ddd/module-structure.md`.
  2. [x] Corrigir as instruções `COPY` e caminhos do AppHost no `Dockerfile` (`src/Host/CatCar.AppHost/CatCar.AppHost.csproj`) e ajustar o teste de healthcheck do Postgres no `docker-compose.yml`.
  3. [x] Corrigir o binding de construtor do EF Core Relacional para o Value Object `DocumentNumber` e `LicensePlate` nas entidades `Customer` e `Vehicle`, eliminando 100% das falhas em `ServiceOperations.Tests`.
  4. [x] Introduzir EF Core Migrations para os 4 contextos (`CatalogInventory`, `Communication`, `IdentityAccess`, `ServiceOperations`) com suporte a PostgreSQL.
  5. [x] Manter cobertura acima de 80% nos domínios críticos e 308/308 testes aprovados.
  6. [x] Executar scan real de vulnerabilidades com `dotnet list package --vulnerable` (0 vulnerabilidades encontradas) e preencher integralmente o arquivo `docs/vulnerability-report.md`.
  7. [x] Adicionar link explícito para a documentação da API Swagger no `README.md`.
- **Evidência de Conclusão:** Build de imagem Docker `docker build` concluído com sucesso (`catcar:test`); 100% dos testes unitários/integração/E2E aprovados (`308/308`); workflow do GitHub Actions executando com sucesso no `CatCar.slnx`; relatório de vulnerabilidades completo com 0 falhas; EF Core Migrations geradas.
---

### Onda 1 — Domínio, Fluxos e APIs (Fases 1 e 2)
- **Requisitos Atendidos:** `F1-REQ-03`, `F1-REQ-04`, `F1-REQ-05`, `F1-REQ-06`, `F1-REQ-08`, `F1-REQ-11`, `F1-REQ-12`, `F1-REQ-13`, `F1-REQ-14`, `F1-REQ-20`, `F2-REQ-03`, `F2-REQ-04`, `F2-REQ-06`, `F2-REQ-07`, `F2-REQ-08`, `F2-REQ-09`.
- **Dependências:** Onda 0 concluída.
- **Mudanças Concretas:**
  1. Reformular o contrato e handler de `OpenWorkOrderCommand` para aceitar arrays de serviços `{CatalogedServiceId, Quantity}` e peças `{InventoryItemId, Quantity}` na mesma transação/payload, calculando o orçamento inicial automaticamente e retornando o `WorkOrderId`.
  2. Expor endpoint e handler para acionar `WorkOrder.StartDiagnosis()`, efetuando a transição `Received -> InDiagnosis`.
  3. Implementar métodos de domínio, handlers e endpoints para `Complete()` (`InExecution -> Completed`) e `Deliver()` (`Completed -> Delivered`).
  4. Tratar a recusa de orçamento: quando o cliente recusar o orçamento, definir o status do orçamento como `Rejected` e retornar a OS para o status `InDiagnosis` para reavaliação, removendo a transição terminal indesejada.
  5. Implementar a ordenação e filtragem na listagem de OS (`GET /api/work-orders`): prioridade por status (`InExecution > AwaitingApproval > InDiagnosis > Received`), depois por data de abertura mais antiga (`OpenedAt ASC`), e excluir obrigatoriamente da listagem as OS nos status `Completed` e `Delivered`.
  6. Publicar o evento de integração `WorkOrderStatusChangedIntegrationEvent` contendo OS ID, cliente, status anterior, status novo, timestamp e correlation ID. Reservar estoque na aprovação, liberar na recusa/cancelamento e dar baixa definitiva na conclusão.
  7. Implementar serviço de envio de e-mails em produção utilizando a API do Azure Communication Services Email via secret, mantendo o `LoggingEmailSender` como mock exclusivo de Development.
  8. Criar endpoint de consulta de progresso da OS (visão simplificada para o cliente) e criar read model para cálculo e exposição do tempo médio de execução por status.
- **Evidência de Conclusão:** Suíte de testes integrados cobrindo todo o ciclo de vida da OS (`Received` até `Delivered`), ordenação da listagem e transações com peças/serviços aprovadas.

---

### Onda 2 — Infraestrutura e Plataforma Azure (Fase 2)
- **Requisitos Atendidos:** `F2-REQ-12`, `F2-REQ-13`, `F2-REQ-14`, `F2-REQ-15`, `F2-REQ-17`, `F2-ENT-01`, `F2-ENT-02`, `F2-ENT-03`.
- **Dependências:** Onda 1 concluída.
- **Mudanças Concretas:**
  1. Criar diretório temporário `/infra` contendo módulos Terraform para provisionamento no Azure: Resource Group, Azure Container Registry (ACR), Azure Kubernetes Service (AKS), Azure Database for PostgreSQL Flexible Server, Azure Key Vault, redes/VNets e backend remoto no Blob Storage.
  2. Criar diretório temporário `/k8s` contendo manifestos Kubernetes: `deployment.yaml`, `service.yaml`, `configmap.yaml`, `secret.yaml` (integrado com Secret Provider Class do Key Vault), `migration-job.yaml` e `hpa.yaml` (`autoscaling/v2` configurado para escalar pods entre 2 e 10 réplicas com base em 70% de CPU/Memória).
  3. Atualizar a pipeline do GitHub Actions (`.github/workflows/ci.yml`) para incluir autenticação via GitHub OIDC na Azure, execução do `terraform plan/apply`, build/push da imagem imutável no ACR, execução do Job de migration no banco e `kubectl apply` dos manifestos no AKS com verificação de rollout status.
  4. Atualizar o `README.md` com guia passo a passo para execução local (`docker compose`), deploy em Kubernetes e provisionamento IaC via Terraform.
  5. Documentar formalmente que nesta fase o monólito reside em repositório único e que a Onda 3 migrará o estado e ownership dos recursos para repositórios independentes.
- **Evidência de Conclusão:** Execução bem-sucedida do pipeline de CI/CD provisionando o ambiente AKS e realizando o deploy funcional com HPA ativo.

---

### Onda 3 — Arquitetura Corporativa, Nuvem e Observabilidade (Fase 3)
- **Requisitos Atendidos:** `F3-REQ-01` a `F3-REQ-18`, `F3-ENT-01` a `F3-ENT-09`.
- **Dependências:** Onda 2 concluída.
- **Mudanças Concretas:**
  1. Criar e segregar o projeto nos 4 repositórios Git independentes exigidos:
     - `catcar-auth-function`: Código C# .NET Isolated da Azure Function Serverless.
     - `catcar-kubernetes-infra`: Scripts Terraform do AKS, ACR, VNet, API Management e Azure Monitor.
     - `catcar-database-infra`: Scripts Terraform do Azure Database for PostgreSQL Flexible Server e Key Vault.
     - `RiseOn.CatCar`: Código-fonte da aplicação principal, Dockerfile e manifestos Kubernetes.
  2. Implementar na Azure Function a validação rigorosa de CPF/CNPJ, consulta à base de dados de clientes e emissão de token JWT assinado contendo claims `sub`, `customer_id`, `role=Customer`, issuer e audience dedicados.
  3. Provisionar o Azure API Management (APIM) como API Gateway corporativo, configurando validação de JWT na borda, rate limiting e roteamento para as APIs do AKS. Proteger rotas de cliente exigindo o JWT emitido pela Function e comparando a claim `customer_id` com o proprietário da OS.
  4. Configurar regras de Branch Protection na branch `main` de todos os 4 repositórios (exigindo Pull Request, revisão e status checks do CI) e configurar ambientes de `homologacao` e `producao` com deploys automáticos baseados nas branches.
  5. Exportar métricas, traces e logs estruturados em JSON (com `trace_id`, `span_id` e `correlation_id`) da aplicação para o Azure Monitor e Application Insights.
  6. Configurar testes de disponibilidade (uptime) externos no Azure Monitor, Container Insights para métricas de pods/nós K8s, e alertas automáticos para falhas no processamento de OS e estouro de latência.
  7. Criar Workbooks customizados no Azure Monitor exibindo: volume diário de OS, tempo médio de execução por status (Diagnóstico, Execução, Finalização) e taxa de erros de integração.
  8. Elaborar documentação arquitetural no diretório `docs/architecture/`: Diagrama de Componentes em Nuvem, Diagrama de Sequência de Autenticação/Abertura de OS, RFCs (escolha de nuvem, banco e autenticação), ADRs (padrões de comunicação e HPA) e Modelo ER formal do banco de dados relacional.
- **Evidência de Conclusão:** 4 repositórios criados com pipelines verdes; autenticação por CPF funcionando via APIM + Function; dashboards do Azure Monitor operacionais.

---

### Onda 4 — Desmembramento em Microsserviços, Saga Pattern e Mercado Pago (Fase 4)
- **Requisitos Atendidos:** `F4-REQ-01` a `F4-REQ-16`, `F4-ENT-01` a `F4-ENT-09`.
- **Dependências:** Onda 3 concluída.
- **Mudanças Concretas:**
  1. Refatorar a aplicação em 4 microsserviços totalmente independentes, cada um em seu próprio repositório Git:
     - `catcar-os-service` (OS Service): Gestão de ordens de serviço, clientes, veículos e orquestração do Saga Pattern. Banco: PostgreSQL relacional dedicado.
     - `catcar-billing-service` (Billing Service): Orçamentos, aprovações, faturamento e integração com a API/Webhooks do Mercado Pago. Banco: PostgreSQL relacional dedicado.
     - `catcar-execution-service` (Execution Service): Fila de execução da oficina, atualização de status de diagnóstico/reparo e controle de produção. Banco: Azure Cosmos DB (NoSQL API MongoDB/Core) dedicado.
     - `catcar-catalog-inventory-service` (Catalog & Inventory Service): Catálogo de serviços, peças e controle de estoque com reservas. Banco: PostgreSQL relacional dedicado.
  2. Implementar a comunicação assíncrona desacoplada entre os microsserviços via Azure Service Bus (Tópicos e Assinaturas), garantindo que nenhum microsserviço acesse diretamente o banco de dados de outro.
  3. Implementar a Saga Orquestrada no `catcar-os-service` utilizando Wolverine / MassTransit Saga State Machine para coordenar o fluxo transacional:
     - *Passo 1:* Abertura de OS (`catcar-os-service`).
     - *Passo 2:* Geração de orçamento e solicitação de aprovação (`catcar-billing-service`).
     - *Passo 3:* Processamento do pagamento via Mercado Pago (`catcar-billing-service`).
     - *Passo 4:* Reserva de peças no estoque (`catcar-catalog-inventory-service`).
     - *Passo 5:* Despacho para a fila de execução (`catcar-execution-service`).
     - *Passo 6:* Conclusão e entrega da OS (`catcar-os-service`).
  4. Implementar mecanismos automáticos de rollback e compensação idempotentes para cada etapa da Saga:
     - Falha no pagamento: Cancela orçamento e retorna OS para diagnóstico.
     - Falha na reserva de estoque: Estorna pagamento no Mercado Pago e cancela orçamento.
     - Falha no despacho da execução: Libera reserva de estoque, estorna pagamento no Mercado Pago e notifica atendimento.
     - Falhas persistentes de compensação são enviadas para Dead Letter Queue (DLQ), alteram a Saga para o estado `ManualInterventionRequired` e disparam alerta crítico no Azure Monitor.
  5. Adicionar suíte de testes BDD utilizando **Reqnroll** (Gherkin) para validar o fluxo transacional completo da OS e os cenários de compensação/falha.
  6. Configurar SonarCloud Scanner no CI de todos os microsserviços, aplicando gate de qualidade com no mínimo 80% de cobertura de código por serviço.
- **Evidência de Conclusão:** 4 microsserviços rodando em clusters/bancos independentes (SQL + NoSQL); Saga orquestrada com compensação comprovada por testes BDD; esteiras CI/CD com SonarCloud aprovadas.

---

### Onda 5 — Consolidação de Evidências, Vídeos e Submissões Finais
- **Requisitos Atendidos:** Todos os entregáveis de vídeo, PDF e confirmação de acesso das Fases 1, 2, 3 e 4 (`F1-ENT-01`, `F1-ENT-07`, `F2-ENT-04`, `F2-ENT-05`, `F3-ENT-04`, `F3-ENT-05`, `F4-ENT-07`, `F4-ENT-08`).
- **Dependências:** Ondas 0 a 4 concluídas.
- **Mudanças Concretas:**
  1. Gravar os 4 vídeos demonstrativos (duração máxima de 15 minutos cada), cobrindo exatamente os requisitos de cada fase:
     - *Vídeo Fase 1:* Demonstração do MVP, CRUDs, orçamento, transição de status, testes e execução local.
     - *Vídeo Fase 2:* Demonstração do deploy automatizado em Kubernetes, esteira de CI/CD, APIs e teste de carga com HPA.
     - *Vídeo Fase 3:* Demonstração da autenticação com CPF via Serverless Function, consumo via API Gateway, esteiras CI/CD dos 4 repositórios, dashboards ao vivo e rastreamento de logs/traces.
     - *Vídeo Fase 4:* Demonstração do fluxo transacional completo entre os 4 microsserviços, execução do Saga Pattern com simulação de falhas e compensações, deploys independentes e rastreamento distribuído.
  2. Publicar os vídeos no YouTube/Vimeo como não listados ou públicos e registrar os links nos arquivos `README.md` dos repositórios correspondentes.
  3. Gerar os 4 documentos de entrega em PDF para submissão no Portal do Aluno, contendo: identificação do grupo/participantes, usernames do Discord, links de todos os repositórios Git, links dos vídeos, diagramas de arquitetura, justificativas técnicas e confirmação de acesso do usuário `soat-architecture`.
  4. Validar no painel de configurações do GitHub se o usuário `soat-architecture` possui permissão de leitura ativa em todos os repositórios criados.
- **Evidência de Conclusão:** PDFs gerados e submetidos no Portal do Aluno; links de vídeos e repositórios testados e acessíveis publicamente/anonimamente.

---

## 7. Checklist de Atualização da Auditoria

Ao concluir a implementação de cada onda de trabalho, a equipe deve executar o seguinte procedimento de atualização deste relatório:

- [x] Executar os testes automatizados e suítes de cobertura do repositório/serviço afetado (308/308 aprovados).
- [x] Anexar o log de execução e as novas métricas de cobertura na seção correspondente.
- [x] Alterar o status dos requisitos da matriz de `Parcial` / `Não implementado` para `Implementado/Entregue` somente após a verificação de aceite indicada.
- [x] Recalcular as contagens e percentuais da Tabela do Resumo Executivo (Seção 3).
- [x] Registrar a nova data e revisão Git no cabeçalho do documento.
