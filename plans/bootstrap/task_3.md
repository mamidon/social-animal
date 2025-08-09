Install NuGet packages and configure project dependencies

This task installs all necessary NuGet packages for each project layer, establishing the technical foundation for Entity Framework Core with PostgreSQL, NodaTime for timestamp handling, and other essential dependencies following the patterns in CLAUDE.md.

## Work to be Done

### SocialAnimal.Core Package Installation
The Core project should remain minimal with only essential domain dependencies. Install NodaTime for precise timestamp handling as specified in the architecture:

```bash
cd SocialAnimal.Core
dotnet add package NodaTime --version 3.1.11
```

Core intentionally has minimal dependencies to maintain its independence from infrastructure concerns, following Clean Architecture principles.

### SocialAnimal.Infrastructure Package Installation
Infrastructure requires packages for database access, Entity Framework Core with PostgreSQL support, and naming convention handling:

```bash
cd SocialAnimal.Infrastructure

# Entity Framework Core with PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.4
dotnet add package EFCore.NamingConventions --version 8.0.3

# NodaTime support for Entity Framework
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime --version 8.0.4

# Core EF tools for migrations
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.8
```

These packages enable:
- PostgreSQL as the database with snake_case naming convention support
- NodaTime `Instant` type mapping for precise UTC timestamps
- Entity Framework migrations and database management

### SocialAnimal.Web Package Installation
The Web API project needs ASP.NET Core packages and authentication support:

```bash
cd SocialAnimal.Web

# Authentication and JWT support
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.8
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.8

# API documentation
dotnet add package Swashbuckle.AspNetCore --version 6.6.2

# Structured logging with Serilog
dotnet add package Serilog.AspNetCore --version 8.0.2
dotnet add package Serilog.Sinks.Console --version 5.0.1
```

These packages provide:
- JWT token authentication as specified in the tech stack
- ASP.NET Core Identity for user management
- Swagger/OpenAPI documentation
- Serilog for structured logging following the portal pattern

### SocialAnimal.Tests Package Installation
The test project requires testing frameworks and utilities:

```bash
cd SocialAnimal.Tests

# Testing frameworks
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.8
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Moq --version 4.20.70

# Test database support
dotnet add package Testcontainers.PostgreSql --version 3.9.0

# Code coverage
dotnet add package coverlet.collector --version 6.0.2
```

These packages enable:
- Integration testing with `WebApplicationFactory`
- Fluent assertion syntax for readable tests
- Mocking for unit tests
- PostgreSQL test containers for integration tests
- Code coverage collection

### Global Tool Installation
Install Entity Framework Core tools globally for migration management:

```bash
dotnet tool install --global dotnet-ef --version 8.0.8
```

### Configure Package Management
Create a `Directory.Build.props` file in the solution root to centralize package versions and common properties:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <PropertyGroup>
    <EFCoreVersion>8.0.8</EFCoreVersion>
    <NpgsqlVersion>8.0.4</NpgsqlVersion>
    <NodaTimeVersion>3.1.11</NodaTimeVersion>
  </PropertyGroup>
</Project>
```

## Relevant Patterns from CLAUDE.md

- **Time and Date Handling**: Use NodaTime `Instant` for UTC timestamps with custom Entity Framework converters
- **Configuration Management**: Type-safe configuration with strongly-typed classes
- **Service Organization**: Dependency injection with convention-based discovery
- **Testing Patterns**: Integration tests with real database connections, mock objects for external dependencies

## Deliverables

1. Core project with NodaTime package installed
2. Infrastructure project with Entity Framework Core, PostgreSQL, and NodaTime EF support
3. Web project with authentication, logging, and API documentation packages
4. Tests project with testing frameworks and test container support
5. Global dotnet-ef tool installed for migration management
6. Directory.Build.props file for centralized package management

## Acceptance Criteria

- All projects build successfully after package installation
- Package versions are consistent and compatible with .NET 8.0
- No package version conflicts or warnings
- Entity Framework tools are accessible via `dotnet ef` command
- Test project can reference all necessary testing utilities
- Serilog is configured for structured logging
- PostgreSQL with snake_case naming convention is ready for configuration