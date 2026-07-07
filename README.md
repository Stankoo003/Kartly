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

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/) (`dotnet --version`)
- [Node.js 22.22.3+ / 24.15+ / 26+](https://nodejs.org/) and npm (required by Angular 22)

## Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/Kartly.Api
```

The API listens on `http://localhost:5185` (and `https://localhost:7276`).
OpenAPI is available in Development at `/openapi/v1.json`.

## Frontend

```bash
cd frontend
npm install
npm start
```

The app runs on `http://localhost:4200` and proxies `/api/*` requests to the
backend via `proxy.conf.json`.

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