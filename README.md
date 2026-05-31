# Nexus Learning System

Nexus Learning System is an ASP.NET Core MVC foundation for a professional Learning Management System (LMS).

## Foundation Architecture

The solution is organized as a clean 3-tier architecture:

- `Nexus.Web` - ASP.NET Core 9 MVC presentation layer, Bootstrap 5 assets, routing, authentication middleware, and application startup configuration.
- `Nexus.Business` - Business layer contracts and dependency injection entry point. Business modules will be added in later phases.
- `Nexus.Data` - Entity Framework Core persistence layer, SQL Server configuration, ASP.NET Identity context, and repository/unit-of-work implementations.

## Configured Foundation

- ASP.NET Core 9 MVC
- Entity Framework Core 9
- SQL Server connection string configuration
- ASP.NET Identity with `ApplicationUser`
- Dependency injection extension methods for each layer
- Repository pattern and unit of work abstractions
- Bootstrap 5 front-end assets from the MVC template

Business modules are intentionally not included in this phase.
