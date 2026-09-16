# Authentication and Work Order Opening Sequences

## Administrative authentication

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Administrative user
    participant APIM as API Management
    participant Auth as Azure Function
    participant KV as Key Vault
    participant IAM as Identity Access

    Admin->>APIM: POST /auth/login (credentials)
    APIM->>Auth: Forward authenticated gateway request
    Auth->>KV: Read signing/configuration secret via managed identity
    KV-->>Auth: Secret reference value
    Auth->>IAM: Validate administrative user and password hash
    IAM-->>Auth: Identity and role or validation failure
    Auth-->>APIM: Signed token or RFC 7807 failure
    APIM-->>Admin: Token or failure response
```

The gateway is the public policy boundary. The Function owns the serverless authentication deployment boundary; secret retrieval is identity-based and no password or signing key is placed in source control. Identity Access remains the authoritative owner of administrative user credentials and roles.

## Work order opening

```mermaid
sequenceDiagram
    autonumber
    actor Attendant
    participant APIM as API Management
    participant OS as Service Operations
    participant DB as PostgreSQL service_operations
    participant Outbox as Transactional outbox
    participant Bus as Integration transport
    participant Catalog as Catalog Inventory
    participant Comm as Communication

    Attendant->>APIM: POST /work-orders (Bearer token, customer, vehicle, description)
    APIM->>OS: Route authorized request
    OS->>DB: Validate references and persist WorkOrder
    OS->>Outbox: Persist WorkOrderOpened event in same transaction
    DB-->>OS: Commit
    OS-->>APIM: 201 Created (work order id and status)
    APIM-->>Attendant: 201 Created
    Outbox->>Bus: Publish WorkOrderOpened asynchronously
    Bus->>Catalog: Reserve/check inventory as needed
    Bus->>Comm: Trigger customer communication as needed
```

Opening an OS is synchronous only through the Service Operations transaction. The API returns after the work order and its outbox message commit atomically. Downstream bounded contexts consume the integration event asynchronously, apply idempotent handling, and do not read or write the `service_operations` schema directly. A failed downstream action is retried or compensated by its own consumer rather than rolling back the committed OS.
