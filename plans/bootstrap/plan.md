# Social Animal - Bootstrap Implementation Plan

## Project Overview
Social Animal is a C#/.NET web service to facilitate hosting social events. This plan outlines the bootstrap phase to establish the foundational architecture and infrastructure.

## Tech Stack
- **Backend Framework**: ASP.NET Core 8.0 Web API
- **Database**: PostgreSQL with Entity Framework Core (snake_case naming)
- **Authentication**: ASP.NET Core Identity with JWT tokens
- **Message Queue**: In-memory queues (hexagonal architecture for future cloud swap)
- **Time Handling**: NodaTime for precise timestamp management
- **Logging**: Serilog with console output (structured logging via hexagonal architecture)
- **Metrics**: In-memory metrics collection (hexagonal architecture for future cloud swap)
- **Testing**: xUnit with integration test framework

## C# Project Structure

### SocialAnimal.Core (Class Library)
- Domain models (`*Record` types)
- Business services (`*Service` classes) 
- Portal interfaces (`ILoggerPortal`, `IMetricsPortal`, `IMessagePortal`)
- Repository interfaces (`ICrudRepo<T>`, `IEventRepo`, etc.)
- Message types (`*Message` classes)
- No external dependencies except NodaTime

### SocialAnimal.Infrastructure (Class Library)
- Database entities and DbContext
- Repository implementations
- Entity-Record mapping (`IInto<T>`, `IFrom<T>`)
- In-memory portal implementations (console logger, in-memory metrics/queues)
- References: Core, EntityFramework, Npgsql, NodaTime

### SocialAnimal.Web (ASP.NET Core Web API)
- Controllers (`*Controller` classes)
- Startup/Program configuration
- Dependency injection setup
- Middleware configuration
- References: Core, Infrastructure

### SocialAnimal.Tests (xUnit Test Project)
- Integration tests with test database
- Unit tests for services and repositories
- Test utilities and builders
- References: Core, Infrastructure, Web

### Project Dependencies
```
Web → Infrastructure → Core
Tests → Web, Infrastructure, Core
```

## Implementation Phases

### Phase 1: Project Setup & Core Infrastructure
1. Create ASP.NET Core 8.0 Web API project structure
2. Set up Clean Architecture folder structure (`Core/`, `Infrastructure/`, `Web/`)
3. Configure Entity Framework Core with PostgreSQL
4. Add EFCore.NamingConventions package for snake_case naming
5. Configure DbContext with `.UseSnakeCaseNamingConvention()`
6. Implement base repository patterns (`ICrudRepo<T>`, `ICrudQueries<T>`)
7. Set up dependency injection with convention-based service discovery

### Phase 2: Hexagonal Architecture Abstractions
1. Create portal interfaces in `Core/Portals/`:
   - `ILoggerPortal` for structured logging
   - `IMetricsPortal` for metrics collection
   - `IMessagePortal` for async messaging
2. Implement in-memory infrastructure adapters:
   - Console-based logger implementation
   - In-memory metrics collector
   - In-memory message queue with pub/sub
3. Configure NodaTime for timestamp handling

### Phase 3: Domain Foundation
1. Create core domain records (`UserRecord`, `EventRecord`, etc.)
2. Implement entity-record mapping with `IInto<T>` and `IFrom<T>` patterns
3. Set up database context and migrations
4. Create base service classes following naming conventions

### Phase 4: Authentication & User Management
1. Implement ASP.NET Core Identity
2. Create JWT token authentication
3. Build user registration and login endpoints
4. Add user management services

### Phase 5: Event Management Core
1. Create event domain models and services
2. Implement RSVP/attendance system
3. Add location/venue management
4. Build event search and filtering

### Phase 6: Testing & Quality
1. Set up xUnit testing framework
2. Create integration tests with test database
3. Add unit tests for services and repositories
4. Configure health check endpoints

## Architectural Principles

### Clean Architecture / Hexagonal Architecture
- Core Domain Separation: Business logic in `Core/` layer
- Infrastructure Layer: Database, external services in dedicated layers
- Portals Pattern: Interfaces in `Core/Portals/` for layer contracts
- Dependency Direction: Dependencies point inward toward core

### CQRS-like Pattern
- Read/Write Separation: `ICrudQueries<T>` for reads, repositories for writes
- Specialized Repositories: Domain-specific repos for complex queries
- Generic CRUD: `ICrudRepo` for basic operations

### Message-Driven Architecture
- Publishers and Subscribers: `IMessagePublisher` and `IMessageSubscriber<T>`
- Async Processing: Dedicated subscriber classes
- Strongly-typed Messages: Message classes for operations

## Naming Conventions
- **`*Record`**: Domain models/DTOs
- **`*Service`**: Business logic services
- **`*Message`**: Message types for async processing
- **`*Repo`**: Repository interfaces
- **`*Controller`**: MVC controllers
- **`*Stub`**: DTOs for creation/updates
- **Prefixed External IDs**: `user_abc123`, `event_xyz789`
- **Database IDs**: `long` type with descriptive names (`UserId`, `EventId`)

## Future Extensibility
The hexagonal architecture design allows for easy swapping of:
- In-memory message queues → Redis/Azure Service Bus
- Console logging → Azure Application Insights/CloudWatch
- In-memory metrics → Prometheus/Azure Monitor
- Local development → Cloud deployment