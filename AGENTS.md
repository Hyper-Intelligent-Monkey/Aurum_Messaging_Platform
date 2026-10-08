# Project Guidelines: Messaging Platform

## Tech Stack Overview
- **Backend**: ASP.NET Core (C#), Entity Framework Core, SignalR (WebSockets for real-time messaging), DDD/Clean Architecture.
- **Frontend**: Vue 3 (Composition API `<script setup>`), Vite, Vue Router, Pinia state management.

## Code Conventions & Architecture
1. **Domain Entities (C#)**:
   - Use Rich Domain Models with encapsulated state (private setters).
   - Instantiate entities using static factory methods (e.g., `User.Create(...)`, `Message.Create(...)`).
   - Throw `DomainException` for validation failures.
2. **Real-time Messaging (SignalR)**:
   - Always define strong contracts for SignalR hubs and client callbacks.
   - Pair every C# Hub method with a corresponding Vue 3 composable (`useSignalR` or similar).
3. **Frontend Components (Vue 3)**:
   - Use `<script setup lang="ts">` (or JS) with Vue 3 Composition API.
   - Keep chat UI state inside Pinia stores (`useChatStore`, `useAuthStore`).

## Verification & Build Commands
- **Backend Build/Test**: `dotnet build server/MessagingServer.slnx` and `dotnet test server/`
- **Frontend Dev/Test**: `npm run dev` and `npm run test` (inside `client/`)

# Custom Agent Rules

- **Strict User Permission for File Changes**: NEVER create, modify, or delete project files automatically. Always present the proposed changes, explain the rationale line-by-line, and wait for explicit user permission before executing any edits.
- **Line-by-Line Technical Explanations**: Explain how proposed changes work line-by-line, outlining required actions step-by-step before seeking confirmation.
- **Single-Scope Execution**: Work on one single file or task at a time as directed by the user. Do not make cascading changes across unrelated files unless explicitly asked.

