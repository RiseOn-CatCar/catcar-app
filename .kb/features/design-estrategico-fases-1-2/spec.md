---
feature: design-estrategico-fases-1-2
kind: spec
status: accepted
created: 2026-06-09
updated: 2026-06-10
phase: archived
---

# Documentação Técnica — Fase 1 CatCar

## Context

O Tech Challenge da Fase 1 exige um conjunto de entregáveis de documentação DDD e técnica que comprovem o domínio estratégico e a capacidade de execução do projeto. O design estratégico (discovery + design + diagramas iniciais) já está ratificado em `discovery.md` e `design.md`; esta feature cobre a documentação restante necessária para completar os entregáveis técnicos da Fase 1 que vivem no repositório.

O Event Storming inicial focou o fluxo crítico de aprovação de orçamento e reserva de estoque. O enunciado exige Event Storming **completo** dos fluxos de criação/acompanhamento da OS e de gestão de peças e insumos — lacunas que precisam ser preenchidas. Os diagramas de Context Map e Module Structure já existem e precisam de revisão final. O README.md e o relatório de vulnerabilidades ainda não foram produzidos.

## Acceptance Criteria

- **AC-001** (ubiquitous): The DDD documentation shall include a complete Event Storming of the Work Order creation flow, covering customer identification by CPF/CNPJ, vehicle registration (plate, make, model, year), service item inclusion, parts and supplies inclusion, automatic budget generation based on services and parts, and budget submission to the customer for approval.

- **AC-002** (ubiquitous): The DDD documentation shall include a complete Event Storming of the Work Order tracking flow, covering all six status transitions (Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue), the automatic status changes triggered by system actions, and the customer query capability for progress tracking.

- **AC-003** (ubiquitous): The DDD documentation shall include a complete Event Storming of the parts and supplies management flow, covering CRUD operations for cataloged services, CRUD operations for parts and supplies, and stock control operations including availability check, reservation, consumption, and restocking.

- **AC-004** (ubiquitous): The DDD documentation shall include a Context Map diagram showing all four bounded contexts (Atendimento/OS, Catalogo & Estoque, Comunicacao com Cliente, Identidade & Acesso), their strategic types (Core, Supporting, Generic), the Shared Kernel, and all integration relationships with translation strategies (OHS, ACL, Published Language, Customer-Supplier).

- **AC-005** (ubiquitous): The DDD documentation shall include a Module Structure diagram showing the .NET solution organization with Vertical Slices per bounded context, including all supporting projects (SharedKernel, Contracts, Host, Api) and test projects.

- **AC-006** (ubiquitous): The DDD documentation shall apply ubiquitous language consistently, with a ratified glossary of domain terms including formal definitions and forbidden aliases for every term that crosses a bounded context boundary.

- **AC-007** (ubiquitous): The project shall include a README.md at the repository root with project objectives, technology stack overview, prerequisites, step-by-step setup instructions, and usage guide covering how to run the application locally.

- **AC-008** (ubiquitous): The project shall include a vulnerability report document containing the scan methodology, tools used, findings with risk classification, and remediation recommendations for the source code.

## Out of scope

- Vídeo demonstrativo de até 15 minutos (artefato de submissão, não documentação técnica do repositório).
- Documento de entrega em PDF (artefato de submissão com dados do grupo e links).
- Configuração de Swagger/OpenAPI no código (requisito técnico de implementação, coberto por features de código).
- Implementação de APIs, Docker, docker-compose, ou testes automatizados (cobertos por features de implementação).
- Análise de vulnerabilidades executada contra o código (depende do código existir; esta feature cobre apenas a estrutura do relatório).

## Open questions

- **Q-01** (2026-06-09): O relatório de vulnerabilidades deve seguir um formato específico (ex: OWASP ASVS, template personalizado) ou um formato livre com seções de metodologia, achados e recomendações?
