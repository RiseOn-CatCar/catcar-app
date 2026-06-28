---
feature: design-estrategico-fases-1-2
kind: tasks
status: accepted
created: 2026-06-09
updated: 2026-06-10
phase: archived
---

# Tasks — Documentação Técnica Fase 1 CatCar

## T-001: Expandir Event Storming — fluxo de criação da OS
- covers: [AC-001]
- files: [.kb/features/design-estrategico-fases-1-2/event-storming.md, .kb/features/design-estrategico-fases-1-2/event-storming.excalidraw]
- depends_on: []
- verification: O diagrama Event Storming inclui eventos de domínio, comandos, agregados, atores e políticas para: identificação do cliente por CPF/CNPJ, cadastro de veículo (placa, marca, modelo, ano), inclusão de serviços solicitados, inclusão de peças e insumos, geração automática de orçamento, e envio do orçamento ao cliente para aprovação.
- effort: medium
- status: done

## T-002: Expandir Event Storming — fluxo de acompanhamento da OS
- covers: [AC-002]
- files: [.kb/features/design-estrategico-fases-1-2/event-storming.md, .kb/features/design-estrategico-fases-1-2/event-storming.excalidraw]
- depends_on: [T-001]
- verification: O diagrama Event Storming mostra todas as 6 transições de status (Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue) com os comandos/eventos que disparam cada transição, e inclui a consulta do cliente via API para acompanhamento do progresso.
- effort: medium
- status: done

## T-003: Expandir Event Storming — gestão de peças e insumos
- covers: [AC-003]
- files: [.kb/features/design-estrategico-fases-1-2/event-storming.md, .kb/features/design-estrategico-fases-1-2/event-storming.excalidraw]
- depends_on: [T-002]
- verification: O diagrama Event Storming inclui operações de CRUD para serviços catalogados, CRUD para peças e insumos, e operações de controle de estoque (consulta de disponibilidade, reserva, consumo e reposição) com seus respectivos eventos, comandos e agregados.
- effort: medium
- status: done

## T-004: Revisar e finalizar Context Map
- covers: [AC-004]
- files: [.kb/features/design-estrategico-fases-1-2/context-map.md, .kb/features/design-estrategico-fases-1-2/context-map.excalidraw]
- depends_on: []
- verification: O Context Map exibe todos os 4 BCs com seus tipos estratégicos (Core, Supporting, Generic), o Shared Kernel tático, e todas as relações de integração com estratégias de tradução (OHS, ACL, Published Language, Customer-Supplier) conforme documentado em `design.md#ddd-context-map`.
- effort: small
- status: done

## T-005: Revisar e finalizar Module Structure
- covers: [AC-005]
- files: [.kb/features/design-estrategico-fases-1-2/module-structure.md, .kb/features/design-estrategico-fases-1-2/module-structure.excalidraw]
- depends_on: []
- verification: O Module Structure exibe a organização completa da solução .NET com todos os BCs (Atendimento, CatalogoEstoque, Comunicacao, Identidade), seus subfolders (Domain, Features, Infrastructure, Integrations), e projetos de suporte (SharedKernel, Contracts, Host, Api) e testes (Contexts/<BC>.Tests, Architecture.Tests, E2E).
- effort: small
- status: done

## T-006: Consolidar glossário de linguagem ubíqua
- covers: [AC-006]
- files: [.kb/features/design-estrategico-fases-1-2/design.md]
- depends_on: []
- verification: O glossário em `design.md#ddd-glossary` inclui todos os termos de domínio que cruzam fronteiras de contexto (WorkOrder, Budget, ApprovalLink/ExternalAccessToken, InventoryItem, CatalogedService, AdministrativeUser, Customer, Vehicle) com definições formais e aliases proibidos, e está marcado como `ratified: true`.
- effort: small
- status: done

## T-007: Criar README.md do projeto
- covers: [AC-007]
- files: [README.md]
- depends_on: []
- verification: O arquivo `README.md` existe na raiz do repositório e contém seções: objetivos do projeto, stack tecnológica (conforme constitution.md §3), pré-requisitos (.NET 10 SDK, Docker, dotnet-ef tool), instruções passo-a-passo de setup local, e guia de uso (como rodar a aplicação via Aspire AppHost e docker-compose).
- effort: medium
- status: done

## T-008: Criar estrutura do relatório de vulnerabilidades
- covers: [AC-008]
- files: [docs/vulnerability-report.md]
- depends_on: []
- verification: O documento `docs/vulnerability-report.md` existe e contém seções para: metodologia de análise, ferramentas de scan utilizadas (ex: `dotnet list package --vulnerable`, Trivy), achados com classificação de risco (crítico/alto/médio/baixo), e recomendações de remediação.
- effort: small
- status: done

## T-009: Espelhar documentação DDD em docs/
- covers: [AC-001, AC-002, AC-003, AC-004, AC-005, AC-006]
- note: Amended on drift per user request to expose DDD deliverables under `docs/` for Phase 1 submission visibility.
- files: [docs/ddd/README.md, docs/ddd/event-storming.md, docs/ddd/event-storming.excalidraw, docs/ddd/context-map.md, docs/ddd/context-map.excalidraw, docs/ddd/module-structure.md, docs/ddd/module-structure.excalidraw, docs/ddd/glossary.md, README.md]
- depends_on: [T-001, T-002, T-003, T-004, T-005, T-006]
- verification: A pasta `docs/ddd/` existe com índice e espelho dos artefatos DDD principais (Event Storming, Context Map, Module Structure, Glossary), incluindo os arquivos `.excalidraw` editáveis, e o `README.md` raiz referencia explicitamente `docs/ddd/`.
- effort: medium
- status: done
