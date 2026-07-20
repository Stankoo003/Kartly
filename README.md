# Kartly

Monorepo containing the Kartly backend (.NET Web API) and frontend (Angular).

## Structure

```
Kartly/
├── backend/                    # .NET solution (layered / clean architecture)
│   ├── Kartly.slnx
│   └── src/
│       ├── Kartly.Api/            # HTTP layer: controllers, Program.cs, DI wiring
│       ├── Kartly.Application/    # Domain + business logic + contracts (no deps)
│       └── Kartly.Infrastructure/ # Data-access: repository implementations
└── frontend/           # Angular application (kartly-web)
    └── proxy.conf.json # Proxies /api -> backend during dev
```

## Running the project

Prerequisites:

- [.NET SDK 10](https://dotnet.microsoft.com/) (`dotnet --version`)
- [Node.js 22.22.3+ / 24.15+ / 26+](https://nodejs.org/) and npm
- [Docker](https://www.docker.com/) (for PostgreSQL)

Run each step in its own terminal.

1. Database:

```bash
docker compose up -d
```

Starts PostgreSQL on `localhost:5432`. On first backend run the schema is
migrated and a default admin is seeded automatically.

2. Backend:

```bash
cd backend
dotnet run --project src/Kartly.Api
```

API on `http://localhost:5185`. OpenAPI (Development) at `/openapi/v1.json`.

3. Frontend:

```bash
cd frontend
npm install   # first time only
npm start
```

App on `http://localhost:4200`, proxies `/api/*` to the backend.

Log in with the seeded admin: `admin@kartly.local` / `Admin123!`.

## Tests

Integration tests (backend) spin up a throwaway PostgreSQL via Testcontainers,
so they need Docker running but do not touch the dev database:

```bash
cd backend
dotnet test
```

## Authentication & authorization

User storage and password hashing use **ASP.NET Core Identity** (EF Core +
PostgreSQL). Login/registration issue a **JWT** that carries the user's role
(`Admin` or `Customer`).

### Endpoints

| Method | Route                | Access                     |
| ------ | -------------------- | -------------------------- |
| POST   | `/api/auth/register` | Anonymous (defaults to `Customer`; pass `"role": "Admin"` to create an admin) |
| POST   | `/api/auth/login`    | Anonymous — returns a JWT  |
| GET    | `/api/products`      | Any authenticated user     |
| POST   | `/api/products`      | `Admin` role only          |

On first run the app migrates the database and seeds a default admin:
`admin@kartly.local` / `Admin123!` (see `KartlyDbInitializer`).

### Configuration

Set the PostgreSQL connection string and a real signing key (do **not** ship the
placeholder) via `appsettings.json` or user-secrets:

```
ConnectionStrings:Postgres  = Host=localhost;Database=kartly;Username=postgres;Password=postgres
Jwt:Key                     = <a long random secret, ≥ 32 chars>
```

Apply migrations manually if needed:

```bash
cd backend
dotnet ef database update --project src/Kartly.Infrastructure --startup-project src/Kartly.Api
```

### Refresh strategy

**Chosen: single long-lived access token, no refresh token.** The login
endpoint returns one JWT valid for `Jwt:ExpiryHours` (default 8h); when it
expires the user simply signs in again. The Angular `AuthService` drops the
token from storage once past its `expiresAt` so stale tokens are never sent.

Trade-off: simplest possible design, but a leaked token stays valid until it
expires and there is no server-side revocation. The upgrade path is documented
in code (`JwtSettings`): shorten the access token and add a persisted,
revocable refresh token plus a `POST /api/auth/refresh` endpoint.

### Frontend wiring (Angular)

- `auth/auth.service.ts` — stores the token in `localStorage`, exposes
  `isAuthenticated` / `isAdmin` signals.
- `auth/auth.interceptor.ts` — HTTP interceptor that attaches
  `Authorization: Bearer <token>` to API calls.
- `auth/admin.guard.ts` — route guard protecting the `/admin` route.

## Order lifecycle states

The Order entity follows a defined lifecycle with the following states:
Pending — the order has been created but has not yet been processed or confirmed.
Confirmed — the order has been validated and accepted for fulfillment.
Shipped — the order has been dispatched and is no longer in the preparation phase.
Cancelled — the order has been terminated before completion.

Pending -> Confirmed

Pending -> Cancelled

Confirmed -> Shipped

Confirmed -> Cancelled

## Transition rules
Pending represents the initial state of an order.
An order may be confirmed only after it has been created.
An order may be cancelled while it is still in the Pending or Confirmed state.
Once an order is marked as Shipped, it is considered final and no further state transitions are allowed.
Cancelled is a terminal state and cannot transition to any other state.


### A valid order flow is:

Pending -> Confirmed -> Shipped
An order may also be terminated earlier in the process:
Pending -> Cancelled
or
Pending -> Confirmed -> Cancelled