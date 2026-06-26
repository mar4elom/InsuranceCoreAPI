# InsuranceCoreAPI

A REST API for a simplified insurance domain: **Customers → Policies → Claims**.

Built with **.NET 10 Web API** (Controllers), in-memory storage, Swagger/OpenAPI, and xUnit tests.

---

## How to Run the API

```bash
cd src/InsuranceCoreAPI
dotnet run
```

The API starts on:
- HTTP: `http://localhost:5247`
- HTTPS: `https://localhost:7222`

**Swagger UI** is available at `http://localhost:5247/swagger` while running in Development.

**OpenAPI JSON** is available at `http://localhost:5247/openapi/v1.json`.

---

## How to Run Tests

### All tests

```bash
# Unit tests
dotnet test tests/InsuranceCoreAPI.UnitTests

# Integration tests
dotnet test tests/InsuranceCoreAPI.IntegrationTests
```

### From the solution root

```bash
dotnet test InsuranceCoreAPI.slnx
```

> **Note:** Due to a `.slnx` folder-naming quirk, running `dotnet test` on the solution file may only discover one test project at a time. Run each project directly (as shown above) for a reliable combined result.

### Expected results

| Project | Tests | All pass |
|---|---|---|
| `InsuranceCoreAPI.UnitTests` | 31 | ✓ |
| `InsuranceCoreAPI.IntegrationTests` | 18 | ✓ |

---

## Project Structure

```
InsuranceCoreAPI/
├── src/
│   └── InsuranceCoreAPI/
│       ├── Controllers/          # Thin controllers — delegate all logic to services
│       │   ├── CustomersController.cs
│       │   ├── PoliciesController.cs
│       │   └── ClaimsController.cs
│       ├── Domain/               # Entity classes and enums
│       │   ├── Customer.cs
│       │   ├── Policy.cs         # Includes OverlapsWith() domain logic
│       │   ├── Claim.cs
│       │   └── Enums/
│       ├── DTOs/                 # Request/response records with DataAnnotations
│       ├── Errors/               # AppException hierarchy + GlobalExceptionHandler
│       ├── Repositories/         # ConcurrentDictionary-backed in-memory stores
│       ├── Services/             # Business logic layer (PolicyService, ClaimService, ...)
│       └── Program.cs
└── tests/
    ├── InsuranceCoreAPI.UnitTests/
    │   ├── PolicyServiceTests.cs   # Service-layer unit tests using NSubstitute mocks
    │   ├── ClaimServiceTests.cs
    │   └── PolicyDomainTests.cs    # Pure domain model tests (Policy.OverlapsWith)
    └── InsuranceCoreAPI.IntegrationTests/
        ├── ApiFactory.cs                        # WebApplicationFactory + HTTP helpers
        ├── PolicyActivationIntegrationTests.cs  # Full HTTP-stack policy tests
        └── ClaimIntegrationTests.cs             # Full HTTP-stack claim tests
```

---

## API Endpoints

### Customers
| Method | Path | Description |
|---|---|---|
| `POST` | `/customers` | Create a new customer |
| `GET` | `/customers/{id}` | Get customer by ID |

### Policies
| Method | Path | Description |
|---|---|---|
| `POST` | `/policies` | Create a new policy (status: **Draft**) |
| `POST` | `/policies/{id}/activate` | Transition policy from Draft → **Active** |
| `GET` | `/policies/{id}` | Get policy by ID |

### Claims
| Method | Path | Description |
|---|---|---|
| `POST` | `/claims` | Create a new claim (status: **New**) |
| `POST` | `/claims/{id}/decide` | Approve or Reject a claim |
| `GET` | `/claims/{id}` | Get claim by ID |

---

## Business Rules

### Policy Rules
- `EndDate` must be strictly after `StartDate` → **400** otherwise
- A policy can only be activated from **Draft** status → **409** otherwise
- A customer may not have two **Active** policies of the same `ProductType` with overlapping date ranges → **409** on activation

  Overlap is defined using inclusive bounds: two ranges `[A.Start, A.End]` and `[B.Start, B.End]` overlap when `A.Start ≤ B.End && B.Start ≤ A.End`. This means two ranges that share exactly one boundary day **are** considered overlapping.

### Claim Rules
- A claim can only be filed against an **Active** policy → **409** otherwise
- `IncidentDate` must fall within the policy period `[StartDate, EndDate]` inclusive → **409** otherwise
- A decision (Approve/Reject) can only be made on a **New** claim → **409** otherwise
- Rejecting a claim requires a non-empty `DecisionReason` → **400** otherwise

---

## Error Contract

All errors return [RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807) JSON:

```json
{
  "status": 409,
  "title": "Conflict",
  "detail": "Policy '...' overlaps with existing active policy '...'.",
  "instance": "/policies/00000000-0000-0000-0000-000000000000/activate"
}
```

| Status | Meaning |
|---|---|
| **400** | Invalid input or missing required field (e.g. `EndDate ≤ StartDate`, missing `DecisionReason` on rejection) |
| **404** | Resource not found |
| **409** | Business rule conflict (wrong state, date overlap, incident outside policy period) |

---

## Design Notes & Tradeoffs

### Date type: `DateOnly`

`DateOnly` is used for all date fields (`StartDate`, `EndDate`, `IncidentDate`). Insurance policy validity and incident reporting are calendar-day concepts — time-of-day and timezone are irrelevant. `DateOnly` avoids timezone ambiguity when comparing dates across entities and keeps the domain model honest.

### In-memory storage

Repositories use `ConcurrentDictionary<Guid, T>` and are registered as **singletons**, so state persists for the lifetime of the process. This satisfies the no-database constraint while remaining thread-safe under concurrent requests. In integration tests each test class creates its own `WebApplicationFactory`, giving it a fresh isolated store.

### Service layer

All business logic lives in `PolicyService` and `ClaimService`, not in the controllers. Controllers are intentionally thin: they parse the request, call the service, and map the result to an HTTP response. This makes the business rules independently testable via unit tests with mocked repositories.

### Exception-driven flow

Business rule violations throw typed exceptions (`ValidationException`, `ConflictException`, `NotFoundException`). The `GlobalExceptionHandler` (registered as an `IExceptionHandler`) catches them centrally and converts them to `ProblemDetails` responses. This keeps error-handling logic out of the controllers entirely.

### Test strategy

- **Unit tests** (`InsuranceCoreAPI.UnitTests`) test the service layer in isolation using NSubstitute mocks. They run fast (no HTTP, no I/O) and cover every rule branch including boundary dates.
- **Integration tests** (`InsuranceCoreAPI.IntegrationTests`) use `WebApplicationFactory<Program>` to spin up the real application in-process and call it over HTTP. They validate that the full stack — routing, model binding, service logic, exception handling, and response serialisation — works end-to-end.
