# AGENTS.md

## Project Overview

An F# .NET 10 web API example using the Oxpecker web framework. Implements a todo CRUD API with in-memory storage, bearer auth, and OpenAPI documentation rendered via Scalar.

## Essential Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Format code (fantomas)
dotnet fantomas .

# Lint
dotnet fsharplint lint *.fs
```

### Development Environment

The project uses a Nix flake providing `.NET SDK 10`, `fsautocomplete` (LSP), and `dprint` (markdown formatting):

```bash
nix develop   # or direnv allow if direnv is configured
```

Local .NET tools (fantomas, fsharplint) are defined in `.config/dotnet-tools.json`. The flake's `shellHook` runs `dotnet tool restore` automatically.

## File Structure & Compilation Order

F# compiles files in order. The `.fsproj` defines this sequence — **new files must be inserted at the correct position**:

| File | Purpose |
|------|---------|
| `Models.fs` | Domain types (`TodoItem` record) |
| `TodoStore.fs` | In-memory store via `MailboxProcessor` |
| `Auth.fs` | Demo bearer authentication handler |
| `Handlers.fs` | Endpoint handlers + request/error DTOs |
| `OpenApi.fs` | F#-aware OpenAPI schema transformers |
| `Program.fs` | App composition: DI, routing, middleware |

**All files** are in the same `OxpeckerApi` namespace root. Each module corresponds to the filename (e.g., `Models.fs` → `module OxpeckerApi.Models`).

## Architecture

### TodoStore (`TodoStore.fs`)

An actor-based in-memory store using `MailboxProcessor` to serialize state mutations. The message DU is `private` — external code must use the module-level functions:

- `TodoStore.t` is a type alias: `MailboxProcessor<TodoMessage>`
- `TodoStore.start ()` creates the agent with an empty `Map<Guid, TodoItem>`
- All mutations are single-threaded through the agent's mailbox
- Async replies use `PostAndAsyncReply`; fire-and-forget uses `Post`

### Handlers (`Handlers.fs`)

Endpoint handlers follow a curried pattern:

```fsharp
// No route params: store → EndpointHandler
let getTodos (store : TodoStore.t) : EndpointHandler = ...

// Route params: store → param → EndpointHandler  
let getTodo (store : TodoStore.t) (id : Guid) : EndpointHandler = ...
```

All handlers return `EndpointHandler` (a function `HttpContext → Task`), using `task { }` computation expressions. Response writing uses `ctx.WriteJson`, status codes via `ctx.SetStatusCode`.

### Routing (`Program.fs`)

Routes are organized by HTTP method using Oxpecker's `GET`, `POST`, `PUT`, `DELETE` list builders. Route patterns:

- Static: `route "/todos"`
- Parameterized: `routef "/todos/{%O:guid}" handler` — the parameter is passed as an additional argument to the handler
- Middleware composition: `(requireAuthenticated >=> handler)` uses kleisli composition

### Auth (`Auth.fs`)

A custom `AuthenticationHandler` that accepts a hardcoded bearer token. Constants:

- `DemoScheme = "DemoBearer"`
- `DemoToken = "demo-token"`

Valid request: `Authorization: Bearer demo-token`

### OpenAPI (`OpenApi.fs`)

Contains `FSharpRecordSchemaTransformer` and references `FSharpOptionSchemaTransformer` (from the Oxpecker.OpenApi NuGet package). The record transformer marks non-option fields as required in the generated schema.

Route-level OpenAPI metadata is attached via `addOpenApi` with `OpenApiConfig`, specifying request/response body types and operation-level config (summary, description, security requirements).

## Code Style & Conventions

### Formatting (via `.editorconfig` / fantomas)

- Spaces before parameters, members, colons, and invocations
- Commas: space after, not before
- Semicolons: space after, not before
- Multiline bracket style: **stroustrup** (opening brace at end of line, closing brace dedented)

### Naming

- Modules use PascalCase matching the filename
- Functions use camelCase
- Types (records, DUs) use PascalCase
- `[<Literal>]` constants use PascalCase
- `[<RequireQualifiedAccess>]` on modules that expose a type alias (e.g., `TodoStore`) to avoid naming collisions

### Error Handling

Errors use a record type `{ Error: string; Details: string }` serialized as JSON with the appropriate HTTP status code. There is no exception-based error handling.

## Gotchas

- **Lockfile is enforced**: `RestorePackagesWithLockFile` is true in the `.fsproj`. After adding/updating NuGet packages, run `dotnet restore --lock-file-mode update` to regenerate `packages.lock.json`.
- **Compilation order matters in .fsproj**: adding a new `.fs` file requires inserting `<Compile Include="NewFile.fs" />` at the correct position before any file that depends on it.
- **No test project exists** — this is a demonstration API only.
- **`FSharpOptionSchemaTransformer`** is defined in the `Oxpecker.OpenApi` package, not in the project's `OpenApi.fs`. The file only contains `FSharpRecordSchemaTransformer`.
- **`TodoMessage` DU is `private`** — you cannot construct these messages directly; use the module functions on `TodoStore`.
- **Store is ephemeral** — all data is lost on restart (in-memory `Map`).
