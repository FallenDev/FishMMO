# FishMMO SQL Server + .NET 10 Migration Plan

This repository is currently wired for PostgreSQL (`Npgsql`) and several runnable tools target `.NET 8`.

## Current migration status

- ✅ Runtime-targeted projects have been moved from `net8.0` to `net10.0`.
- ✅ Installer SDK checks now target .NET 10.
- ✅ Shared configuration now includes a `Database:Provider` switch and a starter `SqlServer` settings section.
- ⏳ Database runtime remains PostgreSQL-backed (`FishMMO.Database.Npgsql`) until SQL Server provider implementation is completed.

## Next steps

1. Add `FishMMO.Database.SqlServer` provider package references and config classes.
2. Implement `SqlServerDbContextFactory` and SQL Server service registry.
3. Port EF Core entity mappings and provider-specific SQL behavior.
4. Add SQL Server migration pipeline in `FishMMO-DB-Migrator`.
5. Update installer flow to provision SQL Server as an option and run provider-appropriate migrations.
6. Switch application defaults from PostgreSQL to SQL Server once end-to-end validation is complete.
