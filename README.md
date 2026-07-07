# Kartly

Monorepo containing the Kartly backend (.NET Web API) and frontend (Angular).

## Structure

```
Kartly/
├── backend/            # .NET solution
│   ├── Kartly.sln
│   └── src/
│       └── Kartly.Api/ # ASP.NET Core Web API (controllers)
└── frontend/           # Angular application (kartly-web)
    └── proxy.conf.json # Proxies /api -> backend during dev
```

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/) (`dotnet --version`)
- [Node.js 22.12+](https://nodejs.org/) and npm

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

## Running both

Start the backend in one terminal and the frontend in another. Frontend API
calls to `/api/...` are transparently forwarded to the .NET API.
