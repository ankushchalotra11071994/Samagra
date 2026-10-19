# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

- `BackEnd/` — .NET 10 solution `Samagra.slnx` (Clean Architecture). All `dotnet` commands run from here.
- `FrontEnd/Samagra-app/` — React 19 + Vite + Tailwind v4 SPA. All `npm` commands run from here.
- `.claude/skills/` — repo workflows worth reading before doing the matching task: `new-endpoint`, `ef-migration`, `add-ai-tool`, `rag-eval`.
- `docker-compose.yml` — local SQL Server, PostgreSQL + pgvector and Redis (`docker compose up -d`); the `vector` extension is created by `docker/init-pgvector.sql` on first run.
- `FEATURE_HANDOFF.md` — walkthrough of the bulk-import feature, including what is still unapplied/untested.

## Commands

Backend (from `BackEnd/`):

```bash
dotnet restore && dotnet build
dotnet test                                                   # xUnit + NSubstitute + Shouldly
dotnet test --filter "FullyQualifiedName~BuyerTests"          # single test class / method
dotnet run --project Samagra.API                              # http://localhost:5109, Swagger at /swagger
dotnet ef database update --project Samagra.Infrastructure --startup-project Samagra.API
dotnet ef migrations add <Name> --project Samagra.Infrastructure --startup-project Samagra.API
```

The only test project is `BackEnd/tests/Samagra.Application.Tests` and it references **only** `Samagra.Application` and `Samagra.Domain` — testing anything in Infrastructure/AI/API means adding a project reference (or a new test project to `Samagra.slnx`) first.

RAG evaluation harness (console app, needs real AI provider + databases):

```bash
dotnet run --project Samagra.AiEval                           # runs testset.json against current index
dotnet run --project Samagra.AiEval -- --chunk-size 300 --overlap 50   # clears and reindexes first
```

Passing `--chunk-size` (or `--reindex`) re-embeds `EvalDocuments` before the run; without either flag it evaluates whatever is already indexed.

Azure Function (from `BackEnd/Samagra.Functions/`): `func start` — settings in `local.settings.json` (git-ignored).

Frontend (from `FrontEnd/Samagra-app/`):

```bash
npm run dev      # Vite on :5173, proxies /api → http://localhost:5109
npm run build
npm run lint
```

There are no frontend tests. `npm run lint` still reports pre-existing errors in
`components/HooksPractice.jsx` (a scratch file — `fetchData` is undefined, `useEffect` is called
without a callback) and the mixed-export rule in `context/CartContext.jsx`; everything else is clean.

CI: GitHub Actions (`.github/workflows/dotnet.yml`) builds/tests backend and builds frontend; `azure-pipelines.yml` additionally publishes, builds the Docker image (`BackEnd/Dockerfile`) and deploys to an Azure Web App.

## Configuration

Real connection strings and keys live in `BackEnd/Samagra.API/appsettings.json`, which is **git-ignored** (along with `appsettings.Development.json` and `local.settings.json`). `appsettings.json.example` is the tracked template — when a config key is added, add a placeholder for it there too. Because the real file never reaches a build agent, deployed environments must supply the same values as App Service settings (`ConnectionStrings__Default`, `Ai__ApiKey`, …).

`Samagra.API` needs:
- `ConnectionStrings:Default` (SQL Server, EF Core + Identity), `ConnectionStrings:VectorDb` (PostgreSQL with `vector` extension), `ConnectionStrings:Redis`
- `Jwt` section (validated on start)
- `Ai` section (`AiOptions`): `Provider` = `OpenAI` (Azure OpenAI, needs `Endpoint`/`ApiKey`) or `Ollama`. Startup throws if required values are missing. `Pricing` per model drives cost tracking; `DailyBudgetPerUserUsd` drives the budget guard.
- `BulkImport` section plus `ConnectionStrings:BlobStorage` / `ServiceBus` for the import pipeline (resolved lazily, so the API starts without them).

The vector table depends on the provider: `document_chunks` (1536 dims, Azure embeddings) vs `document_chunks_768` (Ollama `nomic-embed-text`, schema in `Samagra.AI/Rag/Sql/`). `AiOptions.Active*` picks model + table from `Provider`, so switching provider requires reindexing into the matching table. Table names go straight into SQL, so they're validated by `VectorTableName`.

`Program.cs` seeds the `Buyer`/`Admin` roles on every start; in Development it also seeds the test logins listed in `Samagra.API/Seed/DevDataSeeder.cs`:

| Email | Password | Role |
|---|---|---|
| `admin@admin.com` | `admin` | Admin |
| `buyer@buyer.com` | `buyer` | Buyer |
| `admin@samagra.dev` | `admin` | Admin (the original dev admin, username `admin`) |

The seed is idempotent (skips any email that already exists). These passwords fail Identity's policy — only the length is relaxed in `Program.cs`, so digit/uppercase/symbol are still required — so the seeder sets `PasswordHash` through `IPasswordHasher` instead of `CreateAsync(user, password)`. Buyer accounts get their own `Buyer` row, like `POST /api/auth/register` creates.

## Architecture

Project dependencies: `Domain` ← `Application` ← `Infrastructure` ← `AI` ← `API` (and `AiEval`).

- **Application** holds only interfaces (`IAiAssistant`, `IAgentChat`, `IVectorSearch`, `IDocumentIndexer`, `IAiUsageRecorder`, `IBulkImport*`, …) and exceptions. Controllers depend on these — the API project must not reference Azure/OpenAI SDKs directly.
- **Infrastructure** — `AppDbContext` (SQL Server + ASP.NET Identity with `Buyer`/`Admin` roles), EF configurations, migrations, repositories, JWT token service, bulk-import Azure clients.
- **AI** — everything LLM-related, wired via `services.AddAiServices(config)` in `Samagra.AI/DependencyInjection.cs`.

Data access is not uniform: the CRUD controllers (`Products`, `Categories`, `Orders`, `Payments`, `Login`) inject `AppDbContext` directly, while `Buyer` and bulk-import jobs go through repositories behind Application interfaces. Follow the surrounding controller rather than introducing a third pattern.

Authorization is per-controller/action, with no fallback policy, so check the attributes rather than assuming. `AiController`, `OrdersController` and `PaymentsController` are `[Authorize]`; `/api/ai/index`, `/api/products` writes, `/api/categories` writes, `/api/orders` (list) + status, `/api/bulkimports`, `/api/admin/dashboard` and `/api/admin/ai-usage` are `Admin`-only. The two `GET`s that stay anonymous are the catalog (`/api/products/hybrid`, `/api/products/{id}`, which also returns deactivated rows to an admin) and `/api/categories`.

Anything scoped to a user takes the id from the token — `User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "uid"`, the same pattern as `Samagra.AI/Tools/OrderTools` — never from the request body. `POST /api/orders` therefore has no `UserId` field, and `GET /api/orders/{id}` plus the payment endpoints 404 rather than leak another buyer's row (an admin may read any).

Order status is a plain string on `Order.Status`. The allowed values and the legal transitions live in `OrderStatuses` at the bottom of `OrdersController.cs` (`PENDING → CONFIRMED → SHIPPED → DELIVERED`, cancellable until shipped, with `DELIVERED`/`CANCELLED` terminal); `PaymentsController` writes `CONFIRMED` on a successful payment, and `services/orderService.js` mirrors the transition table so the UI only offers legal moves. Placing an order decrements `Product.Stock` inside a transaction alongside the insert.

`CategoriesController.cs` is a LINQ-join playground on suffixed routes (`inner-join`, `summary`, …); the real category CRUD is a separate controller, `CategoriesCrudController.cs`, on the bare `/api/categories` resource. Product writes call `HybridCache.RemoveAsync("products:all:hybrid")`, otherwise admin edits stay invisible for 30 seconds.

### Auth
JWT is issued into an HttpOnly `access_token` cookie; `JwtBearerEvents.OnMessageReceived` in `Program.cs` reads it from the cookie. The frontend axios client (`src/api/axios.js`) uses `withCredentials: true`, and CORS policy `AllowReact` allows credentials from `http://localhost:5173`.

### IChatClient middleware pipeline (order matters)
`BudgetGuard → InputGuard → FunctionInvocation → CostTracking → provider`
- Budget guard is outermost so it checks once per question (throws `AiBudgetExceededException`).
- `InputGuardChatClient` uses `Guardrails/` (injection detection, PII masking); throws `UnsafeInputException`.
- Cost tracking is innermost so every tool-loop round-trip is recorded as its own `AiUsage` row. Embeddings have their own cost-tracking wrapper.
- These clients are singletons but `IAiUsageRecorder` needs a scoped `DbContext`, so they resolve it via `IServiceScopeFactory` per call — keep that pattern when adding middleware.
- Domain exceptions map to HTTP responses via `IExceptionHandler`s in `Samagra.API/ExceptionHandlers/`, registered before the catch-all `GlobalExceptionHandler` (order of `AddExceptionHandler` matters).

### Tools, RAG, agents, MCP
- `Tools/OrderTools` and `Tools/PolicyTools` are the actual C# implementations. The current user id is **never** a tool parameter — it comes from `HttpContext`, and every order query filters by it (order-by-id also checks ownership). Preserve this when adding tools.
- RAG: `TextChunker` → `DocumentIndexer` (embeds + stores in pgvector) → `VectorSearch` (cosine distance, results above a threshold are dropped before prompting).
- `AiAssistant` (`IAiAssistant`) backs `/api/ai/ask`, `/ask-rag`, `/ask-tools`.
- The API also hosts an MCP server at `/mcp` (requires auth) exposing `MCP/OrderMcpTools` and `MCP/PolicyMcpTools`, which wrap the same tool classes.
- `Agent/SupportAgent` (`IAgentChat`, Microsoft.Agents.AI) connects back to that MCP endpoint as a client (`Ai:McpServerUrl`), forwarding the caller's token, and persists conversation sessions in Redis (`IDistributedCache`, 30 min). `AiStreamController` streams its output as SSE events (`conversation`, `token`, `done`, `error`); the frontend consumes it with `@microsoft/fetch-event-source`.

### Bulk import (products CSV)
`BulkImportsController` → `POST upload-url` creates a `BulkImportJob` and returns a write-only SAS URL → browser PUTs the CSV straight to Blob Storage → `POST {id}/start` checks the blob and sends a `BulkImportMessage` to Service Bus → `Samagra.Functions` (isolated worker, `ProcessBulkImport`) streams the CSV and `SqlBulkCopy`s rows into `Products`, with row errors going to `BulkImportRowErrors`. All the inserts and the job's `Completed` update run in one transaction (the job row is locked), so a redelivered message never imports twice. The function uses plain ADO.NET, not EF, so status strings must match `BulkImportStatus` names. `InvalidImportFileException` signals a non-retryable failure.

### Caching
Redis (`Samagra:` key prefix) is the distributed cache; `HybridCache` layers L1 memory (10 s) over it (30 s) and is used in `ProductsController`; `OutputCache` is also enabled.

### Frontend
`src/App.jsx` nests every screen under `<RequireAuth>`, then splits into two shells: buyer screens render inside `components/ShopLayout` (top nav) and admin screens inside `components/AdminLayout` (dark sidebar) behind a second `<RequireAuth requireAdmin>`. Buyer routes are `/home`, `/products/:id`, `/cart`, `/order`, `/payment-success/:id`, `/orders`, `/orders/:id`, `/profile`, `/ai`; admin routes all sit under `/admin` (dashboard index, `products`, `products/new`, `products/:id/edit`, `categories`, `orders`, `orders/:id`, `bulk-import`, `ai-usage`). `/bulk-import` and `/Home` are kept as redirects for old links.

The access token is an HttpOnly cookie the SPA cannot read, so `context/AuthContext.jsx` calls `GET /api/auth/me` once on mount to learn who is signed in; `context/auth-context.js` holds the context object and `useAuth` hook (kept separate so the file exports only components / only helpers — the `react-refresh/only-export-components` lint rule). Sign-in must go through `useAuth().login()`, not a bare `api.post`, or the guard will bounce straight back to `/login`.

Data loading goes through `hooks/useAsyncData.js` (`{ data, loading, error, reload }`), which takes a `useCallback`-memoized fetcher. The ESLint config forbids synchronous `setState` in an effect body, so every fetch effect must do its first `setState` after an `await` — follow the hook rather than reintroducing a `load()` callback that starts with `setLoading(true)`.

`src/api/axios.js` is the shared client (`VITE_API_URL`, else `http://localhost:5109/api`); per-feature calls live in `src/services/*.js`; shared presentational bits are in `components/ui.jsx` and pure formatters in `lib/format.js`. Cart state is a `CartContext` provider, mounted in `App.jsx` alongside `AuthProvider`. Styling is Tailwind v4 via the Vite plugin — there is no `tailwind.config.js`.

## Conventions

Existing code carries inline comments in Hindi/Hinglish alongside English. Match the surrounding file rather than translating it.
