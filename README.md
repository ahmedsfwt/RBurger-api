# RBurger Backend (ASP.NET Core 8)

Clean Architecture backend for the RBurger food-delivery platform, implementing the API
contract in **Technical & Product Documentation v1.2**.

## Solution layout

```
src/
  RBurger.Domain/           Entities, enums - no dependencies
  RBurger.Application/      CQRS (MediatR), DTOs, validators, interfaces
  RBurger.Infrastructure/   EF Core, repositories, SignalR, JWT, caching
  RBurger.Api/              Controllers, Program.cs, middleware
tests/
  RBurger.Application.Tests/       Unit tests (dependency-free fakes)
  RBurger.Api.IntegrationTests/    WebApplicationFactory-based auth-matrix tests
```

## Running locally

Requirements: .NET 8 SDK, SQL Server (local, container, or RDS).

1. Set configuration (see **Configuration** below) via `dotnet user-secrets` or environment
   variables - **never commit real values to `appsettings.json`**, which ships with every
   secret-shaped value blank.
2. Restore, build, migrate, run:

```bash
dotnet restore
dotnet build

cd src/RBurger.Infrastructure
dotnet ef database update --startup-project ../RBurger.Api
cd ../..

dotnet run --project src/RBurger.Api
```

3. Health check: `GET /health` returns `{ "status": "healthy" }`. Suitable for an ECS/ALB
   target group health check (no DB round-trip by design - see `Program.cs`).

## Configuration

All secrets/environment-specific values are configuration-driven, not hardcoded. Locally, use
.NET User Secrets (`dotnet user-secrets set "Key" "value" --project src/RBurger.Api`); in AWS,
these map to Secrets Manager / Parameter Store per §10.1.

| Key | Purpose | Required |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | Yes |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` | JWT signing (undocumented in v1.2 - operational config) | Yes |
| `RefreshToken:ExpiresInDays` | Refresh-token TTL (default 30) | No |
| `RateLimiting:AuthPermitLimit` / `AuthWindowSeconds` | Fixed-window limit on `/auth/*` | No |
| `RateLimiting:PaymentPermitLimit` / `PaymentWindowSeconds` | Fixed-window limit on `/payments/*` | No |

## Migrations

Migrations are source-controlled under `src/RBurger.Infrastructure/Migrations/`. To add a new
one after a model change:

```bash
cd src/RBurger.Infrastructure
dotnet ef migrations add <DescriptiveName> --startup-project ../RBurger.Api
dotnet ef database update --startup-project ../RBurger.Api
```

## Tests

```bash
dotnet test
# or, matching the Release build used for deployment:
dotnet build -c Release
dotnet test -c Release --no-build
```

`RBurger.Application.Tests` uses hand-written in-memory fakes (no mocking framework, no real
DB) for every repository/external interface. `RBurger.Api.IntegrationTests` boots the real
`Program.cs` via `WebApplicationFactory` and asserts the full role-authorization matrix (every
route x every wrong-role JWT -> 403/401).

## Production integrations - what's ready vs. what needs external configuration

This backend keeps every external integration behind a clean interface
(`IPaymentProvider`, `IMenuItemImageStorage`) so the abstraction and API contract are complete
and testable **without** real credentials. Swapping in a real provider means implementing the
interface and registering it in `RBurger.Infrastructure`'s DI - no Application/API changes.

| Integration | Backend code | Status |
|---|---|---|
| Payments (Paymob/Fawry) | `IPaymentProvider` | Abstraction complete; `NotConfiguredPaymentProvider` always returns 503 until real gateway credentials are supplied |
| Menu image storage (S3/CloudFront) | `IMenuItemImageStorage` | Abstraction complete; `NotConfiguredMenuItemImageStorage` always returns 503 until an AWS bucket/region/CDN domain is configured |
| SignalR backplane (Redis/ElastiCache) | n/a | Not configured - single-instance in-memory SignalR only; required before running more than one API instance behind a load balancer (§10.3) |
| Analytics cache | `ICacheService` | In-process `IMemoryCache` (§12) - swappable to Redis later via the same interface, not configured for distributed use yet |

**No AWS resources (ECS, RDS, S3, CloudFront, Secrets Manager, CDK, GitHub Actions) are part of
this repository.** Provisioning those is a separate, infrastructure-repository concern (§10-§11
of the documentation) and is explicitly out of scope for this backend codebase.

## Known/flagged contract decisions

A number of implementation decisions were required where Documentation v1.2 is silent or
ambiguous. Each is documented at its point of use with an XML comment explaining the decision
and citing the relevant section. Notable ones:

- `POST /api/v1/auth/refresh` now returns `{ accessToken, refreshToken, expiresInSeconds }` -
  the one deliberate, explicitly approved deviation from §7.1's literal example payload (which
  shows only `accessToken`/`expiresInSeconds`), needed so a client can actually continue using
  refresh-token rotation after the first call.
- `POST /api/v1/auth/logout` request shape (`{ refreshToken }`, public) mirrors `/refresh`
  exactly - not documented in v1.2 at all.
- `GET /api/v1/admin/analytics/overview`'s `completionRatePercent` (and its delta) are returned
  as `null` - the formula is undefined anywhere in Documentation v1.2.
