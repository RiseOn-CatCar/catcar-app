---
id: adr-0003-adopt-riseon-packages
feature: fundacao-arquitetural-monolito-modular
status: accepted
created: 2026-06-10
updated: 2026-06-26
phase: archived
---

# ADR 0003 — Adopt RiseOn.ResultRail and RiseOn.AutoInject packages

## Status

Accepted

## Context

The CatCar project is bootstrapping from an empty repository. The tactical design (design.md §3) originally specified a custom `Result<T>` implementation in the SharedKernel for the Result pattern, and manual DI registration via `ServiceCollectionExtensions.Add<BC>()` in each Bounded Context (design.md §8.1).

The project author is also the maintainer of two NuGet packages:

1. **RiseOn.ResultRail** (v1.1.1, MIT, https://www.nuget.org/packages/RiseOn.ResultRail)
   - Implements Result/Railway pattern with type called "Upshot"
   - Provides `Upshot` (non-generic) and `Upshot<T>` (generic) as readonly structs
   - Has `Error` class with implicit conversions from string/Exception
   - Extension methods for chaining: OnRail, OnRailSuccess, OnRailFail, Map, Finally
   - Targets: net9.0, net8.0, netstandard2.0

2. **RiseOn.AutoInject** (v1.0.1-beta, MIT, https://www.nuget.org/packages/RiseOn.AutoInject/1.0.1-beta)
   - Automatic DI registration via source generator + attributes
   - Attribute: `[InjectService(ServiceLifetimeType, injectAlone, CollectionName, ImplementationOf, Key)]`
   - Source generator produces IServiceCollection extension methods
   - Targets: netstandard2.0 (compatible with .NET 5+)

The initial architectural review (@architector) recommended against adoption on the grounds that:
- Constitution §4.1 explicitly names `Result<T>` as a SharedKernel element
- Adding external dependencies to SharedKernel violates CON-003 (purity)
- AutoInject conflicts with explicit composition root pattern (constitution §4.2)
- AutoInject conflicts with Vertical Slice explicit registration

However, the project author confirmed that they are the maintainer of both packages and wants to adopt them as a strategic ecosystem decision (dogfooding), not merely as a third-party tool evaluation.

## Decision

We will adopt both packages:

1. **RiseOn.ResultRail** replaces the custom `Result<T>` in SharedKernel. The type `Upshot<T>` becomes the standard Result pattern implementation across all Bounded Contexts.

2. **RiseOn.AutoInject** supplements (not replaces) the explicit `ServiceCollectionExtensions.Add<BC>()` pattern. Handlers, validators, and infrastructure services within each BC use `[InjectService]` attributes for automatic registration, while the BC-level module registration remains explicit in the composition root.

## Alternatives considered

| Option | Pros | Cons | Reason rejected/accepted |
|---|---|---|---|
| Custom Result<T> (original design) | Zero external dependencies, full control, matches constitution literally | Requires maintaining boilerplate, no railway extensions out-of-the-box | Rejected — author wants to dogfood RiseOn.ResultRail |
| RiseOn.ResultRail | Ready-made railway extensions, struct-based (low allocation), MIT, published on nuget.org | External dependency in SharedKernel, naming mismatch ("Upshot" vs "Result"), requires AD-R to deviate from constitution | **Accepted** — strategic ecosystem decision |
| Manual DI registration (original design) | Explicit, debuggable, matches constitution literally | More boilerplate (~20-30 lines per BC) | Partially rejected — author wants to dogfood RiseOn.AutoInject |
| RiseOn.AutoInject | Less boilerplate, attribute-based, source generator (compile-time) | Implicit behavior, harder to trace registrations, requires AD-R | **Accepted** — supplements explicit BC registration, not replaces it |

## Trade-offs

### RiseOn.ResultRail

**Positive:**
- Dogfooding strategy — the project author maintains the package and wants real-world validation
- Railway extensions (OnRail, Map, Finally) reduce boilerplate in handler chaining
- Struct-based implementation has lower allocation overhead (though negligible for CRUD-heavy system)
- MIT license, published on nuget.org (availability resolved)

**Negative:**
- Adds external dependency to SharedKernel (CON-003 purity concern) — mitigated by the fact that the dependency is maintained by the project author
- Naming mismatch: "Upshot" vs "Result" — requires updating all design docs, specs, and tasks to use "Upshot" terminology
- Constitution §4.1 explicitly names `Result<T>` — this ADR supersedes that clause for the purposes of this project
- No async helpers in RiseOn.ResultRail — if needed, must be added locally

### RiseOn.AutoInject

**Positive:**
- Dogfooding strategy — same rationale as ResultRail
- Reduces boilerplate for handler/validator/infrastructure registration within each BC
- Source generator approach is compile-time (no runtime reflection overhead)
- MIT license, published on nuget.org

**Negative:**
- Implicit registration via attributes makes DI graph less visible — mitigated by keeping explicit BC-level registration in composition root
- Conflicts with Vertical Slice explicit registration philosophy — mitigated by using AutoInject only for intra-BC services, not cross-BC or BC-level module registration
- Beta version (1.0.1-beta) — risk of breaking changes, but acceptable for a project author dogfooding their own library

## Consequences

### SharedKernel changes
- Remove custom `Result.cs` and `Result<T>` implementation from `src/SharedKernel/`
- Add `RiseOn.ResultRail` package reference to `CatCar.SharedKernel.csproj`
- Replace all `Result<T>` usage with `Upshot<T>` throughout the codebase
- Update design.md §3, spec.md AC-007, and tasks.md T-002 to reflect this change
- Constitution §4.1 is superseded for this project — `Upshot<T>` replaces `Result<T>` as the SharedKernel result type

### DI registration changes
- Add `RiseOn.AutoInject` package reference to all BC `.csproj` files
- Handlers, validators, and infrastructure services use `[InjectService(ServiceLifetimeType.Scoped)]` (or appropriate lifetime)
- Each BC's `ServiceCollectionExtensions.Add<BC>()` calls the generated `Use{BC}Services()` extension method from AutoInject
- Composition root in `Program.cs` remains explicit — it calls `AddAtendimento()`, `AddCatalogoEstoque()`, etc.
- Update design.md §8.1, spec.md (add AC for AutoInject), and tasks.md T-004/T-006

### Documentation updates
- design.md §3 (SharedKernel): Replace `Result<T>` with `Upshot<T>` from RiseOn.ResultRail
- design.md §8.1 (API composition root): Mention RiseOn.AutoInject for intra-BC service registration
- spec.md AC-007: Update to reflect RiseOn.ResultRail adoption
- spec.md: Add new AC for RiseOn.AutoInject adoption
- tasks.md T-001: Add RiseOn.ResultRail and RiseOn.AutoInject to Directory.Packages.props
- tasks.md T-002: Remove custom Result<T> implementation, use RiseOn.ResultRail
- tasks.md T-004/T-006: Use RiseOn.AutoInject for service registration

### Constraints emitted
- No new CON-level constraint. Package adoption is enforced by this ADR and CPM pinning.
- CON-003 (SharedKernel purity) is superseded for the RiseOn.ResultRail dependency — the dependency is acceptable because the package is maintained by the project author and published on nuget.org.

## Evidence

- RiseOn.ResultRail on nuget.org: https://www.nuget.org/packages/RiseOn.ResultRail
- RiseOn.AutoInject on nuget.org: https://www.nuget.org/packages/RiseOn.AutoInject/1.0.1-beta
- RiseOn.ResultRail GitHub: https://github.com/daviholandas/RiseOn.ResultRail
- RiseOn.AutoInject GitHub: https://github.com/daviholandas/RiseOn.AutoInject
- Both packages are MIT licensed and maintained by the project author (daviholandas)
