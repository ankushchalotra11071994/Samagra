---
name: new-endpoint
description: Add a new REST endpoint to Samagra.API following the Clean Architecture layering (Application interface → Infrastructure implementation → API controller). Use when the user wants a new API route or feature backed by the database.
---

# New API endpoint

Layering (dependencies point inward): `Domain` ← `Application` ← `Infrastructure` ← `API`.

## Steps

1. **Domain** — add or update the entity only if needed (if the schema changes, follow the `ef-migration` skill).
2. **Application** — add an interface in `BackEnd/Samagra.Application/Interfaces/` (e.g. `IBuyerRepository`). Async methods with a `CancellationToken ct = default` parameter. No EF Core or ASP.NET types here.
3. **Infrastructure** — implement it in `Samagra.Infrastructure/Repositories/`, injecting `AppDbContext` (see `BuyerRepository`). Use `AsNoTracking()` for reads.
4. **DI** — register it in `BackEnd/Samagra.API/Program.cs`: `builder.Services.AddScoped<IThing, Thing>();`. Check it isn't already registered.
5. **API** — controller in `Samagra.API/Controllers/`:
   - `[ApiController]`, `[Route("api/[controller]")]`, inherit `ControllerBase`.
   - Depend on the Application interface, not `AppDbContext` directly.
   - `[Authorize]` for signed-in users, `[Authorize(Roles = Roles.Admin)]` for admin-only. Auth comes from the `access_token` cookie, so no extra header handling is needed.
   - Request/response records go in `Samagra.API/Contracts/`. Model validation runs automatically through the global `ModelValidation` filter.
   - For per-user data, read the user id from claims (`ClaimTypes.NameIdentifier`) and filter by it.
   - Pass `CancellationToken ct` from the action down to the repository.
6. For read-heavy endpoints, consider `HybridCache` (already registered) or `[OutputCache]`.
7. If the frontend needs it, add a call in `FrontEnd/Samagra-app/src/services/` using the shared axios instance from `src/api/axios.js`.
8. `cd BackEnd && dotnet build`, and add an example request to `Samagra.API/Samagra.API.http` if that's useful.
