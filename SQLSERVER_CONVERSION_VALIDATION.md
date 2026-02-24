# SQL Server Conversion Status

This repository has been updated for SQL Server-only runtime usage.

## Completed conversion highlights

- SQL Server factory/configuration contracts are now used across runtime services.
- SQL Server provider packages are retained; legacy non-SQL Server package references were removed from active project files.
- Runtime app DI registrations now resolve SQL Server components.
- Release setup configuration now uses SQL Server settings only.
- Installer database provisioning now enforces SQL Server 2022 compatibility level (`160`).

## Note

Legacy file paths from earlier provider implementations may remain for repository history, but active code paths and namespaces are SQL Server-oriented.
