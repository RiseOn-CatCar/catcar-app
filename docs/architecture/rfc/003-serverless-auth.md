# RFC 003: Serverless Authentication with Azure Function

- **Status:** Accepted
- **Date:** 2026-09-16
- **Decision owners:** CatCar architecture team

## Context

Authentication has bursty traffic and a sharply bounded responsibility: accept credentials through the gateway, validate the administrative identity, and issue an authentication result. It needs an independently deployable release cadence without coupling to the AKS workload image.

## Decision

Deploy the `catcar-auth-function` as an Azure Function behind Azure API Management. The Function uses managed identity to read runtime secrets from Key Vault and delegates administrative identity validation to the Identity Access boundary.

## Rationale

A Function scales from zero-to-burst demand and gives authentication its own deployment artifact and CI workflow. APIM remains the consistent external gateway for policy enforcement, while Key Vault removes signing secrets from packages and GitHub secrets. The separate function also supports the required multi-repository segregation without exposing database credentials to a gateway policy.

## Consequences

- The Function CI builds, tests, publishes, zips, and uploads a deployable artifact independently.
- Cold-start behavior must be measured and its endpoint must be covered by availability monitoring.
- The Function receives least-privilege access: only the Key Vault secrets and Identity Access API/contract it needs.
- Token lifetime, issuer, audience, and key rotation must be configuration-driven and coordinated with APIM and API consumers.

## Alternatives considered

- **Authentication inside the AKS API:** rejected because it couples release cadence and scaling to the main workload.
- **APIM policy-only authentication:** rejected because credential validation and signing-key lifecycle are application concerns, not gateway policy.
- **Third-party hosted identity provider:** deferred; it is viable when federation or customer identity requirements exceed the current administrative scope.
