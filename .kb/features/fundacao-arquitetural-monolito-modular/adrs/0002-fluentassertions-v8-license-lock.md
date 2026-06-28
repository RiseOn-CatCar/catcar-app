---
id: adr-0002-fluentassertions-v8-license-lock
feature: fundacao-arquitetural-monolito-modular
status: accepted
created: 2026-06-10
updated: 2026-06-26
phase: archived
---

# ADR 0002 — FluentAssertions v8 license lock (Apache-2.0)

## Status
Accepted

## Context

FluentAssertions has been the standard assertion library in the .NET ecosystem for over a decade. In January 2025, the maintainer (Dennis Doomen) [announced](https://x.com/ddoomen/status/1879722440629018656) that **v9.0+ would move from Apache-2.0 to a commercial license** (Xceed EULA), requiring a paid license for commercial use.

The CatCar project is an academic/portfolio project (FIAP SOAT Architecture post-grad), but the constitution (§7.4) explicitly flagged this risk (R-005) and required a decision at bootstrap. Even for non-commercial projects, pinning to an Apache-2.0 version avoids licensing ambiguity and keeps the door open for any future use.

v8.x remains **Apache-2.0** and is fully functional. However, v8 will not receive new features or .NET 10-specific improvements. The community has responded with forks, most notably **AwesomeAssertions** ([GitHub](https://github.com/AwesomeAssertions/AwesomeAssertions)), a community-maintained continuation of FluentAssertions from the last Apache-2.0 codebase (v7.x), adding .NET 9+ support.

## Decision

We will use **FluentAssertions 8.2.0** (last Apache-2.0 v8 release) for Phase 1 and Phase 2. This version:

- Has the full v8 feature set (improved `BeEquivalentTo`, `SatisfyRespectively`, etc.).
- Is compatible with .NET 10 (v8 targets netstandard2.0 + net6.0 — both run on .NET 10 without issues).
- Has no commercial license obligations.

If v8.2.0 proves incompatible with a future .NET 10.0.x patch, we will evaluate **AwesomeAssertions** as the migration target. This is not expected, as net6.0-targeting libraries are forward-compatible with .NET 10.

## Alternatives considered

| Option | Pros | Cons | Reason rejected |
|---|---|---|---|
| FluentAssertions v9+ | Latest features; .NET 10 native support | **Commercial license (Xceed EULA)** — requires paid license; ambiguous for academic/portfolio use; adds licensing friction to an otherwise fully OSS stack | Licensing is a non-negotiable concern (constitution §2 principles: "Seguranca desde o primeiro commit") |
| AwesomeAssertions (community fork) | Fully Apache-2.0; .NET 9+ support; active community | Younger project; API may diverge from FluentAssertions; smaller ecosystem; doesn't yet have all v8 features | Promising but unproven — evaluate in Phase 2 if v8 becomes a blocker. Not urgent — v8.2.0 works on .NET 10 today |
| Shouldly | Apache-2.0; mature; different assertion style (`ShouldBe()`) | API style is less fluent than FluentAssertions; smaller community; team familiarity is with FluentAssertions | Different paradigm — migration cost not justified |
| **FluentAssertions 8.2.0** | Apache-2.0; full v8 feature set; runs on .NET 10 (net6.0 target); zero license friction; team already knows the API | No new v8 releases; security patches won't be backported; if .NET 11 drops net6.0 TFM support, we'll need to migrate | Chosen — see Decision |

## Trade-offs

- **Positive:** Zero licensing ambiguity. Full Apache-2.0 stack from test runner (xUnit) through assertions to mocking (NSubstitute).
- **Positive:** Team familiarity. No retraining needed on Shouldly/AwesomeAssertions syntax.
- **Negative:** v8.2.0 is in maintenance-only mode. No bug fixes unless the community backports them (unlikely given the fork).
- **Negative:** If a .NET 10.x runtime change breaks net6.0 binary compatibility, we must migrate to AwesomeAssertions. The migration surface is small (~1 file per test project for `using` statements).

## Consequences

- All test projects reference `FluentAssertions 8.2.0` via CPM.
- `@developer` must not add `FluentAssertions` v9+ to any `.csproj`.
- If a migration to AwesomeAssertions becomes necessary, this ADR must be superseded by a new ADR documenting the new version and migration steps.
- Dependabot must be configured to **ignore** FluentAssertions major version bumps (stay on v8.x).

## Constraints emitted

- No new CON-level constraint. Version lock is enforced by CPM pinning + Dependabot config.

## Evidence

- FluentAssertions v9 license change announcement: [Twitter/X](https://x.com/ddoomen/status/1879722440629018656) (Jan 2025).
- FluentAssertions v8.2.0 NuGet: [NuGet.org](https://www.nuget.org/packages/FluentAssertions/8.2.0) — license shows `Apache-2.0`.
- AwesomeAssertions fork: [GitHub](https://github.com/AwesomeAssertions/AwesomeAssertions) — Apache-2.0, targets .NET 9+.
- .NET forward compatibility: net6.0 libraries run on .NET 10 ([compatibility docs](https://learn.microsoft.com/dotnet/core/compatibility/)).
