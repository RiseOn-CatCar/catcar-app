---
id: adr-0001-nsubstitute-over-moq
feature: fundacao-arquitetural-monolito-modular
status: accepted
created: 2026-06-10
updated: 2026-06-26
phase: archived
---

# ADR 0001 — NSubstitute over Moq for unit test mocking

## Status
Accepted

## Context

The CatCar project needs a mocking framework for unit tests — to isolate domain handlers from infrastructure dependencies (repositories, external services). The constitution (§7.4) explicitly left the choice between **NSubstitute** and **Moq** open, to be decided at bootstrap.

Moq (v4.x) was the de facto standard in .NET for over a decade. However, in mid-2023 (v4.18.4), Moq introduced **SponsorLink**, a closed-source telemetry component that:
- Phoned home during builds.
- Embedded obfuscated DLLs that read the local Git email.
- Could not be disabled via configuration.

The community backlash was severe; Moq v4.20+ attempted to mitigate but the trust was broken. NSubstitute, by contrast, has always been fully open-source (BSD license), with a clean, expression-based API that needs no `.Object` unwrapping.

## Decision

We will use **NSubstitute 5.3.0** as the sole mocking framework for all CatCar unit tests.

**Moq is prohibited** — it must not appear in `Directory.Packages.props` or any `.csproj`.

## Alternatives considered

| Option | Pros | Cons | Reason rejected |
|---|---|---|---|
| Moq 4.20+ | Familiar API; huge community; vast documentation | SponsorLink controversy eroded trust; closed-source component in build pipeline; license uncertainty | Trust and supply-chain integrity are non-negotiable for a project that emphasizes security from commit 1 (constitution §6) |
| FakeItEasy | Mature; "natural" API ("A.CallTo") | Smaller community; less familiar to the team; fewer integrations | NSubstitute has broader adoption in DDD/.NET communities |
| **NSubstitute 5.3.0** | Fully open-source (BSD); clean API (`substitute.Received()`); no `.Object` unwrapping; widely used in .NET OSS; active maintenance | Smaller ecosystem than Moq historically (gap narrowing); API differences require team learning | Chosen — see Decision |

## Trade-offs

- **Positive:** Supply-chain integrity — NSubstitute has no closed-source components, no phone-home, no SponsorLink-equivalent.
- **Positive:** Simpler API — no `.Object` property unwrapping; `substitute.Received(1).Method()` reads naturally.
- **Negative:** Team members familiar with Moq need a small learning investment (~30 min to adapt). The APIs are semantically similar: `mock.Verify(x => x.Foo(), Times.Once)` becomes `substitute.Received(1).Foo()`.
- **Negative:** Some advanced Moq features (`Mock.Get()`, `MockBehavior.Strict`) have no direct NSubstitute equivalent. These are rarely needed in Vertical Slice handler tests (which test behavior, not strict invocation order).

## Consequences

- All test projects reference `NSubstitute 5.3.0` via CPM.
- `@developer` must not add `Moq` to any `.csproj`.
- The Architecture.Tests NetArchTest suite should verify that no test project references Moq (optional — enforce at review time).
- If a future use case genuinely requires a Moq-only feature, this ADR must be superseded with justification.

## Constraints emitted

- No new CON-level constraint. Moq prohibition is enforced by this ADR and the CPM pinning.

## Evidence

- Moq SponsorLink controversy: [Discussion #1372](https://github.com/devlooped/moq/issues/1372) (Aug 2023), [SponsorLink v1 announcement](https://www.nuget.org/packages/SponsorLink/).
- NSubstitute license: [BSD-3-Clause](https://github.com/nsubstitute/NSubstitute/blob/main/LICENSE.txt).
- NSubstitute 5.3.0 targets .NET 8+, compatible with .NET 10.
