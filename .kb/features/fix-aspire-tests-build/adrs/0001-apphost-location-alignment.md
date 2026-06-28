---
id: adr-0001-apphost-location-alignment
feature: fix-aspire-tests-build
kind: adr
status: accepted
created: 2026-06-26
updated: 2026-06-27
phase: archived
---

# ADR 0001 — AppHost and ServiceDefaults Location Alignment

## Status
Accepted

## Context

The CatCar constitution §4.2 (canonical folder structure) and §10.1 (local dev command) explicitly specify the Aspire AppHost at `src/Host/CatCar.AppHost.csproj`. The archived foundation design (`fundacao-arquitetural-monolito-modular/design.md` §2) reinforces this: source project #8 is `src/Host/CatCar.AppHost.csproj` with `Program.cs`, `appsettings.json`, and `Properties/launchSettings.json` nested under `Host/`.

During implementation, the Aspire AppHost was generated via `aspire new` (template), which placed it at `src/Infrastructure/CatCar.AppHost/CatCar.AppHost.csproj` alongside `CatCar.ServiceDefaults` and `aspire.config.json`. This is a drift from the ratified canonical path. The `CatCar.slnx` solution folder `/Infrastructure/` now groups both Aspire projects.

The constitution §14 states the constitution is non-negotiable without an ADR, and changes to architectural pattern require evidence (spike, benchmark). The burden of proof falls on the drift, not on the canonical path. "The template put it somewhere else" is not sufficient justification to amend a ratified constitution.

The Inverse Conway Law (constitution §2.4) requires the code structure to make it easy to extract a BC into a microservice tomorrow. The AppHost is not a BC — it is orchestration. Placing it in `src/Infrastructure/` conflates two distinct architectural concerns: BC-internal infrastructure (the DDD meaning of "Infrastructure" per design.md §4.1, where each BC has its own `Infrastructure/` folder for EF Core, repos, integrations) and cross-cutting orchestration (the AppHost composition root). A developer extracting a BC should never have to wonder whether `src/Infrastructure/` is shared BC infrastructure or the orchestration host.

## Decision

We will move both the Aspire AppHost and ServiceDefaults from `src/Infrastructure/` to `src/Host/`, aligning with constitution §4.2 and §10.1. The `aspire.config.json` moves alongside them to `src/Host/`.

The target structure is one-folder-per-project under the `src/Host/` root:

```
src/Host/
  aspire.config.json
  CatCar.AppHost/
    CatCar.AppHost.csproj
    AppHost.cs
    appsettings.json
    appsettings.Development.json
    Properties/launchSettings.json
  CatCar.ServiceDefaults/
    CatCar.ServiceDefaults.csproj
    Extensions.cs
```

The `CatCar.slnx` solution folder `/Infrastructure/` is removed; a `/Host/` folder replaces it containing both projects.

We do **not** amend the constitution. This ADR records *why we align to the canonical path* rather than *why we diverge*. The constitution already says `src/Host/`; we honor it.

## Alternatives considered

| Option | Pros | Cons | Reason rejected |
|---|---|---|---|
| **Keep `src/Infrastructure/`** — accept drift, update constitution | No file movement | Requires constitution amendment (§14: ADR + justification + trade-offs). `Infrastructure/` is semantically ambiguous in this project's DDD grammar — it means BC-internal infrastructure, not orchestration. Creates documentation split-brain: the archived design.md says `src/Host/` while the constitution would say `src/Infrastructure/`. Every downstream feature inherits the drift. | The burden of proof is on the drift, not the canonical path. "Template convenience" is not sufficient justification to amend a ratified constitution. Semantic ambiguity violates the spirit of §2.4. |
| **Split: AppHost to `src/Host/`, ServiceDefaults stays in `src/Infrastructure/`** | AppHost (composition root) is distinct from ServiceDefaults (shared infra consumed by API) | ServiceDefaults is Aspire-specific and conceptually paired with the AppHost (the Aspire template generates them together). Splitting them across two folders creates an artificial separation and two locations to reason about for the Aspire setup. `aspire.config.json` location becomes ambiguous. | The pairing is stronger than the semantic distinction. Both are Aspire orchestration concerns; keeping them together in `Host/` preserves a single, self-contained Aspire setup. |
| **Chosen: Move both to `src/Host/`** | Constitution compliance (§4.2, §10.1). Semantic clarity — `Host/` = composition root + orchestration, distinct from BC infrastructure. ServiceDefaults stays paired with AppHost. `aspire.config.json` sits with the AppHost it configures. Prevents drift from solidifying into a de-facto standard. | Requires moving files, updating slnx, updating aspire.config.json path. | (chosen — see Decision) |

## Trade-offs

- **Positive:** Constitution §10.1 devX command `dotnet run --project src/Host/CatCar.AppHost.csproj` works as documented — no documentation lies.
- **Positive:** Semantic clarity: `src/Host/` is unambiguously the orchestration composition root; `src/Contexts/<BC>/Infrastructure/` remains unambiguously BC-internal infrastructure. No conflation.
- **Positive:** Single self-contained Aspire setup under one root (`src/Host/`), including `aspire.config.json`.
- **Negative:** File movement cost — git history for the AppHost and ServiceDefaults files will show as rename/move. This is a one-time cost absorbed by the fix feature, which is already editing these files.
- **Negative:** The design.md §2 folder sketch shows a flat `Host/CatCar.AppHost.csproj` (csproj directly under `Host/`), but the practical realization nests one folder per project (`Host/CatCar.AppHost/CatCar.AppHost.csproj`). This is a minor deviation from the sketch, justified by the one-folder-per-project .NET convention and the addition of ServiceDefaults (not in the original sketch). The `src/Host/` *root* is honored; the internal nesting is a practical refinement.

## Consequences

- The fix feature's tasks T-002, T-003, T-004, T-007 must reference `src/Host/CatCar.AppHost/...` and `src/Host/CatCar.ServiceDefaults/...` paths instead of `src/Infrastructure/...`.
- `CatCar.slnx` gains a `/Host/` solution folder and loses the `/Infrastructure/` folder.
- `aspire.config.json` moves to `src/Host/aspire.config.json` with the AppHost path updated to `CatCar.AppHost/CatCar.AppHost.csproj` (relative to `src/Host/`).
- The constitution §10.1 command works as documented without amendment.
- Downstream features (02–08) inherit the canonical structure; no future drift.
- CON-016 (see `constraints.md`) makes the AppHost location enforceable, preventing regression.

## Constraints emitted

- See `constraints.md#CON-016`

## Evidence

- Constitution §4.2 canonical folder structure: `src/Host/` labeled "Aspire AppHost + Composition Root".
- Constitution §10.1: `dotnet run --project src/Host/CatCar.AppHost.csproj`.
- `fundacao-arquitetural-monolito-modular/design.md` §2 source projects table, row #8: `src/Host/CatCar.AppHost.csproj`.
- Current drifted state: `src/Infrastructure/CatCar.AppHost/CatCar.AppHost.csproj` (verified via glob + read).
- `CatCar.slnx` lines 14–17: `/Infrastructure/` solution folder groups both Aspire projects.