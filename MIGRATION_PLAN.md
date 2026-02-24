# FishMMO SQL Server + .NET 10 Migration Plan

This repository is now wired for SQL Server and targets modern .NET runtimes.

## Current migration status

- ✅ Runtime-targeted projects have been moved from `net8.0` to `net10.0`.
- ✅ Installer SDK checks now target .NET 10.
- ✅ Shared configuration now includes a `Database:Provider` switch and a starter `SqlServer` settings section.
- ⏳ Database runtime remains SQL Server-backed (`FishMMO.Database.SqlServer`) as part of the completed SQL Server migration.

## Next steps

1. Add `FishMMO.Database.SqlServer` provider package references and config classes.
2. Implement `SqlServerDbContextFactory` and SQL Server service registry.
3. Port EF Core entity mappings and provider-specific SQL behavior.
4. Add SQL Server migration pipeline in `FishMMO-DB-Migrator`.
5. Update installer flow to provision SQL Server as an option and run provider-appropriate migrations.
6. Switch application defaults from SQL Server to SQL Server once end-to-end validation is complete.
