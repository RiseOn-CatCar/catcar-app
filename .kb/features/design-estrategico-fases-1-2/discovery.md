---
feature: design-estrategico-fases-1-2
kind: discovery
status: ratified
updated: 2026-06-10
relations:
  depends_on: []
  extends: []
  shares_context: []
  related:
    - decomposicao-features-fases-1-2
ratified_by: .kb/features/design-estrategico-fases-1-2/design.md
phase: archived
---

# Design Estratégico — Fases 1 e 2

## Concept
Este artefato transforma os requisitos das fases 1 e 2 em um mapa estratégico inicial do domínio do CatCar. O objetivo é manter o projeto simples o suficiente para uma entrega individual, mas realista o bastante para suportar segurança, rastreabilidade, evolução arquitetural e decomposição em features táticas.

Premissas já alinhadas:
- implementação inicial como monólito modular;
- cliente e veículo permanecem sob o contexto de `Atendimento/OS`;
- preços de serviços e peças são congelados no orçamento/OS;
- o cliente aprova orçamento e consulta andamento por link/token externo;
- múltiplas unidades ficam fora do escopo desta feature estratégica.

## Domain Map

> O Event Storming desta feature foca a fronteira mais carregada do domínio neste momento: emissão/aprovação de orçamento, comunicação externa com o cliente e comprometimento de estoque. A abertura detalhada da OS e o fluxo operacional fino serão refinados nas próximas features táticas.

### Events
- `OrcamentoEmitido` — a OS passou a ter uma proposta formal vigente para o cliente.
- `OrcamentoAprovado` — o cliente autorizou a execução do orçamento vigente.
- `EstoqueReservado` — peças/insumos necessários foram comprometidos para uma OS autorizada.

### Commands
- `EmitirOrcamento` — consolidar serviços e peças e gerar a proposta vigente da OS.
- `RegistrarAprovacaoDeOrcamento` — registrar a decisão do cliente recebida por link/token externo.
- `EnviarLinkDeAprovacao` — disparar comunicação externa para o cliente.
- `ReservarEstoque` — comprometer estoque para a execução autorizada.

### Aggregates
- `OrdemDeServico`
  - **Identity:** `OrdemDeServicoId`
  - **Lifecycle:** recebida → diagnóstico → aguardando aprovação → em execução → finalizada → entregue
  - **Invariants:** a OS pertence a um único cliente e a um único veículo; o orçamento vigente preserva snapshot de preços; a OS não avança para execução sem decisão válida sobre o orçamento.
- `ItemEstoque`
  - **Identity:** `ItemEstoqueId`
  - **Lifecycle:** disponível → reservado → consumido | reposto
  - **Invariants:** estoque não pode ficar negativo; toda reserva precisa ser rastreável por OS.

### Actors
- `Cliente` — acompanha a OS e aprova/recusa o orçamento por link/token externo.

### Policies
- **When** `OrcamentoEmitido` **then** `EnviarLinkDeAprovacao`.
- **When** `OrcamentoAprovado` **then** `ReservarEstoque`.

### External Systems
- `EmailProvider` — canal externo para envio do link de aprovação e futuras notificações.

### Business Rules
- O orçamento precisa congelar preço e descrição relevantes de serviços e peças no momento da emissão.
- Cliente e veículo são dados canônicos do contexto `Atendimento/OS` nesta primeira versão.
- O comprometimento de estoque acontece no momento da aprovação do orçamento; o mecanismo de consistência entre contextos ainda será ratificado.

## Bounded Contexts

### `Atendimento/OS` [Core]
- **Responsibility:** concentrar abertura da OS, dados de cliente/veículo, diagnóstico, orçamento, decisão do cliente e acompanhamento do ciclo completo da OS.
- **Owned aggregates:** `OrdemDeServico`, `Cliente`, `Veiculo`, `Orcamento`.
- **Why this boundary now:** reduz coordenação entre contextos no MVP sem perder rastreabilidade do processo ponta a ponta.
- **Key integration notes:** consome catálogo/estoque por contrato explícito e publica linguagem de eventos para comunicação com o cliente.

### `Catalogo & Estoque` [Supporting]
- **Responsibility:** manter serviços catalogados, peças/insumos, disponibilidade, preço-base, reserva e movimentação de estoque.
- **Owned aggregates:** `ServicoCatalogado`, `ItemEstoque`.
- **Why this boundary now:** estoque e catálogo têm linguagem e invariantes próprias, especialmente para disponibilidade, reserva e consumo.

### `Comunicacao com Cliente` [Supporting]
- **Responsibility:** enviar notificações externas, links/token de aprovação e traduzir interações externas do cliente para comandos do domínio.
- **Owned aggregates:** `Notificacao`, `TokenDeAcessoExterno`.
- **Why this boundary now:** protege o domínio principal de detalhes de canal, expiração de link e eventual troca de provedor externo.

### `Identidade & Acesso` [Generic]
- **Responsibility:** autenticação/autorização administrativa e emissão/validação de JWT para APIs administrativas.
- **Owned aggregates:** `UsuarioAdministrativo`, `PapelDeAcesso`.
- **Why this boundary now:** é um subdomínio genérico e não deve vazar sua linguagem para os contextos de negócio.

## Strategic Relationships

- `Atendimento/OS` → `Catalogo & Estoque`
  - **Relation:** `Customer/Supplier`
  - **Contract style:** `OHS + Published Language`
  - **Protection:** `ACL` em `Atendimento/OS`
  - **Shared language crossing the boundary:** disponibilidade, preço-base, item reservado.
- `Atendimento/OS` → `Comunicacao com Cliente`
  - **Relation:** `Published Language`
  - **Events crossing the boundary:** `OrcamentoEmitido` e sinais futuros de acompanhamento da OS.
- `Comunicacao com Cliente` → `Atendimento/OS`
  - **Relation:** `Customer/Supplier`
  - **Contract style:** `OHS`
  - **Commands crossing the boundary:** `RegistrarAprovacaoDeOrcamento` e futura recusa de orçamento.
- `Atendimento/OS` → `Identidade & Acesso`
  - **Relation:** `ACL`
  - **Reason:** JWT, papéis e identidade administrativa não devem contaminar o modelo de negócio da OS.

## Glossary

### Ordem de Serviço (OS)
- **Definition:** unidade de acompanhamento do trabalho executado para um veículo específico, com histórico de status, orçamento vigente e resultado operacional.
- **Aliases (forbidden):** "chamado", "ticket".

### Orçamento
- **Definition:** proposta comercial vigente de serviços e peças vinculada a uma OS, congelada para auditoria a partir da emissão.
- **Aliases (forbidden):** "cotação dinâmica".

### Link de Aprovação
- **Definition:** meio externo seguro pelo qual o cliente aprova ou recusa o orçamento sem precisar de login administrativo.
- **Aliases (forbidden):** "login do cliente".

### Item de Estoque
- **Definition:** peça ou insumo controlado com disponibilidade, reserva e consumo rastreáveis.
- **Aliases (forbidden):** "produto" quando a intenção for estoque operacional.

## Domain Diagrams
- Event Storming (Mermaid): [event-storming.md](event-storming.md)
- Event Storming: [event-storming.excalidraw](event-storming.excalidraw)
- Context Map: [context-map.excalidraw](context-map.excalidraw)
- Context Map (Mermaid): [context-map.md](context-map.md)

## Open Architectural Questions

### 1. Consistência entre aprovação do orçamento e comprometimento de estoque
- **Resolvida em:** `design.md#resolved-open-questions`
- **Decisão:** **consistência forte no MVP** (chamada síncrona no mesmo handler, mesma transação via outbox) + **publicação do evento `OrcamentoAprovado` via outbox** para observers secundários. Preparada para consistência eventual quando um BC for extraído.

### 2. Fronteira operacional dentro de `Atendimento/OS`
- **Resolvida em:** `design.md#resolved-open-questions`
- **Decisão:** **contexto único por enquanto**, com modularização interna já disciplinada por **Vertical Slices nomeadas por fase operacional** (`Features/AbrirOS/`, `Features/EmitirOrcamento/`, `Features/RegistrarAprovacao/`, `Features/GerenciarExecucao/`).

## Out of Scope for this feature
- modelar múltiplas unidades como capacidade corrente;
- transformar infraestrutura de fase 2 (Kubernetes, Terraform, CI/CD) em bounded contexts de negócio;
- detalhar regras táticas por endpoint ou por caso de uso individual.
