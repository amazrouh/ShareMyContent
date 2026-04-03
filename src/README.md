# Share Showcase — dev scaffold

This folder contains the **ASP.NET Core** API and **React (Vite)** app described in `simplified_share_&_showcase/simplified_share_showcase_dotnet_react_technical_design.md`.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (9.x matches the template)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine) for **local PostgreSQL**
- [Node.js](https://nodejs.org/) 20.18+ works; **20.19+** recommended for some ESLint-related engine warnings. The project pins **Vite 6** so `npm run build` works on Windows without the Vite 8 / Rolldown optional-native issues seen on some setups.

## Database (PostgreSQL — local and production)

The API uses **PostgreSQL** via **Npgsql** (same as production, e.g. Railway).

**Local:** from the **repository root**:

```bash
docker compose up -d
```

This starts Postgres **16** on port **5432** with user/database `shareshowcase` (password `shareshowcase_dev`) — matching `appsettings.Development.json`.

**Migrations** run automatically on API startup (`Database.MigrateAsync()`).

**Production:** set the environment variable `ConnectionStrings__DefaultConnection` to your managed Postgres URL (do not commit secrets). Base `appsettings.json` does not include a connection string.

## Run the API

```bash
cd src/ShareShowcase.Api
dotnet run --launch-profile https
```

Use **`ASPNETCORE_ENVIRONMENT=Development`** (default in `launchSettings.json`) so `appsettings.Development.json` supplies the local connection string.

Defaults: **https://localhost:7194** and http://localhost:5115. Health check: `GET /api/v1/system/health`.

## Run the web app

In another terminal:

```bash
cd src/share-showcase-web
npm install
npm run dev
```

Open **http://localhost:5173**. The Vite dev server **proxies** `/api` to `https://localhost:7194` (see `vite.config.ts`), so the home page health check works without CORS issues. Ensure the API is running with the **https** profile so the port matches the proxy.

## Solution file

`ShareShowcase.sln` at the repository root includes `src/ShareShowcase.Api`.

## New EF migrations

From the repo root (with `dotnet-ef` local tool):

```bash
dotnet tool run dotnet-ef migrations add MigrationName --project src/ShareShowcase.Api/ShareShowcase.Api.csproj
```

Requires Postgres running (`docker compose up -d`).

## What’s implemented (P0)

- **PostgreSQL** + **EF Core** migrations, applied on startup.
- **ASP.NET Core Identity** + **JWT** (`Jwt` section in `appsettings.json`).
- **Folders** (default **Library** root on register) and **media files** stored under `App_Data/blobs/` (local disk; R2 planned for production).
- **API:** `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `GET /api/v1/auth/me`, `GET/POST /api/v1/folders`, `GET/POST .../folders/{id}/files`, `GET .../files/{id}/download`.
- **React:** `/login`, `/register`, protected **`/library`** (folders, subfolders, upload, download). JWT in `localStorage` (`shareshowcase_token`).

**Restart the API** after pulling changes so migrations run. If `dotnet run` locks the Debug build, stop it or build with `-c Release`.

## Next steps

- Share links (see technical design §7), R2 storage, bulk ZIP, upload-from-URL.
- Point production `VITE_API_BASE_URL` at your API host if the SPA is not served behind the same origin.
