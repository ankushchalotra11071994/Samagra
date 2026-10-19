---
name: ef-migration
description: Change the SQL Server schema in Samagra — add/modify a Domain entity, its EF configuration, and create an EF Core migration. Use when the user adds an entity, column, index, or relationship.
---

# EF Core migration

## Where things live
- Entities: `BackEnd/Samagra.Domain/Entities/` (no EF attributes; Domain has no dependencies).
- `DbSet`s: `BackEnd/Samagra.Infrastructure/Data/AppDbContext.cs` (derives from `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`).
- Mapping: one `IEntityTypeConfiguration<T>` per entity in `Samagra.Infrastructure/Data/Configurations/` — picked up automatically by `ApplyConfigurationsFromAssembly`. Prefer this over adding to `OnModelCreating`.
- Migrations: `Samagra.Infrastructure/Migrations/`.

## Steps

1. Create/modify the entity in Domain.
2. Add a `DbSet<T>` to `AppDbContext` if it's a new entity (use the `=> Set<T>()` style).
3. Add/update the configuration class: table name, key, `IsRequired`, `HasMaxLength`, `HasPrecision(18, 2)` for money, indexes, relationships.
4. Build: `cd BackEnd && dotnet build`.
5. Create the migration (PascalCase name describing the change):
   ```bash
   cd BackEnd
   dotnet ef migrations add <Name> --project Samagra.Infrastructure --startup-project Samagra.API
   ```
   Design-time uses `AppDbContextFactory` (hard-coded local SQL Server at `localhost,1433`).
6. Read the generated `Up`/`Down` and point out anything destructive (dropped columns/tables, data-losing type changes, non-nullable columns without defaults on tables that already have rows).
7. Ask before applying it to the database:
   ```bash
   dotnet ef database update --project Samagra.Infrastructure --startup-project Samagra.API
   ```

Don't edit `AppDbContextModelSnapshot.cs` or migrations that are already applied by hand. To undo the latest unapplied migration: `dotnet ef migrations remove --project Samagra.Infrastructure --startup-project Samagra.API`.
