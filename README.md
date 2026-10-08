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

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
