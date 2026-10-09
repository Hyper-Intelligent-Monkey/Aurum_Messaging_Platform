# Aurum Messaging

A modern, full-stack, real-time messaging web application. Designed for direct 1-on-1 communication, media exchange, and seamless user interaction. Built with **ASP.NET Core (.NET 10)**, **SignalR WebSockets**, **PostgreSQL**, and **Vue 3**.

---

## Features

- **Real-Time 1-on-1 Messaging**: Real-time messaging powered by ASP.NET Core SignalR WebSockets.
- **Typing Indicators & Presence**: Live typing notifications and online/offline status tracking with "last seen" timestamps.
- **Media & Attachment Sharing**: Send and receive photos, videos, voice recordings, and documents.
- **User Authentication & Management**:
  - Manual registration and login with email verification.
  - OTP password reset.
  - Google OAuth 2.0 integration.
  - Case-insensitive unique usernames.
  - Avatar uploads.
- **Chat Controls**:
  - Context menus.
  - Chat muting with custom durations.
  - User blocking and unblocking.
  - Search existing users.
- **UI/UX**:
  - Responsive design optimized for both desktop and mobile screens.
  - Interactive emoji picker.

---

## Current Limitations

- **Group Chat is not Supported**: The platform is strictly designed for direct 1-on-1 private messaging. Group conversations, group administration, and multi-user room management are not currently supported.
- **No Live Audio/Video Calls**: While sending voice messages (audio recordings) and video file attachments is supported, live real-time audio/video calling is not supported.
- **No In-App GIF Search**: GIF can be sent as a file attachment, but no GIF search functionality is implemented.
- **Transport Security Only (No E2EE)**: All communications are encrypted in transit over TLS (HTTPS & WSS) and stored securely in PostgreSQL, but client-side End-to-End Encryption (zero-knowledge E2EE) is not implemented.

---

## Tech Stack

### Backend
- **Framework**: ASP.NET Core (.NET 10)
- **Architecture**: Domain-Driven Design (DDD)
- **Database & ORM**: PostgreSQL via Entity Framework Core 10
- **Real-Time Engine**: ASP.NET Core SignalR
- **Authentication**: JWT Bearer Tokens,  BCrypt.Net, and Google OAuth 2.0
- **Validation**: FluentValidation
- **Mail Delivery**: SMTP Email Service

### Frontend
- **Framework**: Vue 3
- **Build Tool**: Vite
- **UI Framework & Styling**: Bootstrap 5 & Scoped CSS
- **State Management**: Pinia
- **Routing**: Vue Router 4
- **Real-Time Client**: Microsoft SignalR
- **Form Validation**: Vuelidate
- **Icons**: Heroicons & FontAwesome

---

## Architecture Overview
The solution adheres to Clean Architecture on the backend and modern component-driven state architecture on the frontend:

```text
/
├── client/                     # Vue 3 Single Page Application
│   ├── src/
│   │   ├── components/         # Reusable UI & modal components
│   │   ├── views/              # Pages (Chat, Contacts, Profile, Auth)
│   │   ├── store/              # Pinia state stores (auth, chat, toast)
│   │   └── services/           # API fetch client & SignalR connection
│   └── package.json
│
└── server/                     # ASP.NET Core Backend Solution
    ├── src/
    │   ├── Domain/             # Core Entities, Exceptions, Business Invariants
    │   ├── Application/        # DTOs, Interfaces, Business Services, Validators
    │   ├── Infrastructure/     # EF Core, PostgreSQL Context, Repositories, SMTP
    │   └── Api/                # Controllers, SignalR Hubs, Program.cs entry
    └── MessagingServer.slnx
```

---

## Getting Started (Local Development)

### Prerequisites

Ensure you have the following installed:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or higher)
- [Node.js (v18+)](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/) (running locally or via Docker)

---

### Guide to all commands

Available commands: [`package.json`](package.json)
run commands in the root workspace:

```bash

npm run #script name

```

---

### 1. Environment Setup

1.  In [`client/.env.example`](client/.env.example), remove the `.example` extension and populate the following contents with your own frontend configurations.

2. In [`server/src/Api/.env.example`](server/src/Api/.env.example), remove the `.example` extension and populate the following contents with your own backend configurations.

---

### 2. Database Migration

Migrate your database:

```bash
# in the root workspace
npm run migrate

# For adding a migration file example:
npm run migration:add -- AddUserTable

```

---

### 3. Running Locally

You can run both client and server from the root workspace:

```bash
# Install frontend dependencies
npm --prefix client install

# Start both backend and frontend concurrently:
npm run dev
```

Alternatively, run them in separate terminals:

```bash
# Terminal 1:
dotnet watch --project server/src/Api

# Terminal 2:
npm --prefix client run dev       
```

--- 

### 4. Build & Verification

Run in the root workspace:

```bash
# Compile entire solution (Backend + Frontend)
npm run build

# Run backend unit/integration tests
dotnet test server/
```

---

## Production Deployment (Docker & Caddy)

The production environment is containerized and orchestrated using **Docker Compose** and **Caddy** for automatic SSL termination and reverse proxying.

### Containerized Stack
- **`aurum-postgres`** (`postgres:16-alpine`): Isolated database container with healthcheck verification and persistent storage.
- **`aurum-api`** (Multi-stage .NET 10): Lean ASP.NET Core production runtime container on port `8080`.
- **`aurum-caddy`** (`caddy:2-alpine`): Edge reverse proxy managing automatic Let's Encrypt HTTPS/TLS certificates, WebSocket upgrades for SignalR, and Gzip/Zstandard compression on ports `80` and `443`.

### 1. Configure Production Environment Variables
Copy [.env.production.example](.env.production.example) to `.env` (or rename it to .env) and populate the following contents with your production environment configurations.

### 2. Push to your Repository
Clone your repository in your chosen cloud server (e.g. Google Cloud VM).

### 3. Deploy with Docker Compose
Open the Cloud Server terminal and run the following commands on your cloud server (e.g. Google Cloud VM):

```bash
# Build and launch all containers in detached mode
docker compose up -d --build

# Verify container health status
docker compose ps

# Monitor live logs
docker compose logs -f
```

### 4. Updating the Production Deployment
To pull code updates and redeploy without downtime:

```bash
git pull origin main
docker compose up -d --build
```

---

## AI Assistant & Antigravity Setup

This project utilized **Google Antigravity** using [`AGENTS.md`](AGENTS.md) for additional context to enhance code quality and maintainability.

### MCP Configuration
At [`.agents/mcp_config.example.json`](.agents/mcp_config.example.json), remove the `.example` extension and populate this configuration if you have other MCP servers to use for this project. 

**Tools included:**
- **`dotnet-sdk`** (`Community.Mcp.DotNet`): .NET CLI tooling, build diagnostics, and EF Core migrations.
- **`nuget-manager`** (`NuGet.Mcp.Server`): Dependency search and supply-chain vulnerability auditing.

---

## Performance & Optimization

### Backend & Database
- **Query Cancellation**: Propagates `CancellationToken` across all asynchronous EF Core database calls to terminate orphaned queries when users disconnect.
- **Fast Search Indexing**: Stores pre-normalized usernames to enable indexed case-insensitive lookups without slow runtime `LOWER()` scans.

### Frontend & Real-Time
- **Multi-Tier Caching**: Cache avatars to eliminate redundant fetches across navigation.
- **Non-Blocking UI Rendering**: Leverages `decoding="async"` (off-thread image decoding) and `loading="eager"` on critical assets to prevent frame drops.
- **Socket Optimization**: Debounces typing events and uses automatic SignalR reconnection with room state re-syncing.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
