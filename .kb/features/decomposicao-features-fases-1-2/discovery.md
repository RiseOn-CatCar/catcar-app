---
feature: decomposicao-features-fases-1-2
kind: discovery
status: draft
updated: 2026-06-07
relations:
  depends_on:
    - design-estrategico-fases-1-2
  extends: []
  shares_context: []
  related: []
---

# Decomposição de Features — Fases 1 e 2

## Concept
Este artefato organiza os requisitos das fases 1 e 2 em um roadmap de features executáveis, com granularidade média e corte principal por capacidades de negócio + bounded contexts. A intenção é permitir evolução incremental, sem perder os requisitos não funcionais que precisam entrar desde o início.

## Princípios de Decomposição
- manter o projeto como **monólito modular**;
- quebrar por **capacidade de negócio** e não apenas por endpoint;
- preservar o mapa estratégico já definido em `design-estrategico-fases-1-2`;
- tratar requisitos não funcionais como **critérios de construção**, não apenas como validação final;
- manter uma única feature dedicada de plataforma para os entregáveis fortes da fase 2.

## Roadmap Proposto

| Ordem | Feature | Foco principal | Cobertura dominante |
|---|---|---|---|
| 00 | `design-estrategico-fases-1-2` | mapa estratégico, bounded contexts, relações, event storming | F1 + F2 |
| 01 | `fundacao-arquitetural-monolito-modular` | estrutura base da aplicação e contratos técnicos iniciais | F2 antecipada |
| 02 | `identidade-acesso-administrativo` | autenticação/autorização administrativa | F1 |
| 03 | `catalogo-servicos-pecas-estoque` | catálogo de serviços, peças, insumos e estoque | F1 |
| 04 | `atendimento-inicial-e-abertura-os` | cliente, veículo, abertura da OS e orçamento inicial | F1 |
| 05 | `aprovacao-orcamento-e-comunicacao-cliente` | aprovação externa do cliente e canais de comunicação | F1 + F2 |
| 06 | `acompanhamento-execucao-e-priorizacao-os` | ciclo operacional, consulta, listagem e priorização da OS | F1 + F2 |
| 07 | `qualidade-seguranca-e-evidencias` | testes, cobertura, validações, scan e documentação de entrega | F1 + F2 |
| 08 | `plataforma-entrega-e-escalabilidade` | Docker, K8s, Terraform, CI/CD, HPA e deploy | F2 |

## Features Planejadas

### 00. `design-estrategico-fases-1-2`
- **Status:** já criada.
- **Aborda:** design estratégico, context map inicial, event storming e principais fronteiras do domínio.
- **Saída esperada:** base para todas as próximas features táticas.

### 01. `fundacao-arquitetural-monolito-modular`
- **Aborda:** estrutura inicial do monólito modular, separação por módulos/camadas, contratos REST, documentação OpenAPI/Swagger, escolha e justificativa do banco, convenções de persistência e bootstrap local.
- **Contextos impactados:** todos, com ênfase em fronteiras internas.
- **Requisitos não funcionais mais fortes:** manutenibilidade, simplicidade estrutural, clareza arquitetural, execução local simples.
- **Observação:** como as fases 1 e 2 serão entregues juntas, esta feature antecipa a base de Clean Architecture/Hexagonal em vez de deixar uma refatoração pesada para depois.

### 02. `identidade-acesso-administrativo`
- **Aborda:** autenticação JWT para APIs administrativas, papéis/perfis básicos, proteção de endpoints, isolamento entre identidade e domínio, estratégia de segredos/configurações sensíveis.
- **Contextos impactados:** `Identidade & Acesso`, com `ACL` para os demais.
- **Requisitos não funcionais mais fortes:** segurança, controle de acesso, proteção de credenciais.

### 03. `catalogo-servicos-pecas-estoque`
- **Aborda:** CRUD de serviços, CRUD de peças e insumos, preço-base, disponibilidade, movimentação de estoque, reserva/consumo como capacidades do domínio de estoque.
- **Contextos impactados:** `Catalogo & Estoque`.
- **Requisitos não funcionais mais fortes:** integridade de estoque, auditabilidade e rastreabilidade operacional.
- **Observação:** a política exata de consistência com `Atendimento/OS` ainda depende do fechamento da questão arquitetural aberta.

### 04. `atendimento-inicial-e-abertura-os`
- **Aborda:** CRUD de clientes, CRUD de veículos, validações de CPF/CNPJ e placa, identificação do cliente, cadastro/seleção do veículo, abertura da OS, inclusão de serviços/peças e geração automática do orçamento inicial com snapshot de preços.
- **Contextos impactados:** `Atendimento/OS`, consumindo catálogo/estoque por contrato.
- **Requisitos não funcionais mais fortes:** validação de dados sensíveis, consistência de entrada, rastreabilidade de orçamento.

### 05. `aprovacao-orcamento-e-comunicacao-cliente`
- **Aborda:** envio do orçamento ao cliente, link/token externo seguro, expiração/validação desse acesso, endpoint para aprovação ou recusa, integração por e-mail e adaptação do canal externo para comandos do domínio.
- **Contextos impactados:** `Comunicacao com Cliente` + `Atendimento/OS`.
- **Requisitos não funcionais mais fortes:** segurança do acesso externo, idempotência de aprovação, resiliência de integração com canal externo.
- **Observação:** a atualização de status por ferramenta como e-mail entra aqui como adaptação/canal; a regra de negócio do status continua no domínio da OS.

### 06. `acompanhamento-execucao-e-priorizacao-os`
- **Aborda:** ciclo de status da OS, alterações automáticas conforme ações do sistema, consulta de status pelo cliente, listagem e detalhamento de ordens de serviço, ordenação por prioridade de negócio, ocultação lógica de OS finalizadas/entregues e monitoramento do tempo médio de execução.
- **Contextos impactados:** `Atendimento/OS`, com dependência relevante de `Catalogo & Estoque`.
- **Requisitos não funcionais mais fortes:** visibilidade operacional, previsibilidade de fluxo, consistência do ciclo de vida.
- **Observação:** esta feature pode ser desdobrada no futuro se a divisão entre `Recepcao & Orcamento` e `Oficina & Execucao` for ratificada.

### 07. `qualidade-seguranca-e-evidencias`
- **Aborda:** testes unitários e de integração dos fluxos críticos, cobertura mínima nos domínios críticos, consolidação das validações sensíveis, análise/scan de vulnerabilidades, README final, collection/link das APIs e preparação dos entregáveis documentais.
- **Contextos impactados:** transversal.
- **Requisitos não funcionais mais fortes:** qualidade, segurança, conformidade com os entregáveis, legibilidade e confiança na solução.
- **Observação:** embora exista como feature explícita, seus critérios devem começar a ser atendidos desde as primeiras implementações.

### 08. `plataforma-entrega-e-escalabilidade`
- **Aborda:** Dockerfile, docker-compose, manifestos Kubernetes, ConfigMaps/Secrets, HPA, scripts Terraform para cluster e banco, pipeline CI/CD, build/test/deploy automatizados, deploy da aplicação e do banco e documentação do fluxo de entrega.
- **Contextos impactados:** plataforma e entrega; não é bounded context de negócio.
- **Requisitos não funcionais mais fortes:** escalabilidade, automação, repetibilidade de deploy, resiliência e operação.
- **Observação:** esta é a feature que concentra os entregáveis mais claros da fase 2, mas depende da base funcional mínima estar estável.

## Trilhas Transversais Obrigatórias

### Segurança
- JWT administrativo entra na feature 02.
- token externo de cliente entra na feature 05.
- segredos, ConfigMaps/Secrets e pipeline segura entram na feature 08.

### Qualidade
- testes precisam nascer com as features 03 a 06;
- a feature 07 consolida cobertura, evidências e lacunas remanescentes.

### Documentação
- OpenAPI começa na feature 01;
- README, collection, links e evidências finais fecham na feature 07;
- documentação operacional de deploy fecha na feature 08.

## Dependências e Sequência Recomendada
1. `design-estrategico-fases-1-2`
2. `fundacao-arquitetural-monolito-modular`
3. `identidade-acesso-administrativo`
4. `catalogo-servicos-pecas-estoque`
5. `atendimento-inicial-e-abertura-os`
6. `aprovacao-orcamento-e-comunicacao-cliente`
7. `acompanhamento-execucao-e-priorizacao-os`
8. `qualidade-seguranca-e-evidencias`
9. `plataforma-entrega-e-escalabilidade`

## Open Questions
- a feature 06 poderá ser dividida depois, caso `@architector` confirme a separação operacional entre recepção/orçamento e oficina/execução;
- a política definitiva entre aprovação do orçamento e comprometimento de estoque pode mover parte da complexidade entre as features 03, 05 e 06;
- ainda será útil consolidar uma `.kb/constitution.md` leve para registrar diretrizes estáveis do projeto.

## Out of Scope
- decomposição das fases 3 e 4;
- detalhamento tático interno de cada feature em tarefas de implementação;
- decisões arquiteturais profundas ainda pendentes no design estratégico.
